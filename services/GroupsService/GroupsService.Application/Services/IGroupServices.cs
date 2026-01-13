using GroupsService.Application.DTOs;

namespace GroupsService.Application.Services;

public interface IGroupService
{
    Task<GroupDto> CreateGroupAsync(CreateGroupDto createGroupDto, string userId, string nickName, CancellationToken cancellationToken = default);
    Task<GroupDto> GetGroupByIdAsync(string groupId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GroupDto>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default);
    Task<GroupDto> UpdateGroupAsync(string groupId, UpdateGroupDto updateGroupDto, string userId, CancellationToken cancellationToken = default);
    Task DeleteGroupAsync(string groupId, string userId, CancellationToken cancellationToken = default);
    Task<SearchGroupsResultDto> SearchGroupsAsync(SearchGroupsDto searchDto, CancellationToken cancellationToken = default);
    Task JoinGroupAsync(string groupId, JoinGroupDto joinGroupDto, string userId, string nickName, CancellationToken cancellationToken = default);
    Task LeaveGroupAsync(string groupId, string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GroupMemberDto>> GetGroupMembersAsync(string groupId, string userId, CancellationToken cancellationToken = default);
}