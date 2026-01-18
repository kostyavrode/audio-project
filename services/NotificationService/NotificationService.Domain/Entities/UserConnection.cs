namespace NotificationService.Domain.Entities;

public class UserConnection : BaseEntity
{
    public string UserId { get; private set; } = String.Empty;
    
    public string ConnectionId { get; private set; } = String.Empty;
    
    public DateTime ConnectedAt { get; private set; } = DateTime.UtcNow;
    
    public DateTime LastActivityAt { get; protected internal set; } = DateTime.UtcNow;
    
    private UserConnection() { }
}