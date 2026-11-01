namespace GroupsService.Application.DTOs;

public class CreateGroupInviteDto
{
    public string GroupId { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string? ChannelId { get; set; }
    public string? ChannelName { get; set; }
}

public class GroupInviteDto
{
    public string Id { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string? ChannelId { get; set; }
    public string? ChannelName { get; set; }
    public string FromNickName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Куда перейти после принятия приглашения.</summary>
public class AcceptedInviteDto
{
    public string GroupId { get; set; } = string.Empty;
    public string? ChannelId { get; set; }

    // Ключ для группы с паролем, если пригласил владелец или админ
    public string? InviteCode { get; set; }
}
