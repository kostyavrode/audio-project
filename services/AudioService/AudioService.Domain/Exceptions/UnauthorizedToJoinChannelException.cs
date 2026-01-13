namespace AudioService.Domain.Exceptions;

public class UnauthorizedToJoinChannelException : DomainException
{
    public string ChannelId { get; }
    public string UserId { get; }

    public UnauthorizedToJoinChannelException(string channelId, string userId)
        : base($"User '{userId}' is not authorized to join channel '{channelId}'")
    {
        ChannelId = channelId;
        UserId = userId;
    }
}
