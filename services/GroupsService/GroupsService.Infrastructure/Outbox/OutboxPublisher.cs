using System.Text.Json;
using GroupsService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GroupsService.Infrastructure.Outbox;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly OutboxPublisherSettings _settings;
    private readonly TimeSpan _pollingInterval;
    
    public OutboxPublisher(IServiceProvider serviceProvider, ILogger<OutboxPublisher> logger, IOptions<OutboxPublisherSettings> settings)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        
        _pollingInterval = TimeSpan.FromSeconds(_settings.PollingIntervalSeconds);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Publisher started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }
            
            await Task.Delay(_pollingInterval, stoppingToken);
        }
        
        _logger.LogInformation("Outbox Publisher stopped");
    }
    
    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var rabbitMqPublisher = scope.ServiceProvider.GetRequiredService<IRabbitMQPublisher>();
        
        var pendingMessages = await dbContext.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(_settings.BatchSize)
            .ToListAsync(cancellationToken);
        
        if (!pendingMessages.Any())
        {
            return;
        }
        
        _logger.LogInformation("Processing {Count} outbox messages", pendingMessages.Count);
        
        foreach (var message in pendingMessages)
        {
            try
            {
                _logger.LogInformation("Publishing event {EventId} of type {EventType} to exchange {Exchange} with routing key {RoutingKey}", 
                    message.EventId, message.EventType, _settings.ExchangeName, message.EventType);
                
                await rabbitMqPublisher.PublishAsync(
                    exchange: _settings.ExchangeName,
                    routingKey: message.EventType,
                    message: message.Payload,
                    cancellationToken
                );
                
                message.Status = OutboxMessageStatus.Published;
                message.PublishedAt = DateTime.UtcNow;
                
                _logger.LogInformation("Successfully published event {EventId} of type {EventType}", 
                    message.EventId, message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event {EventId}", message.EventId);
                
                message.RetryCount++;
                message.ErrorMessage = ex.Message;
                
                if (message.RetryCount >= _settings.MaxRetryCount)
                {
                    message.Status = OutboxMessageStatus.Failed;
                    _logger.LogWarning("Event {EventId} marked as Failed after {RetryCount} attempts",
                        message.EventId, message.RetryCount);
                }
            }
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
