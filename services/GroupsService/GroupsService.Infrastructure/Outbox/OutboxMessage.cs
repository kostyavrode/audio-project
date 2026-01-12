namespace GroupsService.Infrastructure.Outbox;

public enum OutboxMessageStatus
{
    Pending = 0,
    Published = 1,
    Failed = 2
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    
    public Guid EventId { get; set; }
    
    public string EventType { get; set; } = string.Empty;
    
    public string Payload { get; set; } = string.Empty;
    
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? PublishedAt { get; set; }
    
    public int RetryCount { get; set; } = 0;
    
    public string? ErrorMessage { get; set; }
}
