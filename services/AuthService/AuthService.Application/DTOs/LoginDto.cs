namespace AuthService.Application.DTOs;

public class LoginDto
{
    public string NickName { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty;
}