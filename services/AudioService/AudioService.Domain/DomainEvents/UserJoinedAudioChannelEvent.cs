using System.Text.Json.Serialization;

namespace AudioService.Domain.DomainEvents;

public class UserJoinedAudioChannelEvent : IDomainEvent
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; }

    [JsonPropertyName("channelId")]
    [JsonInclude]
    public string ChannelId { get; set; } = null!;

    [JsonPropertyName("userId")]
    [JsonInclude]
    public string UserId { get; set; } = null!;

    [JsonPropertyName("joinedAt")]
    [JsonInclude]
    public DateTime JoinedAt { get; set; }

    public UserJoinedAudioChannelEvent(string channelId, string userId, DateTime joinedAt)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        ChannelId = channelId;
        UserId = userId;
        JoinedAt = joinedAt;
    }

    public UserJoinedAudioChannelEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        ChannelId = string.Empty;
        UserId = string.Empty;
        JoinedAt = DateTime.UtcNow;
    }
}
