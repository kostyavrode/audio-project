using System.Text.Json.Serialization;

namespace GroupsService.Domain.DomainEvents;

public class GroupCreatedEvent : IDomainEvent
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }
    
    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; }
    
    [JsonPropertyName("groupId")]
    [JsonInclude]
    public string GroupId { get; set; } = null!;
    
    [JsonPropertyName("name")]
    [JsonInclude]
    public string Name { get; set; } = null!;
    
    [JsonPropertyName("ownerId")]
    [JsonInclude]
    public string OwnerId { get; set; } = null!;
    
    [JsonPropertyName("createdAt")]
    [JsonInclude]
    public DateTime CreatedAt { get; set; }
    
    public GroupCreatedEvent(string groupId, string name, string ownerId, DateTime createdAt)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        GroupId = groupId;
        Name = name;
        OwnerId = ownerId;
        CreatedAt = createdAt;
    }
    
    public GroupCreatedEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        GroupId = string.Empty;
        Name = string.Empty;
        OwnerId = string.Empty;
        CreatedAt = DateTime.UtcNow;
    }
}
