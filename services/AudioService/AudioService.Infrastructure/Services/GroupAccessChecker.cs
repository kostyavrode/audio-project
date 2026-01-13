using AudioService.Application.Services;
using AudioService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AudioService.Infrastructure.Services;

public class GroupAccessChecker : IGroupAccessChecker
{
    private readonly AudioDbContext _dbContext;

    public GroupAccessChecker(AudioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsGroupOwnerAsync(string groupId, string userId, CancellationToken cancellationToken = default)
    {
        var groupMember = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);

        if (groupMember == null)
        {
            return false;
        }

        if (groupMember.Role == GroupMemberRole.Owner)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> IsGroupMemberAsync(string groupId, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);
    }
}
