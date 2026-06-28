using GroupsService.Domain.Entities;
using GroupsService.Infrastructure.Extensions;
using GroupsService.Infrastructure.Outbox;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GroupsService.Infrastructure.Data;

public class GroupsDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IServiceProvider? _serviceProvider;
    private readonly ILogger<GroupsDbContext>? _logger;
    
    public GroupsDbContext(
        DbContextOptions<GroupsDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null,
        IServiceProvider? serviceProvider = null,
        ILogger<GroupsDbContext>? logger = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<GroupMember> GroupMembers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GroupsDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

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

        if (entitiesWithEvents.Any())
        {
            try
            {
                OutboxDbContext? outboxDbContext = null;

                if (_httpContextAccessor?.HttpContext != null)
                {
                    outboxDbContext = _httpContextAccessor.HttpContext.RequestServices
                        .GetRequiredService<OutboxDbContext>();
                }
                else if (_serviceProvider != null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    outboxDbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
                }

                if (outboxDbContext != null)
                {
                    var totalEvents = entitiesWithEvents.Sum(e => e.DomainEvents.Count);
                    _logger?.LogInformation("Saving {Count} domain events to outbox", totalEvents);

                    foreach (var entity in entitiesWithEvents)
                    {
                        await entity.SaveDomainEventsToOutboxAsync(outboxDbContext, cancellationToken);
                    }

                    await outboxDbContext.SaveChangesAsync(cancellationToken);
                    _logger?.LogInformation("Successfully saved {Count} domain events to outbox", totalEvents);
                }
                else
                {
                    _logger?.LogWarning("Could not resolve OutboxDbContext. Domain events were not saved to outbox.");
                }
            }
            catch (Exception ex)
            {
                // The main entity changes (e.g. member role) are already committed above.
                // A failure to publish the outbox event must not turn a successful
                // role/data change into an error response for the caller.
                _logger?.LogError(ex, "Failed to save domain events to outbox. Main changes were already committed.");
            }
        }

        return result;
    }
}