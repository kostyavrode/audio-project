namespace AudioService.Application.DTOs;

public class RegisterParticipantDto
{
    public long ParticipantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
