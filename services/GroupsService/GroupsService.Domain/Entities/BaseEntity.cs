namespace GroupsService.Domain.Entities;

public class BaseEntity
{
    public string Id { get; protected set; } = String.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}