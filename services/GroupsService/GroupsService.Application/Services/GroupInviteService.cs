using GroupsService.Application.DTOs;
using GroupsService.Domain.Entities;
using GroupsService.Domain.Exceptions;
using GroupsService.Domain.Interfaces;
using GroupsService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace GroupsService.Application.Services;

public class GroupInviteService : IGroupInviteService
{
    private readonly IGroupInviteRepository _invites;
    private readonly IGroupRepository _groups;
    private readonly ILogger<GroupInviteService> _logger;

    public GroupInviteService(IGroupInviteRepository invites, IGroupRepository groups, ILogger<GroupInviteService> logger)
    {
        _invites = invites;
        _groups = groups;
        _logger = logger;
    }

    public async Task<GroupInviteDto> CreateAsync(CreateGroupInviteDto dto, string fromUserId, string fromNickName, CancellationToken cancellationToken = default)
    {
        var group = await _groups.GetByIdAsync(dto.GroupId, cancellationToken)
            ?? throw new GroupNotFoundException(dto.GroupId);

        var sender = group.Members.FirstOrDefault(m => m.UserId == fromUserId)
            ?? throw new UnauthorizedAccessException("Only group members can send invites");

        var target = GroupInvite.NormalizeNickName(dto.NickName);
        if (target == GroupInvite.NormalizeNickName(fromNickName))
        {
            throw new DomainException("Нельзя пригласить самого себя");
        }

        // Участника группы имеет смысл звать только в конкретный канал
        var channelId = string.IsNullOrWhiteSpace(dto.ChannelId) ? null : dto.ChannelId;
        var targetIsMember = group.Members.Any(m => GroupInvite.NormalizeNickName(m.NickName) == target);
        if (targetIsMember && channelId == null)
        {
            throw new DomainException("Этот пользователь уже в группе");
        }

        // Повторное приглашение не копится: у получателя остаётся одно уведомление
        var previous = await _invites.FindSameAsync(group.Id, channelId, target, cancellationToken);
        if (previous != null)
        {
            await _invites.RemoveAsync(previous, cancellationToken);
        }

        var canBypassPassword = sender.Role == GroupMemberRole.Owner || sender.Role == GroupMemberRole.Admin;
        var invite = GroupInvite.Create(Guid.NewGuid().ToString(), group, channelId, dto.ChannelName,
            fromUserId, fromNickName, dto.NickName, canBypassPassword);

        await _invites.AddAsync(invite, cancellationToken);
        await _invites.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invite to group {GroupId} sent by {UserId}", group.Id, fromUserId);
        return ToDto(invite);
    }

    public async Task<IReadOnlyList<GroupInviteDto>> GetPendingAsync(string nickName, CancellationToken cancellationToken = default)
    {
        var invites = await _invites.GetForNickNameAsync(GroupInvite.NormalizeNickName(nickName), cancellationToken);
        return invites.Select(ToDto).ToList();
    }

    public async Task<AcceptedInviteDto> AcceptAsync(string inviteId, string nickName, CancellationToken cancellationToken = default)
    {
        var invite = await GetOwnInviteAsync(inviteId, nickName, cancellationToken);
        var result = new AcceptedInviteDto { GroupId = invite.GroupId, ChannelId = invite.ChannelId };

        var group = await _groups.GetByIdAsync(invite.GroupId, cancellationToken);
        if (group == null)
        {
            await _invites.RemoveAsync(invite, cancellationToken);
            await _invites.SaveChangesAsync(cancellationToken);
            throw new GroupNotFoundException(invite.GroupId);
        }

        if (invite.BypassPassword && !string.IsNullOrEmpty(group.PasswordHash))
        {
            result.InviteCode = group.GetOrCreateInviteCode();
            await _groups.UpdateAsync(group, cancellationToken);
        }

        await _invites.RemoveAsync(invite, cancellationToken);
        await _invites.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task DeclineAsync(string inviteId, string nickName, CancellationToken cancellationToken = default)
    {
        var invite = await GetOwnInviteAsync(inviteId, nickName, cancellationToken);
        await _invites.RemoveAsync(invite, cancellationToken);
        await _invites.SaveChangesAsync(cancellationToken);
    }

    // Чужое приглашение не отличаем от несуществующего
    private async Task<GroupInvite> GetOwnInviteAsync(string inviteId, string nickName, CancellationToken cancellationToken)
    {
        var invite = await _invites.GetByIdAsync(inviteId, cancellationToken);
        if (invite == null || invite.ToNickName != GroupInvite.NormalizeNickName(nickName))
        {
            throw new GroupNotFoundException(inviteId);
        }
        return invite;
    }

    private static GroupInviteDto ToDto(GroupInvite invite) => new()
    {
        Id = invite.Id,
        GroupId = invite.GroupId,
        GroupName = invite.GroupName,
        ChannelId = invite.ChannelId,
        ChannelName = invite.ChannelName,
        FromNickName = invite.FromNickName,
        CreatedAt = invite.CreatedAt
    };
}
