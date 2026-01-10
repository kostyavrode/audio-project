namespace AuthService.Domain.ValueObjects;

public class RefreshToken : IEquatable<RefreshToken>
{
    public string Token { get; private set; }
    
    public DateTime ExpiresAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool IsRevoked { get; private set; }

    private RefreshToken(string token, DateTime expiresAt, DateTime createdAt, bool isRevoked = false)
    {
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        IsRevoked = isRevoked;
    }
    
    public static RefreshToken FromDatabase(string token, DateTime expiresAt, DateTime createdAt, bool isRevoked)
    {
        return new RefreshToken(token, expiresAt, createdAt, isRevoked);
    }
    
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }
    
    public bool IsValid()
    {
        return !IsExpired() && !IsRevoked;
    }
    
    public void Revoke()
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException("Token is already revoked");
        }
    }
    
    public bool Equals(RefreshToken? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }
        
        return Token == other.Token;
    }
    
    public override bool Equals(object? obj)
    {
        return obj is RefreshToken other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return Token.GetHashCode();
    }
    
    public static bool operator ==(RefreshToken? left, RefreshToken? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }
        return left.Equals(right);
    }
    
    public static bool operator !=(RefreshToken? left, RefreshToken? right)
    {
        return !(left == right);
    }
}