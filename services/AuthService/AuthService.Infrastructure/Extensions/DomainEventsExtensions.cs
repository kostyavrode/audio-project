using System.Text.Json;
using AuthService.Domain.DomainEvents;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Outbox;

namespace AuthService.Infrastructure.Extensions;

public static class DomainEventsExtensions
{
    public static async Task SaveDomainEventsToOutboxAsync(
        this BaseEntity entity,
        OutboxDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (entity.DomainEvents == null || !entity.DomainEvents.Any())
        {
            return;
        }
        
        var outboxMessages = entity.DomainEvents
            .Select(domainEvent => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventId = domainEvent.EventId,
                EventType = domainEvent.GetType().Name,
                Payload = JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                Status = OutboxMessageStatus.Pending,
                CreatedAt = domainEvent.OccurredAt
            })
            .ToList();
        
        await dbContext.OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);
    }
}