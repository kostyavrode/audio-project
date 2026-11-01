using GroupsService.Domain.Exceptions;

namespace GroupsService.Domain.Entities;

/// <summary>
/// Уведомление участнику группы от другого участника. Одна запись на получателя:
/// живёт, пока он его не закроет.
/// </summary>
public class GroupNotice : BaseEntity
{
    public const int MaxTextLength = 500;

    public string GroupId { get; private set; } = string.Empty;
    public string GroupName { get; private set; } = string.Empty;
    public string FromNickName { get; private set; } = string.Empty;
    public string ToUserId { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;

    private GroupNotice()
    {
    }

    public static GroupNotice Create(string id, Group group, string fromNickName, string toUserId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainException("Введите текст уведомления");
        }

        if (text.Length > MaxTextLength)
        {
            throw new DomainException($"Текст не длиннее {MaxTextLength} символов");
        }

        return new GroupNotice
        {
            Id = id,
            GroupId = group.Id,
            GroupName = group.Name,
            FromNickName = fromNickName,
            ToUserId = toUserId,
            Text = text.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
