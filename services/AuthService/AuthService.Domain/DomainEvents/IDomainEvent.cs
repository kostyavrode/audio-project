namespace AuthService.Domain.DomainEvents;

public interface IDomainEvent
{
    Guid EventId { get; }
    
    DateTime OccurredAt { get; }
}