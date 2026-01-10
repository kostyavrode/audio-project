using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.DomainEvents;

public class UserRegisteredEvent : IDomainEvent
{
    public Guid EventId { get; }
    
    public DateTime OccurredAt { get; }
    
    public DateTime RegisteredAt { get; }
    
    public string UserId { get; }
    
    public NickName NickName { get; }
    
    public Email Email { get; }
    
    public UserRegisteredEvent(string userId, Email email, NickName nickName, DateTime registeredAt)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        UserId = userId;
        Email = email;
        NickName = nickName;
        RegisteredAt = registeredAt;
    }
}