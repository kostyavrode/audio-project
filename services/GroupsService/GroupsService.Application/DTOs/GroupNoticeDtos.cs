namespace GroupsService.Application.DTOs;

public class SendGroupNoticeDto
{
    public string GroupId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    // Кому отправить: id участников группы
    public List<string> UserIds { get; set; } = new();
}

public class GroupNoticeDto
{
    public string Id { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string FromNickName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
