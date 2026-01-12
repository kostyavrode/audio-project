using GroupsService.Domain.Exceptions;
using GroupsService.Domain.ValueObjects;

namespace GroupsService.Domain.Entities;

public class GroupMember : BaseEntity
{
    public string GroupId { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public GroupMemberRole Role { get; private set; }
    
    public Group? Group { get; private set; }
    
    private GroupMember() { }
    
    public static GroupMember Create(string id, string groupId, string userId, GroupMemberRole role)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainException("GroupMember ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(groupId))
        {
            throw new DomainException("Group ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("User ID cannot be empty");
        }

        return new GroupMember
        {
            Id = id,
            GroupId = groupId,
            UserId = userId,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }
}