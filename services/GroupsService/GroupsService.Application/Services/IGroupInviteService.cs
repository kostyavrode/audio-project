using GroupsService.Application.DTOs;

namespace GroupsService.Application.Services;

public interface IGroupInviteService
{
    Task<GroupInviteDto> CreateAsync(CreateGroupInviteDto dto, string fromUserId, string fromNickName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GroupInviteDto>> GetPendingAsync(string nickName, CancellationToken cancellationToken = default);
    Task<AcceptedInviteDto> AcceptAsync(string inviteId, string nickName, CancellationToken cancellationToken = default);
    Task DeclineAsync(string inviteId, string nickName, CancellationToken cancellationToken = default);
}
