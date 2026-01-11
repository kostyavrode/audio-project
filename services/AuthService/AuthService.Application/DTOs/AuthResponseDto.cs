namespace AuthService.Application.DTOs;

public class AuthResponseDto
{
    public UserDto User { get; set; } = null!;
    
    public string Message { get; set; } = string.Empty;
}