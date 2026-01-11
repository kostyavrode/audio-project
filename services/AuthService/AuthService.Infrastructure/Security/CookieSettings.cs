namespace AuthService.Infrastructure.Security;

public class CookieSettings
{
    public const string SectionName = "CookieSettings";
    
    public bool HttpOnly { get; set; } = true;
    
    public bool Secure { get; set; } = false;
    
    public string SameSite { get; set; } = "Lax";
    
    public string AccessTokenCookieName { get; set; } = "access_token";
    
    public string RefreshTokenCookieName { get; set; } = "refresh_token";
}