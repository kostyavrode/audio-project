using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ChatService.Infrastructure.Messaging;

public class RabbitMQConnectionFactory : IDisposable
{
    private readonly RabbitMQSettings _settings;
    private IConnection? _connection;
    private readonly object _lock = new();
    
    public RabbitMQConnectionFactory(IOptions<RabbitMQSettings> settings)
    {
        _settings = settings.Value;
    }
    
    public IConnection GetConnection()
    {
        if (_connection != null && _connection.IsOpen)
        {
            return _connection;
        }
        
        lock (_lock)
        {
            if (_connection != null && _connection.IsOpen)
            {
                return _connection;
            }
            
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
            return _connection;
        }
    }
    
    public void Dispose()
    {
        _connection?.Dispose();
    }
}