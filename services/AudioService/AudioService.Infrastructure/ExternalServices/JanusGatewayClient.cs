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
                plugin = "janus.plugin.videoroom",
                transaction = Guid.NewGuid().ToString(),
                body = new
                {
                    request = "create",
                    room = roomId,
                    description = description,
                    publishers = 15,
                    bitrate = 64000,
                    fir_freq = 10,
                    audiocodec = "opus",
                    videocodec = "vp8"
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
                plugin = "janus.plugin.videoroom",
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
                plugin = "janus.plugin.videoroom",
                transaction = Guid.NewGuid().ToString(),
                body = new
                {
                    request = "list",
                    room = roomId
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/janus/{sessionId}/{handleId}",
                request,
                cancellationToken
            );

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Проверяем наличие ошибки "No such room"
            if (result.TryGetProperty("plugindata", out var pluginData) &&
                pluginData.TryGetProperty("data", out var data))
            {
                // Проверяем наличие ошибки
                if (data.TryGetProperty("error_code", out var errorCode) &&
                    data.TryGetProperty("error", out var error))
                {
                    var errorCodeValue = errorCode.GetInt32();
                    var errorMessage = error.GetString();
                    
                    if (errorCodeValue == 485 && errorMessage?.Contains("No such room") == true)
                    {
                        _logger.LogWarning("Janus room {RoomId} does not exist", roomId);
                        return null; // Комната не существует
                    }
                    
                    // Другие ошибки - выбрасываем исключение
                    _logger.LogError("Janus error for room {RoomId}: {ErrorCode} - {ErrorMessage}", roomId, errorCodeValue, errorMessage);
                    throw new InvalidOperationException($"Janus error: {errorMessage}");
                }

                // Если нет ошибки, проверяем наличие publishers (VideoRoom) или participants (AudioBridge)
                if (data.TryGetProperty("publishers", out var publishersElement))
                {
                    var publishers = publishersElement.EnumerateArray().ToList();

                    return new JanusRoomInfo
                    {
                        RoomId = roomId,
                        Description = data.TryGetProperty("description", out var desc) ? desc.GetString() ?? string.Empty : string.Empty,
                        ParticipantsCount = publishers.Count
                    };
                }
                // Для обратной совместимости с AudioBridge
                else if (data.TryGetProperty("participants", out var participantsElement))
                {
                    var participants = participantsElement.EnumerateArray().ToList();

                    return new JanusRoomInfo
                    {
                        RoomId = roomId,
                        Description = data.TryGetProperty("description", out var desc) ? desc.GetString() ?? string.Empty : string.Empty,
                        ParticipantsCount = participants.Count
                    };
                }
            }

            // Если структура ответа неожиданная, но статус успешный - считаем что комната существует
            if (response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Unexpected Janus response structure for room {RoomId}, assuming room exists", roomId);
                return new JanusRoomInfo
                {
                    RoomId = roomId,
                    Description = string.Empty,
                    ParticipantsCount = 0
                };
            }

            response.EnsureSuccessStatusCode();
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Janus for room {RoomId}, assuming room does not exist", roomId);
            return null; // Если не можем подключиться, считаем что комната не существует
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Janus room info {RoomId}", roomId);
            throw;
        }
    }

    public async Task<List<JanusParticipant>> GetRoomParticipantsAsync(long roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionId = await CreateSessionAsync(cancellationToken);
            var handleId = await AttachPluginAsync(sessionId, cancellationToken);

            var request = new
            {
                janus = "message",
                plugin = "janus.plugin.videoroom",
                transaction = Guid.NewGuid().ToString(),
                body = new
                {
                    request = "list",
                    room = roomId
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/janus/{sessionId}/{handleId}",
                request,
                cancellationToken
            );

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var participants = new List<JanusParticipant>();

            if (result.TryGetProperty("plugindata", out var pluginData) &&
                pluginData.TryGetProperty("data", out var data))
            {
                if (data.TryGetProperty("error_code", out _))
                {
                    return participants;
                }

                // VideoRoom использует "publishers" вместо "participants"
                if (data.TryGetProperty("publishers", out var publishersElement))
                {
                    foreach (var p in publishersElement.EnumerateArray())
                    {
                        participants.Add(new JanusParticipant
                        {
                            Id = p.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                            Display = p.TryGetProperty("display", out var display) ? display.GetString() ?? "" : "",
                            Muted = p.TryGetProperty("muted", out var muted) && muted.GetBoolean()
                        });
                    }
                }
                // Для обратной совместимости с AudioBridge
                else if (data.TryGetProperty("participants", out var participantsElement))
                {
                    foreach (var p in participantsElement.EnumerateArray())
                    {
                        participants.Add(new JanusParticipant
                        {
                            Id = p.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                            Display = p.TryGetProperty("display", out var display) ? display.GetString() ?? "" : "",
                            Muted = p.TryGetProperty("muted", out var muted) && muted.GetBoolean()
                        });
                    }
                }
            }

            return participants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get participants for room {RoomId}", roomId);
            return new List<JanusParticipant>();
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
            plugin = "janus.plugin.videoroom",
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
