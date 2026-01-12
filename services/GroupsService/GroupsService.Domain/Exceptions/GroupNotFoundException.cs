namespace GroupsService.Domain.Exceptions;

public class GroupNotFoundException : DomainException
{
    public GroupNotFoundException(string groupId) : base($"Group with ID '{groupId}' not found")
    { }
}