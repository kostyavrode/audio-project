using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Api.Hubs;
using System.Security.Claims;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IHubContext<NotificationHub> hubContext,
        ILogger<AdminController> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(AdminStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<AdminStatsDto> GetStats()
    {
        // Проверка доступа - только для администратора (kiberkostya)
        var nickname = User.FindFirstValue("nickname");
        if (string.IsNullOrEmpty(nickname) || !nickname.Equals("kiberkostya", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Unauthorized admin stats access attempt by nickname: {Nickname}", nickname ?? "null");
            return Forbid();
        }

        try
        {
            // Получаем количество активных подключений из Hub
            var activeConnections = NotificationHub.GetActiveConnectionsCount();
            
            var stats = new AdminStatsDto
            {
                TotalConnections = activeConnections,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Admin stats requested by {Nickname}: {Connections} active connections", nickname, activeConnections);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin stats");
            return StatusCode(500, new { error = "Failed to get admin stats" });
        }
    }
}

public class AdminStatsDto
{
    public int TotalConnections { get; set; }
    public DateTime Timestamp { get; set; }
}
