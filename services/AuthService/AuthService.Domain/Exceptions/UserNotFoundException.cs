using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Exceptions;

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(string userId) : base($"User with id '{userId}' was not found")
    {
    }
    
    public UserNotFoundException(Email email) : base($"User with email '{email}' was not found")
    {
    }
}