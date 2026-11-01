using GroupsService.Application.DTOs;
using GroupsService.Domain.Entities;
using GroupsService.Domain.Exceptions;
using GroupsService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GroupsService.Application.Services;

public class GroupNoticeService : IGroupNoticeService
{
    private readonly IGroupNoticeRepository _notices;
    private readonly IGroupRepository _groups;
    private readonly ILogger<GroupNoticeService> _logger;

    public GroupNoticeService(IGroupNoticeRepository notices, IGroupRepository groups, ILogger<GroupNoticeService> logger)
    {
        _notices = notices;
        _groups = groups;
        _logger = logger;
    }

    public async Task<int> SendAsync(SendGroupNoticeDto dto, string fromUserId, string fromNickName, CancellationToken cancellationToken = default)
    {
        var group = await _groups.GetByIdAsync(dto.GroupId, cancellationToken)
            ?? throw new GroupNotFoundException(dto.GroupId);

        if (group.Members.All(m => m.UserId != fromUserId))
        {
            throw new UnauthorizedAccessException("Only group members can send notices");
        }

        // Получатели - только участники этой группы и не сам отправитель
        var wanted = dto.UserIds.ToHashSet();
        var recipients = group.Members
            .Where(m => m.UserId != fromUserId && wanted.Contains(m.UserId))
            .Select(m => m.UserId)
            .ToList();

        if (recipients.Count == 0)
        {
            throw new DomainException("Выберите хотя бы одного участника");
        }

        var notices = recipients
            .Select(userId => GroupNotice.Create(Guid.NewGuid().ToString(), group, fromNickName, userId, dto.Text))
            .ToList();

        await _notices.AddRangeAsync(notices, cancellationToken);
        await _notices.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notice to {Count} members of group {GroupId} sent by {UserId}", recipients.Count, group.Id, fromUserId);
        return recipients.Count;
    }

    public async Task<IReadOnlyList<GroupNoticeDto>> GetPendingAsync(string userId, CancellationToken cancellationToken = default)
    {
        var notices = await _notices.GetForUserAsync(userId, cancellationToken);
        return notices.Select(n => new GroupNoticeDto
        {
            Id = n.Id,
            GroupId = n.GroupId,
            GroupName = n.GroupName,
            FromNickName = n.FromNickName,
            Text = n.Text,
            CreatedAt = n.CreatedAt
        }).ToList();
    }

    public async Task DismissAsync(string noticeId, string userId, CancellationToken cancellationToken = default)
    {
        var notice = await _notices.GetByIdAsync(noticeId, cancellationToken);

        // Чужое уведомление не трогаем; уже закрытое - для пользователя тот же результат
        if (notice == null || notice.ToUserId != userId)
        {
            return;
        }

        await _notices.RemoveAsync(notice, cancellationToken);
        await _notices.SaveChangesAsync(cancellationToken);
    }
}
