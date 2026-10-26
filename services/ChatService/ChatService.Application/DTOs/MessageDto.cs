namespace ChatService.Application.DTOs;

public class MessageDto
{
    public string Id { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    public string UserNickName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // "text" или "sticker"; строкой, чтобы клиентам не зависеть от числовых значений enum
    public string Type { get; set; } = MessageTypes.Text;
    public int? StickerId { get; set; }
}

public static class MessageTypes
{
    public const string Text = "text";
    public const string Sticker = "sticker";
}
