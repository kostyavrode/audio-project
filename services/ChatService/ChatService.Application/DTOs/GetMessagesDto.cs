namespace ChatService.Application.DTOs;

public class GetMessagesDto
{
    public string GroupId { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
