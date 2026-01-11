using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AuthService.Infrastructure.Messaging;

public class RabbitMQConnectionFactory : IDisposable
{
    private readonly RabbitMQSettings _settings;
    private IConnection? _connection;
    private readonly object _lock = new();
    
    public RabbitMQConnectionFactory(IOptions<RabbitMQSettings> settings)
    {
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
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

public class RabbitMQSettings
{
    public const string SectionName = "RabbitMQ";
    
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
}