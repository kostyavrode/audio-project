using AudioService.Domain.DomainEvents;
using AudioService.Domain.Exceptions;

namespace AudioService.Domain.Entities;

public class AudioChannel : BaseEntity
{
    public string GroupId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public long? JanusRoomId { get; private set; }

    private AudioChannel()
    {
    }

    public static AudioChannel Create(string id, string groupId, string name)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainException("Audio channel ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(groupId))
        {
            throw new DomainException("Group ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Audio channel name cannot be empty");
        }

        var trimmedName = name.Trim();

        if (trimmedName.Length > 100)
        {
            throw new DomainException("Audio channel name cannot exceed 100 characters");
        }

        var channel = new AudioChannel
        {
            Id = id,
            GroupId = groupId,
            Name = trimmedName,
            CreatedAt = DateTime.UtcNow
        };

        channel.AddDomainEvent(new AudioChannelCreatedEvent(channel.Id, channel.GroupId, channel.Name, channel.CreatedAt));

        return channel;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Audio channel name cannot be empty");
        }

        var trimmedName = name.Trim();

        if (trimmedName.Length > 100)
        {
            throw new DomainException("Audio channel name cannot exceed 100 characters");
        }

        Name = trimmedName;
        MarkAsUpdated();
    }

    public void SetJanusRoomId(long janusRoomId)
    {
        JanusRoomId = janusRoomId;
        MarkAsUpdated();
    }

    public void MarkForDeletion()
    {
        AddDomainEvent(new AudioChannelDeletedEvent(Id, GroupId, DateTime.UtcNow));
    }
}
