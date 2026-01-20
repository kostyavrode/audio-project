namespace NotificationService.Application.Services;

public interface INotificationService
{
    Task SendChatMessageAsync(string groupId, object messageDto);
    Task SendAudioParticipantJoinedAsync(string groupId, string channelId, string userId, string displayName, long participantId);
    Task SendAudioParticipantLeftAsync(string groupId, string channelId, string userId, long participantId);
}
