namespace GroupsService.Domain.Exceptions;

public class InvalidGroupPasswordException : DomainException
{
    public InvalidGroupPasswordException(string message) : base(message)
    {
    }
}
