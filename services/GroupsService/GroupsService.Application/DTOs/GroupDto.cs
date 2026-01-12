namespace GroupsService.Application.DTOs;

public class GroupDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public bool HasPassword { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}