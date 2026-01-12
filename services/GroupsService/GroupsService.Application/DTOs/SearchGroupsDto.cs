namespace GroupsService.Application.DTOs;

public class SearchGroupsDto
{
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
