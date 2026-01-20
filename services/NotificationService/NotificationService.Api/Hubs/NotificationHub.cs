using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace NotificationService.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            Context.Abort();
            return;
        }

        _logger.LogInformation("User {UserId} connected to NotificationHub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            await Clients.Caller.SendAsync("Error", "Group ID is required");
            return;
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        _logger.LogInformation("User {UserId} joined group {GroupId}", userId, groupId);
        
        await Clients.Caller.SendAsync("JoinedGroup", groupId);
    }

    public async Task LeaveGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            await Clients.Caller.SendAsync("Error", "Group ID is required");
            return;
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        _logger.LogInformation("User {UserId} left group {GroupId}", userId, groupId);
        
        await Clients.Caller.SendAsync("LeftGroup", groupId);
    }

    private string? GetUserId()
    {
        if (Context.User == null) return null;

        var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            userId = Context.User.FindFirstValue("sub");
        }
        return userId;
    }
}
