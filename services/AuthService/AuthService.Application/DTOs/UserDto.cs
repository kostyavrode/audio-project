namespace AuthService.Application.DTOs;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string NickName { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
}