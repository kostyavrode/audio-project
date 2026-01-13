namespace AuthService.Application.DTOs;

public class RegisterDto
{
    public string? Email { get; set; }
    
    public string NickName { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty;
    
    public string ConfirmPassword { get; set; } = string.Empty;
}