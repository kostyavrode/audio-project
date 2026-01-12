using System.Text.Json;
using GroupsService.Domain.DomainEvents;
using GroupsService.Domain.Entities;
using GroupsService.Infrastructure.Outbox;

namespace GroupsService.Infrastructure.Extensions;

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
            .Select(domainEvent =>
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
                    IncludeFields = false,
                    WriteIndented = false
                };
                
                var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), jsonOptions);
                
                return new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    EventId = domainEvent.EventId,
                    EventType = domainEvent.GetType().Name,
                    Payload = payload,
                    Status = OutboxMessageStatus.Pending,
                    CreatedAt = domainEvent.OccurredAt
                };
            })
            .ToList();
        
        await dbContext.OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);
    }
}
