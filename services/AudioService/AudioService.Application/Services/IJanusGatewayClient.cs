namespace AudioService.Application.Services;

public interface IJanusGatewayClient
{
    Task<long> CreateRoomAsync(long roomId, string description, CancellationToken cancellationToken = default);
    Task DeleteRoomAsync(long roomId, CancellationToken cancellationToken = default);
    Task<JanusRoomInfo?> GetRoomInfoAsync(long roomId, CancellationToken cancellationToken = default);
    Task<List<JanusParticipant>> GetRoomParticipantsAsync(long roomId, CancellationToken cancellationToken = default);
    // SetParticipantVolumeAsync удален - громкость теперь управляется на клиенте через Web Audio API
}

public class JanusRoomInfo
{
    public long RoomId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int ParticipantsCount { get; set; }
}

public class JanusParticipant
{
    public long Id { get; set; }
    public string Display { get; set; } = string.Empty;
    public bool Muted { get; set; }
}
