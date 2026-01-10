namespace AuthService.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    
    public string SecretKey { get; set; } = string.Empty;
    
    public string Issuer { get; set; } = String.Empty;
    
    public string Audience { get; set; } = String.Empty;
    
    public int AccessTokenExpirationMinutes { get; set; } = 30;
    
    public int RefreshTokenExpirationDays { get; set; } = 7;
}