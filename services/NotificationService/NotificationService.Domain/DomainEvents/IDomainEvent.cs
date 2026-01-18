namespace NotificationService.Domain.DomainEvents;

public class IDomainEvent
{
    Guid EventId { get; }
    
    DateTime OccurredAt { get; }
}