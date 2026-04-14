using Common.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly SignalRPresenceMetrics _presenceMetrics;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        SignalRPresenceMetrics presenceMetrics,
        ILogger<AdminController> logger)
    {
        _presenceMetrics = presenceMetrics;
        _logger = logger;
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(AdminStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<AdminStatsDto> GetStats()
    {
        var nickname = User.FindFirstValue("nickname");
        if (string.IsNullOrEmpty(nickname) || !nickname.Equals("kiberkostya", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Unauthorized admin stats access attempt by nickname: {Nickname}", nickname ?? "null");
            return Forbid();
        }

        try
        {
            var hub = HubMetricNames.Notification;
            var activeConnections = _presenceMetrics.GetOpenConnections(hub);
            var distinctUsers = _presenceMetrics.GetDistinctUsers(hub);

            _logger.LogInformation(
                "Notification hub presence: {Connections} connections, {DistinctUsers} distinct users",
                activeConnections, distinctUsers);

            var stats = new AdminStatsDto
            {
                TotalConnections = activeConnections,
                DistinctUsersOnline = distinctUsers,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Admin stats requested by {Nickname}: {Connections} connections, {Distinct} distinct users",
                nickname, activeConnections, distinctUsers);
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
    public int DistinctUsersOnline { get; set; }
    public DateTime Timestamp { get; set; }
}
