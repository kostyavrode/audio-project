namespace NotificationService.Domain.Entities;

public class Notification : BaseEntity
{
    public string Type { get; set; } = string.Empty;
    
    public string PayLoad { get; set; } = string.Empty;
    
    public string Recipients { get; set; } = string.Empty;
    
    public string Status { get; set; } = "Pending";
    
    public int RetryCount { get; set; } = 0;
    
    public DateTime ProcessedAt { get; set; } = DateTime.Now;
    
    public string? Error  { get; set; }
}