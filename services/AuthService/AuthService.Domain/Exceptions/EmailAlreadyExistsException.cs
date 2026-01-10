using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Exceptions;

public class EmailAlreadyExistsException : DomainException
{
    public Email Email { get; }
    
    public EmailAlreadyExistsException(Email email) : base($"User with email '{email}' already exists")
    {
        Email = email;
    }
}