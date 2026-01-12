namespace ChatService.Domain.Exceptions;

public class MessageNotFoundException : DomainException
{
    public MessageNotFoundException(string message) : base(message)
    {
    }
}
