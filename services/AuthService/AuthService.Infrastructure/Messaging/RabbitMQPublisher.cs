using System.Text;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Messaging;

public class RabbitMQPublisher : IRabbitMQPublisher, IDisposable
{
    private readonly RabbitMQConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMQPublisher> _logger;
    private IModel? _channel;
    
    public RabbitMQPublisher(RabbitMQConnectionFactory connectionFactory, ILogger<RabbitMQPublisher> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    private IModel GetChannel()
    {
        if (_channel != null && !_channel.IsClosed)
        {
            return _channel;
        }
        
        var connection = _connectionFactory.GetConnection();
        _channel = connection.CreateModel();
        
        return _channel;
    }
    
    public Task PublishAsync(string exchange, string routingKey, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = GetChannel();

            channel.ExchangeDeclare(
                exchange: exchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false
            );
            
            var body = Encoding.UTF8.GetBytes(message);
            
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            
            channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body
            );
            
            _logger.LogDebug("Published message to exchange '{Exchange}' with routing key '{RoutingKey}'",
                exchange, routingKey);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to RabbitMQ");
            throw;
        }
    }
    
    public void Dispose()
    {
        _channel?.Dispose();
    }
}