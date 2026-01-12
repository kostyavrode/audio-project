using ChatService.Domain.Entities;
using ChatService.Infrastructure.Events;
using ChatService.Infrastructure.Extensions;
using ChatService.Infrastructure.Outbox;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChatService.Infrastructure.Data;

public class ChatDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ChatDbContext(
        DbContextOptions<ChatDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<GroupMember> GroupMembers { get; set; } = null!;
    public DbSet<ProcessedEvent> ProcessedEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

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
        
        var entitiesWithEvents = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();
        
        var result = await base.SaveChangesAsync(cancellationToken);
        
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