using GroupsService.Domain.Exceptions;

namespace GroupsService.Domain.Entities;

/// <summary>
/// Личное приглашение в группу или голосовой канал по никнейму.
/// Живёт, пока получатель не примет или не отклонит его: ответ удаляет запись.
/// </summary>
public class GroupInvite : BaseEntity
{
    public string GroupId { get; private set; } = string.Empty;
    public string GroupName { get; private set; } = string.Empty;
    public string? ChannelId { get; private set; }
    public string? ChannelName { get; private set; }
    public string FromUserId { get; private set; } = string.Empty;
    public string FromNickName { get; private set; } = string.Empty;

    // Ник получателя в нижнем регистре: приглашения ищутся по нику из токена
    public string ToNickName { get; private set; } = string.Empty;

    // Пригласил владелец или админ: получатель вступит в группу с паролем без пароля
    public bool BypassPassword { get; private set; }

    private GroupInvite()
    {
    }

    public static string NormalizeNickName(string nickName) => nickName.Trim().ToLowerInvariant();

    public static GroupInvite Create(string id, Group group, string? channelId, string? channelName,
        string fromUserId, string fromNickName, string toNickName, bool bypassPassword)
    {
        if (string.IsNullOrWhiteSpace(toNickName))
        {
            throw new DomainException("Nickname cannot be empty");
        }

        return new GroupInvite
        {
            Id = id,
            GroupId = group.Id,
            GroupName = group.Name,
            ChannelId = string.IsNullOrWhiteSpace(channelId) ? null : channelId,
            ChannelName = string.IsNullOrWhiteSpace(channelName) ? null : channelName.Trim(),
            FromUserId = fromUserId,
            FromNickName = fromNickName,
            ToNickName = NormalizeNickName(toNickName),
            BypassPassword = bypassPassword,
            CreatedAt = DateTime.UtcNow
        };
    }
}
