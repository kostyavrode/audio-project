namespace GroupsService.Application.DTOs;

public class SearchGroupsResultDto
{
    public IEnumerable<GroupDto> Groups { get; set; } = new List<GroupDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    
    public int TotalPages
    {
        get
        {
            if (PageSize == 0)
            {
                return 0;
            }
            return (int)Math.Ceiling((double)TotalCount / PageSize);
        }
    }
}
