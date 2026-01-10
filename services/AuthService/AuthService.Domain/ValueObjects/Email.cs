using System.Text.RegularExpressions;

namespace AuthService.Domain.ValueObjects;

public class Email : IEquatable<Email>
{
    public string Value { get; private set; }

    private Email(string value)
    {
        Value = value;
    }
    
    public static implicit operator string(Email email) => email.Value;
    
    public static explicit operator Email(string email) => Create(email);

    public override string ToString() => Value;
    
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or whitespace.", nameof(email));
        }
        
        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        if (!emailRegex.IsMatch(email))
        {
            throw new ArgumentException("Email is not valid.", nameof(email));
        }

        if (email.Length > 254)
        {
            throw new ArgumentException("Email cannot exceed 254 characters", nameof(email));
        }

        return new Email(email);
    }
    
    public bool Equals(Email? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return Value == other.Value;
    }
    
    public override bool Equals(object? obj)
    {
        return obj is Email other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
    
    public static bool operator ==(Email? left, Email? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }
    
    public static bool operator !=(Email? left, Email? right)
    {
        return !(left == right);
    }
}