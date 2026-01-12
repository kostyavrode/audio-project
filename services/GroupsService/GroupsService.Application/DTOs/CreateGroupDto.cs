using System.ComponentModel.DataAnnotations;

namespace GroupsService.Application.DTOs;

public class CreateGroupDto
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MinLength(1)]
    public string? Password { get; set; }
}