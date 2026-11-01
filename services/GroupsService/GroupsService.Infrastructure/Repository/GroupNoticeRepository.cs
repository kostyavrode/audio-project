using GroupsService.Domain.Entities;
using GroupsService.Domain.Interfaces;
using GroupsService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GroupsService.Infrastructure.Repository;

public class GroupNoticeRepository : IGroupNoticeRepository
{
    private readonly GroupsDbContext _context;

    public GroupNoticeRepository(GroupsDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<GroupNotice> notices, CancellationToken cancellationToken = default)
    {
        await _context.GroupNotices.AddRangeAsync(notices, cancellationToken);
    }

    public Task<GroupNotice?> GetByIdAsync(string noticeId, CancellationToken cancellationToken = default)
    {
        return _context.GroupNotices.FirstOrDefaultAsync(n => n.Id == noticeId, cancellationToken);
    }

    public async Task<IReadOnlyList<GroupNotice>> GetForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupNotices
            .Where(n => n.ToUserId == userId)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task RemoveAsync(GroupNotice notice, CancellationToken cancellationToken = default)
    {
        _context.GroupNotices.Remove(notice);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
