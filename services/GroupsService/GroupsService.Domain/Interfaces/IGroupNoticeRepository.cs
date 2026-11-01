using GroupsService.Domain.Entities;

namespace GroupsService.Domain.Interfaces;

public interface IGroupNoticeRepository
{
    Task AddRangeAsync(IEnumerable<GroupNotice> notices, CancellationToken cancellationToken = default);
    Task<GroupNotice?> GetByIdAsync(string noticeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GroupNotice>> GetForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task RemoveAsync(GroupNotice notice, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
