using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    
    public UserRepository(ApplicationDbContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
    
        _context = context;
    }
    
    public async Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }
    
    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        // EF Core автоматически применяет конверсию из HasConversion
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }
    
    
    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        if (email == null || !email.HasValue)
            return false;
        return await _context.Users.AnyAsync(u => u.Email != null && u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByNickNameAsync(NickName nickName, CancellationToken cancellationToken = default)
    {
        // Используем прямое сравнение через Value Object (EF Core применит конверсию)
        // NickName сравнивается case-insensitive по логике самого Value Object
        var allUsers = await _context.Users.ToListAsync(cancellationToken);
        return allUsers.FirstOrDefault(u => u.NickName == nickName);
    }
    
    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;
    
        return await _context.Users
            .FirstOrDefaultAsync(u => 
                    u.RefreshToken != null && 
                    u.RefreshToken.Token == refreshToken,
                cancellationToken);
    }
    
    public async Task<bool> ExistsByNickNameAsync(NickName nickName, CancellationToken cancellationToken = default)
    {
        // Используем прямое сравнение через Value Object (EF Core применит конверсию)
        // NickName сравнивается case-insensitive по логике самого Value Object
        var allUsers = await _context.Users.ToListAsync(cancellationToken);
        return allUsers.Any(u => u.NickName == nickName);
    }
    
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        await _context.Users.AddAsync(user, cancellationToken);
    }
    
    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        _context.Users.Update(user);
        
        return Task.CompletedTask;
    }
    
    public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        _context.Users.Remove(user);
        
        return Task.CompletedTask;
    }
    
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}