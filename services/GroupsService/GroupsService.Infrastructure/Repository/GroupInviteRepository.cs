using GroupsService.Domain.Entities;
using GroupsService.Domain.Interfaces;
using GroupsService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GroupsService.Infrastructure.Repository;

public class GroupInviteRepository : IGroupInviteRepository
{
    private readonly GroupsDbContext _context;

    public GroupInviteRepository(GroupsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(GroupInvite invite, CancellationToken cancellationToken = default)
    {
        await _context.GroupInvites.AddAsync(invite, cancellationToken);
    }

    public Task<GroupInvite?> GetByIdAsync(string inviteId, CancellationToken cancellationToken = default)
    {
        return _context.GroupInvites.FirstOrDefaultAsync(i => i.Id == inviteId, cancellationToken);
    }

    public async Task<IReadOnlyList<GroupInvite>> GetForNickNameAsync(string normalizedNickName, CancellationToken cancellationToken = default)
    {
        return await _context.GroupInvites
            .Where(i => i.ToNickName == normalizedNickName)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<GroupInvite?> FindSameAsync(string groupId, string? channelId, string normalizedNickName, CancellationToken cancellationToken = default)
    {
        return _context.GroupInvites.FirstOrDefaultAsync(
            i => i.GroupId == groupId && i.ChannelId == channelId && i.ToNickName == normalizedNickName,
            cancellationToken);
    }

    public Task RemoveAsync(GroupInvite invite, CancellationToken cancellationToken = default)
    {
        _context.GroupInvites.Remove(invite);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
