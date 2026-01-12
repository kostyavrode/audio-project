using System.Text.Json.Serialization;

namespace AudioService.Domain.DomainEvents;

public class UserLeftAudioChannelEvent : IDomainEvent
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

    [JsonPropertyName("leftAt")]
    [JsonInclude]
    public DateTime LeftAt { get; set; }

    public UserLeftAudioChannelEvent(string channelId, string userId, DateTime leftAt)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        ChannelId = channelId;
        UserId = userId;
        LeftAt = leftAt;
    }

    public UserLeftAudioChannelEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        ChannelId = string.Empty;
        UserId = string.Empty;
        LeftAt = DateTime.UtcNow;
    }
}
