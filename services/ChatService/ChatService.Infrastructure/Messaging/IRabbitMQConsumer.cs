namespace ChatService.Infrastructure.Messaging;

public interface IRabbitMQConsumer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}