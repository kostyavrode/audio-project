using System.Text.Json.Serialization;

namespace GroupsService.Domain.DomainEvents;

public class GroupMemberRoleChangedEvent : IDomainEvent
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

    [JsonPropertyName("changedAt")]
    [JsonInclude]
    public DateTime ChangedAt { get; set; }

    public GroupMemberRoleChangedEvent(string groupId, string userId, string role, DateTime changedAt)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        GroupId = groupId;
        UserId = userId;
        Role = role;
        ChangedAt = changedAt;
    }

    public GroupMemberRoleChangedEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        GroupId = string.Empty;
        UserId = string.Empty;
        Role = string.Empty;
        ChangedAt = DateTime.UtcNow;
    }
}
