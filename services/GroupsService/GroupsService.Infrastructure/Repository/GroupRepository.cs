using GroupsService.Domain.Entities;
using GroupsService.Domain.Interfaces;
using GroupsService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GroupsService.Infrastructure.Repository;

public class GroupRepository : IGroupRepository
{
    private readonly GroupsDbContext _context;

    public GroupRepository(GroupsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        await _context.Groups.AddAsync(group, cancellationToken);
    }
    
    public async Task<Group?> GetByIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        return await _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
    }
    
    public async Task<IEnumerable<Group>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var groupIds = await _context.GroupMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId)
            .Distinct()
            .ToListAsync(cancellationToken);
        
        return await _context.Groups
            .Include(g => g.Members)
            .Where(g => groupIds.Contains(g.Id))
            .ToListAsync(cancellationToken);
    }
    
    public async Task UpdateAsync(Group group, CancellationToken cancellationToken = default)
    {
        _context.Groups.Update(group);
    }
    
    public async Task DeleteAsync(Group group, CancellationToken cancellationToken = default)
    {
        _context.Groups.Remove(group);
    }
    
    public async Task<(IEnumerable<Group> Groups, int TotalCount)> SearchAsync(string? query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var queryable = _context.Groups.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(query))
        {
            queryable = queryable.Where(g => g.Name.Contains(query));
        }
        
        var totalCount = await queryable.CountAsync(cancellationToken);
        
        var groups = await queryable
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(g => g.Members)
            .ToListAsync(cancellationToken);
        
        return (groups, totalCount);
    }
    
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}