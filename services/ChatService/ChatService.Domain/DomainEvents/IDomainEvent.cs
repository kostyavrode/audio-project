namespace ChatService.Domain.DomainEvents;

public interface IDomainEvent
{
    Guid EventId { get; }
    
    DateTime OccurredAt { get; }
}