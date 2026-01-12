namespace GroupsService.Domain.DomainEvents;

public interface IDomainEvent
{
    Guid EventId { get; }
    
    DateTime OccurredAt { get; }
}
