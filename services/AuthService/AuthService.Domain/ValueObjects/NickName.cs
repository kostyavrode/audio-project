using System.Text.RegularExpressions;

namespace AuthService.Domain.ValueObjects;

public class NickName : IEquatable<NickName>
{
    public string Value { get; private set; }
    
    private NickName(string value)
    {
        Value = value;
    }
    
    public static implicit operator string(NickName nickName) => nickName.Value;
    
    public static explicit operator NickName(string nickName) => Create(nickName);
    
    public override string ToString() => Value;
    
    public static NickName Create(string nickName)
    {
        if (string.IsNullOrWhiteSpace(nickName))
            throw new ArgumentException("NickName cannot be null or empty", nameof(nickName));
        
        nickName = nickName.Trim();
        
        if (nickName.Length < 1)
            throw new ArgumentException("NickName must be at least 1 characters long", nameof(nickName));
        
        if (nickName.Length > 30)
            throw new ArgumentException("NickName cannot exceed 30 characters", nameof(nickName));
        
        var nickNameRegex = new Regex(@"^[a-zA-Zа-яА-ЯёЁ0-9_-]+$", RegexOptions.Compiled);
        
        if (!nickNameRegex.IsMatch(nickName))
            throw new ArgumentException("NickName can only contain letters, numbers, underscores, and hyphens", nameof(nickName));
        
        if (nickName.StartsWith("-") || nickName.StartsWith("_") ||
            nickName.EndsWith("-") || nickName.EndsWith("_"))
        {
            throw new ArgumentException("NickName cannot start or end with hyphen or underscore", nameof(nickName));
        }
        
        return new NickName(nickName);
    }
    
    public bool Equals(NickName? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
    }
    
    public override bool Equals(object? obj)
    {
        return obj is NickName other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }
    
    public static bool operator ==(NickName? left, NickName? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }
    
    public static bool operator !=(NickName? left, NickName? right)
    {
        return !(left == right);
    }
}