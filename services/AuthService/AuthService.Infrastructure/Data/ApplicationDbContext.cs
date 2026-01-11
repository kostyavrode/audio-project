using AuthService.Domain.Entities;
using AuthService.Infrastructure.Extensions;
using AuthService.Infrastructure.Outbox;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && 
                        (e.State == EntityState.Modified || e.State == EntityState.Added));
    
        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;
        
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.MarkAsUpdated();
            }
        }
        
        // Сохраняем доменные события в Outbox
        var entitiesWithEvents = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();
        
        // Сначала сохраняем изменения в основную БД
        var result = await base.SaveChangesAsync(cancellationToken);
        
        // Затем сохраняем доменные события в Outbox
        // Получаем OutboxDbContext через HttpContext (если доступен) или создаем новый
        if (entitiesWithEvents.Any() && _httpContextAccessor?.HttpContext != null)
        {
            var outboxDbContext = _httpContextAccessor.HttpContext.RequestServices
                .GetRequiredService<OutboxDbContext>();
            
            foreach (var entity in entitiesWithEvents)
            {
                await entity.SaveDomainEventsToOutboxAsync(outboxDbContext, cancellationToken);
            }
            
            await outboxDbContext.SaveChangesAsync(cancellationToken);
        }
    
        return result;
    }
}