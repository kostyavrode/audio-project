namespace ChatService.Infrastructure.Messaging;

public interface IRabbitMQPublisher
{
    Task PublishAsync(string exchange, string routingKey, string message, CancellationToken cancellationToken = default);
}