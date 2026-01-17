using System.Security.Claims;
using AuthService.Application.DTOs;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.ValueObjects;
using AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly CookieSettings _cookieSettings;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, JwtTokenGenerator jwtTokenGenerator,
        IOptions<CookieSettings> cookieSettings, IOptions<JwtSettings> jwtSettings, ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _cookieSettings = cookieSettings.Value;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto registerDto, CancellationToken cancellationToken = default)
    {
        var userDto = await _authService.RegisterAsync(registerDto, cancellationToken);
        
        var email = Email.Create(userDto.Email);
        var nickName = NickName.Create(userDto.NickName);
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(userDto.Id, email, nickName);
        var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        
        await _authService.SetRefreshTokenAsync(userDto.Id, refreshTokenString, refreshTokenExpiresAt, cancellationToken);
        
        SetCookie(_cookieSettings.AccessTokenCookieName, accessToken, TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpirationMinutes));
        SetCookie(_cookieSettings.RefreshTokenCookieName, refreshTokenString, TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays));
        
        return Ok(userDto);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto loginDto,
        CancellationToken cancellationToken = default)
    {
        var userDto = await _authService.LoginAsync(loginDto, cancellationToken);
        
        var email = Email.Create(userDto.Email);
        var nickName = NickName.Create(userDto.NickName);
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(userDto.Id, email, nickName);
        var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        
        await _authService.SetRefreshTokenAsync(userDto.Id, refreshTokenString, refreshTokenExpiresAt, cancellationToken);
        
        SetCookie(_cookieSettings.AccessTokenCookieName, accessToken, 
            TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpirationMinutes));
        SetCookie(_cookieSettings.RefreshTokenCookieName, refreshTokenString, 
            TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays));
        
        return Ok(userDto);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> RefreshToken(CancellationToken cancellationToken = default)
    {
        var refreshToken = Request.Cookies[_cookieSettings.RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { error = "Refresh token not found" });
        }
        
        var userDto = await _authService.RefreshTokenAsync(refreshToken, cancellationToken);
        
        var email = Email.Create(userDto.Email);
        var nickName = NickName.Create(userDto.NickName);
        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(userDto.Id, email, nickName);
        var newRefreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();
        var newRefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        await _authService.SetRefreshTokenAsync(userDto.Id, newRefreshTokenString, newRefreshTokenExpiresAt, cancellationToken);

        SetCookie(_cookieSettings.AccessTokenCookieName, newAccessToken, 
            TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpirationMinutes));
        SetCookie(_cookieSettings.RefreshTokenCookieName, newRefreshTokenString, 
            TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays));
        
        return Ok(userDto);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User id not found in token" });
        }
        
        var userDto = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        
        return Ok(userDto);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User id not found in token" });
        }
        
        await _authService.LogoutAsync(userId, cancellationToken);
        
        Response.Cookies.Delete(_cookieSettings.AccessTokenCookieName);
        Response.Cookies.Delete(_cookieSettings.RefreshTokenCookieName);
        
        _logger.LogInformation("User logged out: {UserId}", userId);
        
        return Ok(new { message = "Logged out successfully" });
    }
    
    private void SetCookie(string name, string value, TimeSpan expiration)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = _cookieSettings.HttpOnly,
            Secure = _cookieSettings.Secure,
            SameSite = _cookieSettings.SameSite switch
            {
                "Strict" => SameSiteMode.Strict,
                "Lax" => SameSiteMode.Lax,
                "None" => SameSiteMode.None,
                _ => SameSiteMode.Lax
            },
            Expires = DateTimeOffset.UtcNow.Add(expiration),
            Path = "/"
        };
        
        Response.Cookies.Append(name, value, cookieOptions);
    }
}