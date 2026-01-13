using AudioService.Domain.Entities;
using AudioService.Infrastructure.Data;
using AudioService.Infrastructure.Events;
using AudioService.Infrastructure.Extensions;
using AudioService.Infrastructure.Outbox;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AudioService.Infrastructure.Data;

public class AudioDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IServiceProvider? _serviceProvider;
    private readonly ILogger<AudioDbContext>? _logger;

    public AudioDbContext(
        DbContextOptions<AudioDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null,
        IServiceProvider? serviceProvider = null,
        ILogger<AudioDbContext>? logger = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public DbSet<AudioChannel> AudioChannels { get; set; } = null!;
    public DbSet<AudioChannelParticipant> AudioChannelParticipants { get; set; } = null!;
    public DbSet<GroupMember> GroupMembers { get; set; } = null!;
    public DbSet<ProcessedEvent> ProcessedEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AudioDbContext).Assembly);
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
                foreach (var entity in entitiesWithEvents)
                {
                    await entity.SaveDomainEventsToOutboxAsync(outboxDbContext, cancellationToken);
                }

                await outboxDbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return result;
    }
}
