using AuthService.Domain.Entities;
using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
    
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);
    
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}