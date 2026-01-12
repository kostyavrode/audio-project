using ChatService.Domain.Entities;
using ChatService.Domain.Interfaces;
using ChatService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Repository;

public class MessageRepository : IMessageRepository
{
    private readonly ChatDbContext _context;

    public MessageRepository(ChatDbContext context)
    {
        _context = context;
    }
    
    public async Task<Message?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }
    
    public async Task<IEnumerable<Message>> GetByGroupIdAsync(string groupId, int limit, int offset, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Where(m => m.GroupId == groupId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<int> CountByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .CountAsync(m => m.GroupId == groupId, cancellationToken);
    }
    
    public async Task AddAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _context.Messages.AddAsync(message, cancellationToken);
    }
    
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}