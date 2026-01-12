namespace ChatService.Infrastructure.Outbox;

public class OutboxPublisherSettings
{
    public const string SectionName = "OutboxPublisher";
    
    public int PollingIntervalSeconds { get; set; } = 5;
    
    public int BatchSize { get; set; } = 100;
    
    public int MaxRetryCount { get; set; } = 5;
    
    public string ExchangeName { get; set; } = "chat-events";
}