namespace AudioService.Infrastructure.Data;

public class GroupMember
{
    public string Id { get; set; } = string.Empty;
    
    public string GroupId { get; set; } = string.Empty;
    
    public string UserId { get; set; } = string.Empty;
    
    public GroupMemberRole Role { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum GroupMemberRole
{
    Owner = 0,
    Admin = 1,
    Member = 2
}
