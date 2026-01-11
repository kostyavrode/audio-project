using AuthService.Domain.DomainEvents;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Domain.Entities;

public abstract class BaseEntity : IdentityUser
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    protected BaseEntity()
    {
        CreatedAt = DateTime.UtcNow;
    }
    
    public string Id { get; protected internal set; } = string.Empty;
    
    public DateTime CreatedAt { get;  set; } 
    
    public DateTime? UpdatedAt { get; protected internal set; }
    
    /// <summary>
    /// Доменные события сущности.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected internal void SetId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or empty", nameof(id));
            
        Id = id;
    }
    
    public void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Добавить доменное событие.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    /// <summary>
    /// Очистить доменные события.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}