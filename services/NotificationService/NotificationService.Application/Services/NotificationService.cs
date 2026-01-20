using Microsoft.AspNetCore.SignalR;
using NotificationService.Api.Hubs;

namespace NotificationService.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendChatMessageAsync(string groupId, object messageDto)
    {
        await _hubContext.Clients.Group(groupId).SendAsync("ReceiveMessage", messageDto);
        _logger.LogInformation("Chat message sent to group {GroupId}", groupId);
    }

    public async Task SendAudioParticipantJoinedAsync(string groupId, string channelId, string userId, string displayName, long participantId)
    {
        await _hubContext.Clients.Group(groupId).SendAsync("AudioParticipantJoined", new
        {
            channelId,
            userId,
            displayName,
            participantId
        });
        _logger.LogInformation("Audio participant joined notification sent: User {UserId} joined channel {ChannelId} in group {GroupId}", 
            userId, channelId, groupId);
    }

    public async Task SendAudioParticipantLeftAsync(string groupId, string channelId, string userId, long participantId)
    {
        await _hubContext.Clients.Group(groupId).SendAsync("AudioParticipantLeft", new
        {
            channelId,
            userId,
            participantId
        });
        _logger.LogInformation("Audio participant left notification sent: User {UserId} left channel {ChannelId} in group {GroupId}", 
            userId, channelId, groupId);
    }
}
