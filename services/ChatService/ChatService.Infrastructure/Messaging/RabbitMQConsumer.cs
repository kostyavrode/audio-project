using System.Text;
using System.Text.Json;
using ChatService.Domain.Entities;
using ChatService.Domain.Interfaces;
using ChatService.Infrastructure.Data;
using ChatService.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatService.Infrastructure.Messaging;

public class RabbitMQConsumer : BackgroundService, IRabbitMQConsumer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private const string QueueName = "chat-service.groups-events";
    private const string ExchangeName = "groups-events";
    
    public RabbitMQConsumer(
        IServiceProvider serviceProvider,
        IOptions<RabbitMQSettings> settings,
        ILogger<RabbitMQConsumer> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartAsync(stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        
        await StopAsync(stoppingToken);
    }
    
    public new Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };
            
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false
            );
            
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );
            
            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: "UserJoinedGroupEvent"
            );
            
            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: "UserLeftGroupEvent"
            );
            
            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: "GroupDeletedEvent"
            );
            
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
            
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;
                
                try
                {
                    await ProcessMessageAsync(routingKey, message, cancellationToken);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from RabbitMQ. RoutingKey: {RoutingKey}", routingKey);
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };
            
            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer
            );
            
            _logger.LogInformation("RabbitMQ Consumer started. Listening to queue: {QueueName} on exchange: {ExchangeName}", QueueName, ExchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMQ Consumer");
            throw;
        }
        
        return Task.CompletedTask;
    }
    
    public new Task StopAsync(CancellationToken cancellationToken = default)
    {
        _channel?.Close();
        _connection?.Close();
        _logger.LogInformation("RabbitMQ Consumer stopped");
        return Task.CompletedTask;
    }
    
    private async Task ProcessMessageAsync(string routingKey, string message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var groupMemberRepository = scope.ServiceProvider.GetRequiredService<IGroupMemberRepository>();
        
        _logger.LogDebug("Received message with routing key: {RoutingKey}. Message: {Message}", routingKey, message);
        
        var eventData = JsonSerializer.Deserialize<JsonElement>(message);
        
        if (!eventData.TryGetProperty("eventId", out var eventIdElement))
        {
            _logger.LogWarning("Event message missing eventId. RoutingKey: {RoutingKey}, Message: {Message}", routingKey, message);
            return;
        }
        
        if (!Guid.TryParse(eventIdElement.GetString(), out var eventId))
        {
            _logger.LogWarning("Invalid eventId format. RoutingKey: {RoutingKey}, Message: {Message}", routingKey, message);
            return;
        }
        
        var processedEvent = await dbContext.ProcessedEvents
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        
        if (processedEvent != null)
        {
            _logger.LogDebug("Event {EventId} already processed. Skipping.", eventId);
            return;
        }
        
        switch (routingKey)
        {
            case "UserJoinedGroupEvent":
                await HandleUserJoinedGroupEventAsync(eventData, groupMemberRepository, dbContext, eventId, cancellationToken);
                break;
            
            case "UserLeftGroupEvent":
                await HandleUserLeftGroupEventAsync(eventData, groupMemberRepository, dbContext, eventId, cancellationToken);
                break;
            
            case "GroupDeletedEvent":
                await HandleGroupDeletedEventAsync(eventData, groupMemberRepository, dbContext, eventId, cancellationToken);
                break;
            
            default:
                _logger.LogWarning("Unknown routing key: {RoutingKey}", routingKey);
                break;
        }
    }
    
    private async Task HandleUserJoinedGroupEventAsync(
        JsonElement eventData,
        IGroupMemberRepository groupMemberRepository,
        ChatDbContext dbContext,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing UserJoinedGroupEvent. EventData: {EventData}", eventData.GetRawText());
        
        if (!eventData.TryGetProperty("groupId", out var groupIdElement) ||
            !eventData.TryGetProperty("userId", out var userIdElement))
        {
            _logger.LogWarning("Invalid UserJoinedGroupEvent data: missing groupId or userId. Available properties: {Properties}", 
                string.Join(", ", eventData.EnumerateObject().Select(p => p.Name)));
            return;
        }
        
        var groupId = groupIdElement.GetString();
        var userId = userIdElement.GetString();
        
        if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Invalid UserJoinedGroupEvent data: empty groupId or userId. GroupId: {GroupId}, UserId: {UserId}", 
                groupId, userId);
            return;
        }
        
        string? roleStr = null;
        if (eventData.TryGetProperty("role", out var roleElement))
        {
            roleStr = roleElement.GetString();
        }
        
        var exists = await groupMemberRepository.ExistsAsync(groupId, userId, cancellationToken);
        if (exists)
        {
            _logger.LogDebug("GroupMember already exists. GroupId: {GroupId}, UserId: {UserId}", groupId, userId);
        }
        else
        {
            var role = Enum.Parse<GroupMemberRole>(roleStr ?? "Member");
            var groupMember = GroupMember.Create(
                Guid.NewGuid().ToString(),
                groupId,
                userId,
                role
            );
            
            await groupMemberRepository.AddAsync(groupMember, cancellationToken);
        }
        
        await SaveProcessedEventAsync(dbContext, eventId, "UserJoinedGroupEvent", cancellationToken);
        await groupMemberRepository.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Processed UserJoinedGroupEvent. GroupId: {GroupId}, UserId: {UserId}", groupId, userId);
    }
    
    private async Task HandleUserLeftGroupEventAsync(
        JsonElement eventData,
        IGroupMemberRepository groupMemberRepository,
        ChatDbContext dbContext,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (!eventData.TryGetProperty("groupId", out var groupIdElement) ||
            !eventData.TryGetProperty("userId", out var userIdElement))
        {
            _logger.LogWarning("Invalid UserLeftGroupEvent data: missing groupId or userId");
            return;
        }
        
        var groupId = groupIdElement.GetString();
        var userId = userIdElement.GetString();
        
        if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Invalid UserLeftGroupEvent data");
            return;
        }
        
        var groupMember = await groupMemberRepository.GetByGroupIdAndUserIdAsync(groupId, userId, cancellationToken);
        if (groupMember != null)
        {
            await groupMemberRepository.RemoveAsync(groupMember, cancellationToken);
        }
        
        await SaveProcessedEventAsync(dbContext, eventId, "UserLeftGroupEvent", cancellationToken);
        await groupMemberRepository.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Processed UserLeftGroupEvent. GroupId: {GroupId}, UserId: {UserId}", groupId, userId);
    }
    
    private async Task HandleGroupDeletedEventAsync(
        JsonElement eventData,
        IGroupMemberRepository groupMemberRepository,
        ChatDbContext dbContext,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (!eventData.TryGetProperty("groupId", out var groupIdElement))
        {
            _logger.LogWarning("Invalid GroupDeletedEvent data: missing groupId");
            return;
        }
        
        var groupId = groupIdElement.GetString();
        
        if (string.IsNullOrEmpty(groupId))
        {
            _logger.LogWarning("Invalid GroupDeletedEvent data");
            return;
        }
        
        await groupMemberRepository.RemoveByGroupIdAsync(groupId, cancellationToken);
        
        await SaveProcessedEventAsync(dbContext, eventId, "GroupDeletedEvent", cancellationToken);
        await groupMemberRepository.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Processed GroupDeletedEvent. GroupId: {GroupId}", groupId);
    }
    
    private async Task SaveProcessedEventAsync(
        ChatDbContext dbContext,
        Guid eventId,
        string eventType,
        CancellationToken cancellationToken)
    {
        var processedEvent = new ProcessedEvent
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = eventType,
            ProcessedAt = DateTime.UtcNow
        };
        
        await dbContext.ProcessedEvents.AddAsync(processedEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    
    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}