using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

namespace NotificationService.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, VideoStreamInfo>> _activeVideoStreams = new();
    
    private static int _activeConnectionsCount = 0;
    private static readonly object _connectionsLock = new object();

    public NotificationHub(
        ILogger<NotificationHub> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            Context.Abort();
            return;
        }

        lock (_connectionsLock)
        {
            _activeConnectionsCount++;
        }

        _logger.LogInformation("User {UserId} connected to NotificationHub (Total: {Count})", userId, _activeConnectionsCount);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        
        lock (_connectionsLock)
        {
            if (_activeConnectionsCount > 0)
            {
                _activeConnectionsCount--;
            }
        }

        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("User {UserId} disconnected from NotificationHub (Total: {Count})", userId, _activeConnectionsCount);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            await Clients.Caller.SendAsync("Error", "Group ID is required");
            return;
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        _logger.LogInformation("User {UserId} joined group {GroupId}", userId, groupId);
        
        await Clients.Caller.SendAsync("JoinedGroup", groupId);
    }

    public async Task LeaveGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            await Clients.Caller.SendAsync("Error", "Group ID is required");
            return;
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        _logger.LogInformation("User {UserId} left group {GroupId}", userId, groupId);
        
        await Clients.Caller.SendAsync("LeftGroup", groupId);
    }

    public async Task SendMessage(object sendMessageDto)
    {
        if (sendMessageDto == null)
        {
            await Clients.Caller.SendAsync("Error", "Message data is required");
            return;
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        try
        {
            var chatServiceUrl = _configuration.GetValue<string>("ChatServiceUrl") 
                ?? throw new InvalidOperationException("ChatServiceUrl is not configured");

            var token = GetToken();
            if (string.IsNullOrEmpty(token))
            {
                await Clients.Caller.SendAsync("Error", "Token not found");
                return;
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(sendMessageDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{chatServiceUrl}/api/Messages", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("ChatService API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                await Clients.Caller.SendAsync("Error", $"Failed to send message: {response.StatusCode}");
                return;
            }

            var messageJson = await response.Content.ReadAsStringAsync();
            var messageDto = JsonSerializer.Deserialize<JsonElement>(messageJson);

            if (messageDto.TryGetProperty("groupId", out var groupIdElement))
            {
                var groupId = groupIdElement.GetString();
                if (!string.IsNullOrEmpty(groupId))
                {
                    await Clients.Group(groupId).SendAsync("ReceiveMessage", messageDto);
                    _logger.LogInformation("Message sent to all users in group {GroupId} via SignalR", groupId);
                }
                else
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", messageDto);
                }
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveMessage", messageDto);
            }
            
            _logger.LogInformation("Message sent via ChatService API for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message via ChatService API");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    private string? GetToken()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext == null) return null;

        var token = httpContext.Request.Query["access_token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(token))
        {
            return token;
        }

        token = httpContext.Request.Cookies["access_token"];
        if (!string.IsNullOrEmpty(token))
        {
            return token;
        }

        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        return null;
    }

    private string? GetUserId()
    {
        if (Context.User == null) return null;

        var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            userId = Context.User.FindFirst("sub")?.Value;
        }
        return userId;
    }
    
    private string? GetUserNickName()
    {
        if (Context.User == null) return null;

        var userNickName = Context.User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(userNickName))
        {
            userNickName = Context.User.FindFirst("nickname")?.Value;
        }
        return userNickName;
    }
    
    public async Task StartVideoStream(string groupId, string channelId, string videoType)
    {
        var userId = GetUserId();
        var nickname = GetUserNickName();
        
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nickname))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        _logger.LogInformation("User {UserId} ({Nickname}) started {VideoType} stream in channel {ChannelId}", 
            userId, nickname, videoType, channelId);
        
        var channelStreams = _activeVideoStreams.GetOrAdd(channelId, _ => new ConcurrentDictionary<string, VideoStreamInfo>());
        channelStreams[userId] = new VideoStreamInfo
        {
            UserId = userId,
            Nickname = nickname,
            VideoType = videoType,
            Timestamp = DateTime.UtcNow
        };
        
        await Clients.Group(groupId).SendAsync("VideoStreamStarted", new
        {
            channelId,
            userId,
            nickname,
            videoType,
            timestamp = DateTime.UtcNow
        });
    }
    
    public async Task StopVideoStream(string groupId, string channelId)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        _logger.LogInformation("User {UserId} stopped video stream in channel {ChannelId}", userId, channelId);
        
        if (_activeVideoStreams.TryGetValue(channelId, out var channelStreams))
        {
            channelStreams.TryRemove(userId, out _);
            
            if (channelStreams.IsEmpty)
            {
                _activeVideoStreams.TryRemove(channelId, out _);
            }
        }
        
        await Clients.Group(groupId).SendAsync("VideoStreamStopped", new
        {
            channelId,
            userId,
            timestamp = DateTime.UtcNow
        });
    }
    
    public async Task GetActiveVideoStreams(string channelId)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        _logger.LogInformation("User {UserId} requested active video streams for channel {ChannelId}", userId, channelId);

        var activeStreams = new List<object>();

        if (_activeVideoStreams.TryGetValue(channelId, out var channelStreams))
        {
            foreach (var stream in channelStreams.Values)
            {
                if (stream.UserId != userId)
                {
                    activeStreams.Add(new
                    {
                        userId = stream.UserId,
                        nickname = stream.Nickname,
                        videoType = stream.VideoType,
                        timestamp = stream.Timestamp
                    });
                }
            }
        }

        await Clients.Caller.SendAsync("ActiveVideoStreams", new
        {
            channelId,
            streams = activeStreams
        });
    }
    
    public static int GetActiveConnectionsCount()
    {
        lock (_connectionsLock)
        {
            // Логируем для отладки
            var count = _activeConnectionsCount;
            // Используем статический logger через сервис или просто возвращаем значение
            return count;
        }
    }
}

public class VideoStreamInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string VideoType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
