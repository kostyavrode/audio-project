namespace AudioService.Domain.Exceptions;

public class AudioChannelNotFoundException : DomainException
{
    public string ChannelId { get; }

    public AudioChannelNotFoundException(string channelId)
        : base($"Audio channel with ID '{channelId}' was not found")
    {
        ChannelId = channelId;
    }
}
