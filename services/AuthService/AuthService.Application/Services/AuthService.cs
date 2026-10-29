using AuthService.Application.DTOs;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository, 
        IPasswordHasher<User> passwordHasher,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserDto> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default)
    {
        var email = Email.Create(registerDto.Email);
        var nickName = NickName.Create(registerDto.NickName);

        if (email.HasValue && await _userRepository.ExistsByEmailAsync(email, cancellationToken: cancellationToken))
        {
            throw new EmailAlreadyExistsException(email);
        }

        if (await _userRepository.ExistsByNickNameAsync(nickName, cancellationToken: cancellationToken))
        {
            throw new NickNameAlreadyExistsException(nickName);
        }

        User tempUser = new User();
        
        var passwordHash = _passwordHasher.HashPassword(tempUser, registerDto.Password);
        var userId = Guid.NewGuid().ToString();
        var user = User.Register(userId, email, nickName, passwordHash);
        
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        
        user.ClearDomainEvents();
        
        _logger.LogInformation("User registered: {UserId}, NickName: {NickName}", userId, nickName.Value);

        return MapToUserDto(user);
    }
    
    public async Task<UserDto> CreateGuestAsync(CancellationToken cancellationToken = default)
    {
        // Имя вида «Гость-4821»; если занято - пробуем другое, после нескольких неудач берём длиннее
        NickName? nickName = null;
        for (var attempt = 0; attempt < 12 && nickName == null; attempt++)
        {
            var number = attempt < 8
                ? Random.Shared.Next(1000, 10000)
                : Random.Shared.Next(100000, 1000000);
            var candidate = NickName.Create($"Гость-{number}");
            if (!await _userRepository.ExistsByNickNameAsync(candidate, cancellationToken: cancellationToken))
            {
                nickName = candidate;
            }
        }
        if (nickName == null)
        {
            throw new InvalidOperationException("Could not pick a free guest name");
        }

        // Пароль, который никто не знает: войти в гостевой аккаунт можно только по его cookie
        var unusablePassword = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var passwordHash = _passwordHasher.HashPassword(new User(), unusablePassword);
        var userId = Guid.NewGuid().ToString();
        var user = User.RegisterGuest(userId, nickName, passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        user.ClearDomainEvents();

        _logger.LogInformation("Guest created: {UserId}, NickName: {NickName}", userId, nickName.Value);
        return MapToUserDto(user);
    }

    public async Task<UserDto> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
    {
        var nickName = NickName.Create(loginDto.NickName);
        
        var user = await _userRepository.GetByNickNameAsync(nickName, cancellationToken);
        
        // В гостевой аккаунт по паролю не входят
        if (user == null || user.IsGuest)
        {
            throw new InvalidCredentialsException();
        }
        
        var tempUser = new User();
        var verificationResult = _passwordHasher.VerifyHashedPassword(tempUser, user.PasswordHash, loginDto.Password);
        
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }
        
        _logger.LogInformation("User logged in: {UserId}, NickName: {NickName}", user.Id, nickName.Value);
        
        return MapToUserDto(user);
    }
        
    public async Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }
        
        return MapToUserDto(user);
    }
    
    public async Task<UserDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidCredentialsException("Refresh token not found");
        }
        
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken, cancellationToken);
        if (user == null || user.RefreshToken == null)
        {
            throw new InvalidCredentialsException("Invalid or expired refresh token");
        }
        
        if (user.RefreshToken.IsExpired() || user.RefreshToken.IsRevoked)
        {
            throw new InvalidCredentialsException("Refresh token has expired or been revoked");
        }
        
        return MapToUserDto(user);
    }
    
    public async Task SetRefreshTokenAsync(string userId, string refreshToken, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }
        
        var refreshTokenValueObject = RefreshToken.Create(refreshToken, expiresAt);
        user.SetRefreshToken(refreshTokenValueObject);
        
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<string> IssueRefreshTokenAsync(string userId, string newRefreshToken, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        // Действующий токен не заменяем, а продлеваем. У пользователя хранится один refresh-токен,
        // и его замена при каждом обновлении/входе выкидывала из аккаунта:
        //  - если ответ на refresh не дошёл до браузера (моргнула сеть, закрыли вкладку) -
        //    в cookie оставался старый токен, которого в базе уже нет;
        //  - если две вкладки обновлялись одновременно - вторая приходила с уже заменённым токеном;
        //  - если человек входил со второго устройства - первое теряло сессию.
        var token = user.RefreshToken != null && user.RefreshToken.IsValid()
            ? user.RefreshToken.Token
            : newRefreshToken;

        user.SetRefreshToken(RefreshToken.Create(token, expiresAt));

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }
        
        user.RevokeRefreshToken();
        
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("User logged out: {UserId}", userId);
    }
    
    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email?.Value,
            NickName = user.NickName.Value,
            CreatedAt = user.CreatedAt,
            IsGuest = user.IsGuest
        };
    }
}