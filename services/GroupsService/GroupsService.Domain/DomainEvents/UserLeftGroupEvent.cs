using System.Text.Json.Serialization;

namespace GroupsService.Domain.DomainEvents;

public class UserLeftGroupEvent : IDomainEvent
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }
    
    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; }
    
    [JsonPropertyName("groupId")]
    [JsonInclude]
    public string GroupId { get; set; } = null!;
    
    [JsonPropertyName("userId")]
    [JsonInclude]
    public string UserId { get; set; } = null!;
    
    [JsonPropertyName("leftAt")]
    [JsonInclude]
    public DateTime LeftAt { get; set; }
    
    public UserLeftGroupEvent(string groupId, string userId, DateTime leftAt)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        GroupId = groupId;
        UserId = userId;
        LeftAt = leftAt;
    }
    
    public UserLeftGroupEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        GroupId = string.Empty;
        UserId = string.Empty;
        LeftAt = DateTime.UtcNow;
    }
}
