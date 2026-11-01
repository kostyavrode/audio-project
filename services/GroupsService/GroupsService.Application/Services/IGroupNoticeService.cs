using GroupsService.Application.DTOs;

namespace GroupsService.Application.Services;

public interface IGroupNoticeService
{
    /// <summary>Отправляет уведомление выбранным участникам, возвращает число получателей.</summary>
    Task<int> SendAsync(SendGroupNoticeDto dto, string fromUserId, string fromNickName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GroupNoticeDto>> GetPendingAsync(string userId, CancellationToken cancellationToken = default);
    Task DismissAsync(string noticeId, string userId, CancellationToken cancellationToken = default);
}
