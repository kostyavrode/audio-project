namespace AudioService.Application.DTOs;

public class AudioChannelDto
{
    public string Id { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long? JanusRoomId { get; set; }
    public DateTime CreatedAt { get; set; }
}
