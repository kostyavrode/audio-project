namespace ChatService.Application.DTOs;

public class MessageDto
{
    public string Id { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
