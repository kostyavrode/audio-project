using GroupsService.Domain.Entities;

namespace GroupsService.Domain.Interfaces;

public interface IGroupInviteRepository
{
    Task AddAsync(GroupInvite invite, CancellationToken cancellationToken = default);
    Task<GroupInvite?> GetByIdAsync(string inviteId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GroupInvite>> GetForNickNameAsync(string normalizedNickName, CancellationToken cancellationToken = default);
    Task<GroupInvite?> FindSameAsync(string groupId, string? channelId, string normalizedNickName, CancellationToken cancellationToken = default);
    Task RemoveAsync(GroupInvite invite, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
