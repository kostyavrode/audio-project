namespace AudioService.Application.Services;

public interface IGroupAccessChecker
{
    Task<bool> IsGroupOwnerAsync(string groupId, string userId, CancellationToken cancellationToken = default);
    Task<bool> IsGroupMemberAsync(string groupId, string userId, CancellationToken cancellationToken = default);
}
