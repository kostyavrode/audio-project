using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Notification>> GetPendingNotificationsAsync(int batchSize, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}