namespace AudioService.Domain.Exceptions;

public class UnauthorizedToCreateChannelException : DomainException
{
    public string GroupId { get; }
    public string UserId { get; }

    public UnauthorizedToCreateChannelException(string groupId, string userId)
        : base($"User '{userId}' is not authorized to create channels in group '{groupId}'")
    {
        GroupId = groupId;
        UserId = userId;
    }
}
