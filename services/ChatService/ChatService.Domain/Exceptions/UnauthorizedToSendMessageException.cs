namespace ChatService.Domain.Exceptions;

public class UnauthorizedToSendMessageException : DomainException
{
    public UnauthorizedToSendMessageException(string message) : base(message)
    {
    }
}
