namespace NotificationService.Domain.DomainEvents;

public class MessageSentEvent : IDomainEvent
{
    public Guid EventId { get; }
    
    public DateTime OccurredAt { get; }
    
    public string MessageId { get; }
    
    public string GroupId { get; }
    
    public string UserId { get; }
    
    public string Content { get; }

    public MessageSentEvent(string messageId, string groupId, string userId, string content, DateTime occurredAt)
    {
        EventId = Guid.NewGuid();
        OccurredAt = occurredAt;
        MessageId = messageId;
        GroupId = groupId;
        UserId = userId;
        Content = content;
    }
}