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
                    var eventTypes = entity.DomainEvents.Select(e => e.GetType().Name).ToList();
                    _logger?.LogInformation("Entity {EntityType} (Id: {EntityId}) has {EventCount} events: {EventTypes}", 
                        entity.GetType().Name, entity.Id, entity.DomainEvents.Count, string.Join(", ", eventTypes));
                    
                    foreach (var domainEvent in entity.DomainEvents)
                    {
                        var jsonOptions = new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
                            WriteIndented = false
                        };
                        var testJson = System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), jsonOptions);
                        _logger?.LogInformation("Event {EventType} serialized JSON: {Json}", 
                            domainEvent.GetType().Name, testJson);
                    }
                    
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
    
        return result;
    }
}