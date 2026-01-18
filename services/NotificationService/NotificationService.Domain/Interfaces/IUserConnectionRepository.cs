using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

public interface IUserConnectionRepository
{
    Task AddAsync(UserConnection connection, CancellationToken cancellationToken = default);
    
    Task<UserConnection?> GetByConnectionIdAsync(string connectionId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<UserConnection>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    
    Task RemoveAsync(string connectionId, CancellationToken cancellationToken = default);
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}