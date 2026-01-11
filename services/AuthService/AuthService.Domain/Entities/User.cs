using AuthService.Domain.DomainEvents;
using AuthService.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Domain.Entities;

public class User : BaseEntity
{
    public Email Email { get; private set; }
    
    public string PasswordHash { get; private set; } = string.Empty;
    
    public NickName NickName { get; private set; } = null!;
    
    public RefreshToken? RefreshToken { get; private set; }
    
    public static User Register(string id, Email email, NickName nickName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id cannot be null or empty", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash cannot be null or empty", nameof(passwordHash));
        }

        var user = new User
        {
            Id = id,
            Email = email,
            NickName = nickName,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
        
        var registeredEvent = new UserRegisteredEvent(user.Id, user.Email, user.NickName, user.CreatedAt);
        user.AddDomainEvent(registeredEvent);
        
        return user;
    }
    
    public void SetRefreshToken(RefreshToken refreshToken)
    {
        if (refreshToken is null)
        {
            throw new ArgumentNullException(nameof(refreshToken));
        }

        RefreshToken = refreshToken;
        MarkAsUpdated();
    }
    
    public void RevokeRefreshToken()
    {
        if (RefreshToken is null)
            return;
        
        RefreshToken = RefreshToken.FromDatabase(
            RefreshToken.Token,
            RefreshToken.ExpiresAt,
            RefreshToken.CreatedAt,
            isRevoked: true
        );
        
        MarkAsUpdated();
    }
    
    public void UpdateEmail(Email newEmail)
    {
        if (newEmail is null)
        {
            throw new ArgumentNullException(nameof(newEmail));
        }

        Email = newEmail;
        MarkAsUpdated();
    }
    
    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new ArgumentException("Password hash cannot be null or empty", nameof(newPasswordHash));
        }

        PasswordHash = newPasswordHash;
        MarkAsUpdated();
    }
    
    public void UpdateNickName(NickName newNickName)
    {
        if (newNickName is null)
            throw new ArgumentNullException(nameof(newNickName));
        
        NickName = newNickName;
        MarkAsUpdated();
    }
    
    public bool VerifyPassword(string passwordHash)
    {
        return PasswordHash == passwordHash;
    }
}