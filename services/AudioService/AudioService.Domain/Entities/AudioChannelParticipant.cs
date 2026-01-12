using AudioService.Domain.Exceptions;

namespace AudioService.Domain.Entities;

public class AudioChannelParticipant : BaseEntity
{
    public string ChannelId { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public DateTime JoinedAt { get; private set; }
    public DateTime? LeftAt { get; private set; }

    private AudioChannelParticipant()
    {
    }

    public static AudioChannelParticipant Create(string id, string channelId, string userId)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainException("Participant ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new DomainException("Channel ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("User ID cannot be empty");
        }

        return new AudioChannelParticipant
        {
            Id = id,
            ChannelId = channelId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsLeft()
    {
        if (LeftAt != null)
        {
            throw new DomainException("Participant already left the channel");
        }

        LeftAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public bool IsActive => LeftAt == null;
}
