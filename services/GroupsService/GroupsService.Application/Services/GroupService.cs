using GroupsService.Application.DTOs;
using GroupsService.Domain.Entities;
using GroupsService.Domain.Exceptions;
using GroupsService.Domain.Interfaces;
using GroupsService.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GroupsService.Application.Services;

public class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IPasswordHasher<Group> _hasher;
    private readonly ILogger<GroupService> _logger;

    public GroupService(IGroupRepository groupRepository, IPasswordHasher<Group> hasher, ILogger<GroupService> logger)
    {
        _groupRepository = groupRepository;
        _hasher = hasher;
        _logger = logger;
    }
    
    public async Task<GroupDto> CreateGroupAsync(CreateGroupDto createGroupDto, string userId, string nickName, CancellationToken cancellationToken = default)
    {
        var groupId = Guid.NewGuid().ToString();
        
        string? passwordHash = null;

        if (!string.IsNullOrWhiteSpace(createGroupDto.Password))
        {
            var tempGroup = (Group)System.Activator.CreateInstance(typeof(Group), nonPublic:true)!;
            passwordHash = _hasher.HashPassword(tempGroup, createGroupDto.Password);
        }
        
        var group = Group.Create(groupId, createGroupDto.Name, createGroupDto.Description, passwordHash, userId, nickName);
        
        await _groupRepository.AddAsync(group, cancellationToken);
        await _groupRepository.SaveChangesAsync(cancellationToken);
        
        return MapToGroupDto(group);
    }
    
    public async Task<GroupDto> GetGroupByIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        
        if (group == null)
        {
            throw new GroupNotFoundException(groupId);
        }
        
        return MapToGroupDto(group);
    }

    public async Task<IEnumerable<GroupDto>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var groups = await _groupRepository.GetByUserIdAsync(userId, cancellationToken);
        
        return groups.Select(MapToGroupDto);
    }
    
    public async Task<GroupDto> UpdateGroupAsync(string groupId, UpdateGroupDto updateGroupDto, string userId, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        
        if (group == null)
        {
            throw new GroupNotFoundException(groupId);
        }
        
        if (group.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only group owner can update the group");
        }
        
        if (!string.IsNullOrEmpty(updateGroupDto.Name))
        {
            group.UpdateName(updateGroupDto.Name);
        }
        
        if (updateGroupDto.Description != null)
        {
            group.UpdateDescription(updateGroupDto.Description);
        }
        
        await _groupRepository.UpdateAsync(group, cancellationToken);
        await _groupRepository.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Group {GroupId} updated by user {UserId}", groupId, userId);
        
        return MapToGroupDto(group);
    }
    
    public async Task DeleteGroupAsync(string groupId, string userId, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        
        if (group == null)
        {
            throw new GroupNotFoundException(groupId);
        }
        
        if (group.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only group owner can delete the group");
        }
        
        group.MarkForDeletion();
        
        await _groupRepository.DeleteAsync(group, cancellationToken);
        await _groupRepository.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Group {GroupId} deleted by user {UserId}", groupId, userId);
    }
    
    public async Task<SearchGroupsResultDto> SearchGroupsAsync(SearchGroupsDto searchDto, CancellationToken cancellationToken = default)
    {
        var (groups, totalCount) = await _groupRepository.SearchAsync(
            searchDto.Query, 
            searchDto.Page, 
            searchDto.PageSize, 
            cancellationToken);
        
        return new SearchGroupsResultDto
        {
            Groups = groups.Select(MapToGroupDto),
            TotalCount = totalCount,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize
        };
    }
    
    public async Task JoinGroupAsync(string groupId, JoinGroupDto joinGroupDto, string userId, string nickName, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        
        if (group == null)
        {
            throw new GroupNotFoundException(groupId);
        }
        
        if (group.Members.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException("User is already a member of this group");
        }
        
        if (!string.IsNullOrEmpty(group.PasswordHash))
        {
            if (string.IsNullOrEmpty(joinGroupDto.Password))
            {
                throw new InvalidGroupPasswordException("Password is required to join this group");
            }
            
            var passwordVerificationResult = _hasher.VerifyHashedPassword(
                group, 
                group.PasswordHash, 
                joinGroupDto.Password);
            
            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                throw new InvalidGroupPasswordException("Invalid group password");
            }
        }
        
        group.AddMember(userId, nickName, GroupMemberRole.Member);
        
        await _groupRepository.UpdateAsync(group, cancellationToken);
        await _groupRepository.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("User {UserId} joined group {GroupId}", userId, groupId);
    }
    
    public async Task LeaveGroupAsync(string groupId, string userId, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        
        if (group == null)
        {
            throw new GroupNotFoundException(groupId);
        }
        
        var member = group.Members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
        {
            throw new InvalidOperationException("User is not a member of this group");
        }
        
        if (group.OwnerId == userId)
        {
            throw new InvalidOperationException("Group owner cannot leave the group. Delete the group or transfer ownership first.");
        }
        
        group.RemoveMember(userId);
        
        await _groupRepository.UpdateAsync(group, cancellationToken);
        await _groupRepository.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("User {UserId} left group {GroupId}", userId, groupId);
    }
    
    public async Task<IEnumerable<GroupMemberDto>> GetGroupMembersAsync(string groupId, string userId, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        
        if (group == null)
        {
            throw new GroupNotFoundException(groupId);
        }
        
        if (!group.Members.Any(m => m.UserId == userId))
        {
            throw new UnauthorizedAccessException("User must be a member of the group to view members");
        }
        
        return group.Members.Select(m => new GroupMemberDto
        {
            Id = m.Id,
            GroupId = m.GroupId,
            UserId = m.UserId,
            NickName = m.NickName,
            Role = m.Role.ToString(),
            JoinedAt = m.CreatedAt
        });
    }

    private static GroupDto MapToGroupDto(Group group)
    {
        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            OwnerId = group.OwnerId,
            HasPassword = !string.IsNullOrEmpty(group.PasswordHash),
            MemberCount = group.Members.Count,
            CreatedAt = group.CreatedAt
        };
    }
}