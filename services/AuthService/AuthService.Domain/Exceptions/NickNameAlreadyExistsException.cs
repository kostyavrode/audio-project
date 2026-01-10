using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Exceptions;

public class NickNameAlreadyExistsException : DomainException
{
    public NickName NickName { get; }
    
    public NickNameAlreadyExistsException(NickName nickName) : base($"User with nickname '{nickName}' already exists")
    {
        NickName = nickName;
    }
}