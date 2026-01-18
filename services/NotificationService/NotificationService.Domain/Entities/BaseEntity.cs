using NotificationService.Domain.DomainEvents;

namespace NotificationService.Domain.Entities;

public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public string Id { get; protected internal set; } = string.Empty;
    
    public DateTime CreatedAt { get; protected internal set; }
    
    public DateTime? UpdatedAt { get; protected internal set; }
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}