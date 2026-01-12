namespace ChatService.Application.DTOs;

public class SendMessageDto
{
    public string GroupId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
