using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Infrastructure.Messaging;

public class RabbitMQConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private const string QueueName = "notification-service.chat-messages";
    private const string ExchangeName = "chat-messages";
    
    public RabbitMQConsumer(
        IServiceProvider serviceProvider,
        IOptions<RabbitMQSettings> settings,
        ILogger<RabbitMQConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
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
    
    private Task StartAsync(CancellationToken cancellationToken = default)
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
                routingKey: "ChatMessage"
            );
            
            _channel.QueueBind(
                queue: QueueName,
                exchange: "audio-events",
                routingKey: "AudioParticipantJoined"
            );
            
            _channel.QueueBind(
                queue: QueueName,
                exchange: "audio-events",
                routingKey: "AudioParticipantLeft"
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
    
    private Task StopAsync(CancellationToken cancellationToken = default)
    {
        _channel?.Close();
        _connection?.Close();
        _logger.LogInformation("RabbitMQ Consumer stopped");
        return Task.CompletedTask;
    }
    
    private async Task ProcessMessageAsync(string routingKey, string message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        
        _logger.LogDebug("Received message with routing key: {RoutingKey}. Message: {Message}", routingKey, message);
        
        if (routingKey == "ChatMessage")
        {
            var messageDto = JsonSerializer.Deserialize<JsonElement>(message);
            
            if (messageDto.TryGetProperty("groupId", out var groupIdElement))
            {
                var groupId = groupIdElement.GetString();
                if (!string.IsNullOrEmpty(groupId))
                {
                    await notificationService.SendChatMessageAsync(groupId, messageDto);
                    _logger.LogInformation("Chat message forwarded to NotificationService for group {GroupId}", groupId);
                }
            }
        }
        else if (routingKey == "AudioParticipantJoined")
        {
            var eventData = JsonSerializer.Deserialize<JsonElement>(message);
            
            if (eventData.TryGetProperty("groupId", out var groupIdElement) &&
                eventData.TryGetProperty("channelId", out var channelIdElement) &&
                eventData.TryGetProperty("userId", out var userIdElement) &&
                eventData.TryGetProperty("displayName", out var displayNameElement) &&
                eventData.TryGetProperty("participantId", out var participantIdElement))
            {
                var groupId = groupIdElement.GetString();
                var channelId = channelIdElement.GetString();
                var userId = userIdElement.GetString();
                var displayName = displayNameElement.GetString();
                var participantId = participantIdElement.GetInt64();
                
                if (!string.IsNullOrEmpty(groupId) && !string.IsNullOrEmpty(channelId) && 
                    !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(displayName))
                {
                    await notificationService.SendAudioParticipantJoinedAsync(groupId, channelId, userId, displayName, participantId);
                    _logger.LogInformation("Audio participant joined notification sent for group {GroupId}", groupId);
                }
            }
        }
        else if (routingKey == "AudioParticipantLeft")
        {
            var eventData = JsonSerializer.Deserialize<JsonElement>(message);
            
            if (eventData.TryGetProperty("groupId", out var groupIdElement) &&
                eventData.TryGetProperty("channelId", out var channelIdElement) &&
                eventData.TryGetProperty("userId", out var userIdElement) &&
                eventData.TryGetProperty("participantId", out var participantIdElement))
            {
                var groupId = groupIdElement.GetString();
                var channelId = channelIdElement.GetString();
                var userId = userIdElement.GetString();
                var participantId = participantIdElement.GetInt64();
                
                if (!string.IsNullOrEmpty(groupId) && !string.IsNullOrEmpty(channelId) && !string.IsNullOrEmpty(userId))
                {
                    await notificationService.SendAudioParticipantLeftAsync(groupId, channelId, userId, participantId);
                    _logger.LogInformation("Audio participant left notification sent for group {GroupId}", groupId);
                }
            }
        }
    }
}
