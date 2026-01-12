using ChatService.Domain.Entities;

namespace ChatService.Domain.Interfaces;

public interface IGroupMemberRepository
{
    Task<GroupMember?> GetByGroupIdAndUserIdAsync(string groupId, string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GroupMember>> GetByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string groupId, string userId, CancellationToken cancellationToken = default);
    Task AddAsync(GroupMember groupMember, CancellationToken cancellationToken = default);
    Task RemoveAsync(GroupMember groupMember, CancellationToken cancellationToken = default);
    Task RemoveByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
