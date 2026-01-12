using System.Text.Json.Serialization;

namespace AudioService.Domain.DomainEvents;

public class AudioChannelDeletedEvent : IDomainEvent
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; }

    [JsonPropertyName("channelId")]
    [JsonInclude]
    public string ChannelId { get; set; } = null!;

    [JsonPropertyName("groupId")]
    [JsonInclude]
    public string GroupId { get; set; } = null!;

    [JsonPropertyName("deletedAt")]
    [JsonInclude]
    public DateTime DeletedAt { get; set; }

    public AudioChannelDeletedEvent(string channelId, string groupId, DateTime deletedAt)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        ChannelId = channelId;
        GroupId = groupId;
        DeletedAt = deletedAt;
    }

    public AudioChannelDeletedEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        ChannelId = string.Empty;
        GroupId = string.Empty;
        DeletedAt = DateTime.UtcNow;
    }
}
