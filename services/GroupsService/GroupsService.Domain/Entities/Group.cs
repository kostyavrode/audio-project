using GroupsService.Domain.DomainEvents;
using GroupsService.Domain.Exceptions;
using GroupsService.Domain.ValueObjects;

namespace GroupsService.Domain.Entities;

public class Group : BaseEntity
{
    public string Name { get; set; } = String.Empty;
    
    public string? Description { get; set; } = String.Empty;
    
    public string? PasswordHash { get; set; } = String.Empty;
    
    public string OwnerId { get; set; } = String.Empty;

    private readonly List<GroupMember> _members = new();
    
    public IReadOnlyCollection<GroupMember> Members => _members.AsReadOnly();

    private Group()
    {
    }

    public static Group Create(string id, string name, string? description, string? passwordHash, string ownerId, string ownerNickName)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainException("Group ID cannot be empty");
        }
        
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Group name cannot be empty");
        }

        if (name.Length > 100)
        {
            throw new DomainException("Group name cannot exceed 100 characters");
        }
        
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new DomainException("Owner ID cannot be empty");
        }
        
        var group = new Group
        {
            Id = id,
            Name = name.Trim(),
            PasswordHash = passwordHash,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };
        
        if (description != null)
        {
            group.Description = description.Trim();
        }
        
        var ownerMember = GroupMember.Create(
            Guid.NewGuid().ToString(),
            id,
            ownerId,
            ownerNickName ?? "Owner",
            GroupMemberRole.Owner
        );
        group._members.Add(ownerMember);
        
        group.AddDomainEvent(new GroupCreatedEvent(group.Id, group.Name, group.OwnerId, group.CreatedAt));
        group.AddDomainEvent(new UserJoinedGroupEvent(group.Id, ownerId, GroupMemberRole.Owner.ToString(), group.CreatedAt));
        
        return group;
    }
    
    public void AddMember(string userId, string nickName, GroupMemberRole role = GroupMemberRole.Member)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("User ID cannot be empty");
            
        if (_members.Any(m => m.UserId == userId))
            throw new DomainException($"User {userId} is already a member of this group");
        
        var member = GroupMember.Create(
            Guid.NewGuid().ToString(),
            Id,
            userId,
            nickName ?? "Member",
            role
        );
        
        _members.Add(member);
        MarkAsUpdated();
        
        AddDomainEvent(new UserJoinedGroupEvent(Id, userId, role.ToString(), DateTime.UtcNow));
    }
    
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Group name cannot be empty");
        
        if (name.Length > 100)
            throw new DomainException("Group name cannot exceed 100 characters");
        
        Name = name.Trim();
        MarkAsUpdated();
    }
    
    public void UpdateDescription(string? description)
    {
        if (description != null && description.Length > 500)
        {
            throw new DomainException("Description cannot exceed 500 characters");
        }
        
        if (description != null)
        {
            Description = description.Trim();
        }
        else
        {
            Description = null;
        }
        
        MarkAsUpdated();
    }
    
    public void RemoveMember(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("User ID cannot be empty");
        
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            throw new DomainException($"User {userId} is not a member of this group");
        
        _members.Remove(member);
        MarkAsUpdated();
        
        AddDomainEvent(new UserLeftGroupEvent(Id, userId, DateTime.UtcNow));
    }
    
    public void MarkForDeletion()
    {
        AddDomainEvent(new GroupDeletedEvent(Id, DateTime.UtcNow));
    }
}