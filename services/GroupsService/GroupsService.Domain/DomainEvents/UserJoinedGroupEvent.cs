using System.Text.Json.Serialization;

namespace GroupsService.Domain.DomainEvents;

public class UserJoinedGroupEvent : IDomainEvent
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
    
    [JsonPropertyName("role")]
    [JsonInclude]
    public string Role { get; set; } = null!;
    
    [JsonPropertyName("joinedAt")]
    [JsonInclude]
    public DateTime JoinedAt { get; set; }
    
    public UserJoinedGroupEvent(string groupId, string userId, string role, DateTime joinedAt)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        GroupId = groupId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
    }
    
    public UserJoinedGroupEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        GroupId = string.Empty;
        UserId = string.Empty;
        Role = string.Empty;
        JoinedAt = DateTime.UtcNow;
    }
}
