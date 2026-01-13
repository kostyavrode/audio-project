namespace AudioService.Application.DTOs;

public class AudioParticipantDto
{
    public long Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsMuted { get; set; }
}
