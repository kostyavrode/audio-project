using ChatService.Domain.Entities;
using ChatService.Domain.Interfaces;
using ChatService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Repository;

public class GroupMemberRepository : IGroupMemberRepository
{
    private readonly ChatDbContext _context;

    public GroupMemberRepository(ChatDbContext context)
    {
        _context = context;
    }
    
    public async Task<GroupMember?> GetByGroupIdAndUserIdAsync(string groupId, string userId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);
    }
    
    public async Task<IEnumerable<GroupMember>> GetByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupMembers
            .Where(m => m.GroupId == groupId)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<bool> ExistsAsync(string groupId, string userId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);
    }
    
    public async Task AddAsync(GroupMember groupMember, CancellationToken cancellationToken = default)
    {
        await _context.GroupMembers.AddAsync(groupMember, cancellationToken);
    }
    
    public Task RemoveAsync(GroupMember groupMember, CancellationToken cancellationToken = default)
    {
        _context.GroupMembers.Remove(groupMember);
        return Task.CompletedTask;
    }
    
    public async Task RemoveByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var members = await _context.GroupMembers
            .Where(m => m.GroupId == groupId)
            .ToListAsync(cancellationToken);
        
        _context.GroupMembers.RemoveRange(members);
    }
    
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}