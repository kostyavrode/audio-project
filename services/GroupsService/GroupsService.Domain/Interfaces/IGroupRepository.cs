using GroupsService.Domain.Entities;

namespace GroupsService.Domain.Interfaces;

public interface IGroupRepository
{
    Task AddAsync(Group group, CancellationToken cancellationToken = default);
    Task<Group?> GetByIdAsync(string groupId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Group>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Group group, CancellationToken cancellationToken = default);
    Task DeleteAsync(Group group, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Group> Groups, int TotalCount)> SearchAsync(string? query, int page, int pageSize, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}