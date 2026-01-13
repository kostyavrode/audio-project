using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AudioService.Application.Services;

namespace AudioService.Infrastructure.ExternalServices;

public class JanusGatewayClient : IJanusGatewayClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JanusGatewaySettings _settings;
    private readonly ILogger<JanusGatewayClient> _logger;

    public JanusGatewayClient(
        HttpClient httpClient,
        IOptions<JanusGatewaySettings> settings,
        ILogger<JanusGatewayClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _logger.LogInformation("JanusGatewayClient initialized with BaseUrl: {BaseUrl}", _settings.BaseUrl);
    }

    public async Task<long> CreateRoomAsync(long roomId, string description, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionId = await CreateSessionAsync(cancellationToken);
            var handleId = await AttachPluginAsync(sessionId, cancellationToken);

            var request = new
            {
                janus = "message",
                plugin = "janus.plugin.audiobridge",
                transaction = Guid.NewGuid().ToString(),
                body = new
                {
                    request = "create",
                    room = roomId,
                    description = description,
                    sampling = 16000
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/janus/{sessionId}/{handleId}",
                request,
                cancellationToken
            );

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Janus create room response: {Response}", responseContent);

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (result.TryGetProperty("plugindata", out var pluginData) &&
                pluginData.TryGetProperty("data", out var data) &&
                data.TryGetProperty("room", out var roomElement))
            {
                if (roomElement.TryGetInt64(out var createdRoomId))
                {
                    _logger.LogInformation("Created Janus room {RoomId} with description {Description}", createdRoomId, description);
                    return createdRoomId;
                }
            }

            _logger.LogError("Failed to parse room ID from Janus response. Response structure: {Response}", responseContent);
            throw new InvalidOperationException($"Failed to parse room ID from Janus response. Response: {responseContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Janus room {RoomId}", roomId);
            throw;
        }
    }

    public async Task DeleteRoomAsync(long roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionId = await CreateSessionAsync(cancellationToken);
            var handleId = await AttachPluginAsync(sessionId, cancellationToken);

            var request = new
            {
                janus = "message",
                plugin = "janus.plugin.audiobridge",
                transaction = Guid.NewGuid().ToString(),
                body = new
                {
                    request = "destroy",
                    room = roomId,
                    secret = _settings.ApiSecret
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/janus/{sessionId}/{handleId}",
                request,
                cancellationToken
            );

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Deleted Janus room {RoomId}", roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Janus room {RoomId}", roomId);
            throw;
        }
    }

    public async Task<JanusRoomInfo?> GetRoomInfoAsync(long roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionId = await CreateSessionAsync(cancellationToken);
            var handleId = await AttachPluginAsync(sessionId, cancellationToken);

            var request = new
            {
                janus = "message",
                plugin = "janus.plugin.audiobridge",
                transaction = Guid.NewGuid().ToString(),
                body = new
                {
                    request = "listparticipants",
                    room = roomId
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/janus/{sessionId}/{handleId}",
                request,
                cancellationToken
            );

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

            if (result.TryGetProperty("plugindata", out var pluginData) &&
                pluginData.TryGetProperty("data", out var data) &&
                data.TryGetProperty("participants", out var participantsElement))
            {
                var participants = participantsElement.EnumerateArray().ToList();

                return new JanusRoomInfo
                {
                    RoomId = roomId,
                    Description = data.TryGetProperty("description", out var desc) ? desc.GetString() ?? string.Empty : string.Empty,
                    ParticipantsCount = participants.Count
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Janus room info {RoomId}", roomId);
            throw;
        }
    }

    private async Task<long> CreateSessionAsync(CancellationToken cancellationToken)
    {
        var request = new
        {
            janus = "create",
            transaction = Guid.NewGuid().ToString()
        };

        var response = await _httpClient.PostAsJsonAsync("/janus", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        if (result.TryGetProperty("data", out var data) &&
            data.TryGetProperty("id", out var idElement) &&
            idElement.TryGetInt64(out var sessionId))
        {
            return sessionId;
        }

        throw new InvalidOperationException("Failed to parse session ID from Janus response");
    }

    private async Task<long> AttachPluginAsync(long sessionId, CancellationToken cancellationToken)
    {
        var request = new
        {
            janus = "attach",
            plugin = "janus.plugin.audiobridge",
            transaction = Guid.NewGuid().ToString()
        };

        var response = await _httpClient.PostAsJsonAsync($"/janus/{sessionId}", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        if (result.TryGetProperty("data", out var data) &&
            data.TryGetProperty("id", out var idElement) &&
            idElement.TryGetInt64(out var handleId))
        {
            return handleId;
        }

        throw new InvalidOperationException("Failed to parse handle ID from Janus response");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
