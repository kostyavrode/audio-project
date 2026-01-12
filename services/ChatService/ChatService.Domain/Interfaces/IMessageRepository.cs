using ChatService.Domain.Entities;

namespace ChatService.Domain.Interfaces;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Message>> GetByGroupIdAsync(string groupId, int limit, int offset, CancellationToken cancellationToken = default);
    
    Task<int> CountByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);
    
    Task AddAsync(Message message, CancellationToken cancellationToken = default);
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}