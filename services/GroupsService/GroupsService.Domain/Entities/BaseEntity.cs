using GroupsService.Domain.DomainEvents;

namespace GroupsService.Domain.Entities;

public class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public string Id { get; protected set; } = String.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    public void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
    
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}