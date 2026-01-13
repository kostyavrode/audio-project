namespace AudioService.Infrastructure.ExternalServices;

public class JanusGatewaySettings
{
    public const string SectionName = "JanusGateway";

    public string BaseUrl { get; set; } = "http://localhost:8088";

    public string ApiSecret { get; set; } = string.Empty;
}
