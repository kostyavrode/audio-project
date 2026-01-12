using System.Text.Json.Serialization;

namespace GroupsService.Domain.DomainEvents;

public class GroupDeletedEvent : IDomainEvent
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }
    
    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; }
    
    [JsonPropertyName("groupId")]
    [JsonInclude]
    public string GroupId { get; set; } = null!;
    
    [JsonPropertyName("deletedAt")]
    [JsonInclude]
    public DateTime DeletedAt { get; set; }
    
    public GroupDeletedEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        GroupId = string.Empty;
        DeletedAt = DateTime.UtcNow;
    }
    
    public GroupDeletedEvent(string groupId, DateTime deletedAt)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        GroupId = groupId;
        DeletedAt = deletedAt;
    }
}
