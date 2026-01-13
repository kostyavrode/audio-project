using System.Text.Json.Serialization;

namespace AudioService.Domain.DomainEvents;

public class AudioChannelCreatedEvent : IDomainEvent
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

    [JsonPropertyName("name")]
    [JsonInclude]
    public string Name { get; set; } = null!;

    [JsonPropertyName("createdAt")]
    [JsonInclude]
    public DateTime CreatedAt { get; set; }

    public AudioChannelCreatedEvent(string channelId, string groupId, string name, DateTime createdAt)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        ChannelId = channelId;
        GroupId = groupId;
        Name = name;
        CreatedAt = createdAt;
    }

    public AudioChannelCreatedEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        ChannelId = string.Empty;
        GroupId = string.Empty;
        Name = string.Empty;
        CreatedAt = DateTime.UtcNow;
    }
}
