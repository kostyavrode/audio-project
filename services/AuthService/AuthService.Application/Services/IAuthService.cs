using AuthService.Application.DTOs;

namespace AuthService.Application.Services;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default);
    
    Task<UserDto> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default);
    
    Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);
    
    Task<UserDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    
    Task LogoutAsync(string userId, CancellationToken cancellationToken = default);
}