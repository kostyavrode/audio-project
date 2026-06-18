namespace GroupsService.Application.DTOs;

public class UpdateMemberRoleDto
{
    public string? UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}
