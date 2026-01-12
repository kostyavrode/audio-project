using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GroupsService.Application.DTOs;
using GroupsService.Application.Services;
using GroupsService.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace GroupsService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(IGroupService groupService, ILogger<GroupsController> logger)
    {
        _groupService = groupService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GroupDto>> CreateGroup(
        [FromBody] CreateGroupDto createGroupDto,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = User.FindFirstValue("sub");
        }
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }
        
        try
        {
            var groupDto = await _groupService.CreateGroupAsync(createGroupDto, userIdClaim, cancellationToken);
            
            _logger.LogInformation("Group created: {GroupId} by user {UserId}", groupDto.Id, userIdClaim);
            
            return CreatedAtAction(nameof(GetGroup), new { id = groupDto.Id }, groupDto);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error during group creation for user {UserId}", userIdClaim);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GroupDto>> GetGroup(string id, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = User.FindFirstValue("sub");
        }
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }

        try
        {
            var groupDto = await _groupService.GetGroupByIdAsync(id, cancellationToken);
            return Ok(groupDto);
        }
        catch (GroupNotFoundException ex)
        {
            _logger.LogWarning(ex, "Group not found: {GroupId}", id);
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<GroupDto>>> GetUserGroups(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = User.FindFirstValue("sub");
        }
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }
        
        var groups = await _groupService.GetUserGroupsAsync(userIdClaim, cancellationToken);
        return Ok(groups);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDto>> UpdateGroup(
        string id,
        [FromBody] UpdateGroupDto updateGroupDto,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = User.FindFirstValue("sub");
        }
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }
        
        try
        {
            var groupDto = await _groupService.UpdateGroupAsync(id, updateGroupDto, userIdClaim, cancellationToken);
            
            _logger.LogInformation("Group {GroupId} updated by user {UserId}", id, userIdClaim);
            
            return Ok(groupDto);
        }
        catch (GroupNotFoundException ex)
        {
            _logger.LogWarning(ex, "Group not found: {GroupId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized update attempt: Group {GroupId} by user {UserId}", id, userIdClaim);
            return Unauthorized(new { error = ex.Message });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error during group update: Group {GroupId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroup(string id, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = User.FindFirstValue("sub");
        }
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }
        
        try
        {
            await _groupService.DeleteGroupAsync(id, userIdClaim, cancellationToken);
            
            _logger.LogInformation("Group {GroupId} deleted by user {UserId}", id, userIdClaim);
            
            return NoContent();
        }
        catch (GroupNotFoundException ex)
        {
            _logger.LogWarning(ex, "Group not found: {GroupId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized delete attempt: Group {GroupId} by user {UserId}", id, userIdClaim);
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchGroupsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SearchGroupsResultDto>> SearchGroups(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = User.FindFirstValue("sub");
        }
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }
        
        var searchDto = new SearchGroupsDto
        {
            Query = query,
            Page = page,
            PageSize = pageSize
        };
        
        var result = await _groupService.SearchGroupsAsync(searchDto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/join")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinGroup(
        string id,
        [FromBody] JoinGroupDto joinGroupDto,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = User.FindFirstValue("sub");
        }
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }
        
        try
        {
            await _groupService.JoinGroupAsync(id, joinGroupDto, userIdClaim, cancellationToken);
            
            _logger.LogInformation("User {UserId} joined group {GroupId}", userIdClaim, id);
            
            return NoContent();
        }
        catch (GroupNotFoundException ex)
        {
            _logger.LogWarning(ex, "Group not found: {GroupId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidGroupPasswordException ex)
        {
            _logger.LogWarning(ex, "Invalid password for group {GroupId}", id);
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveGroup(string id, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = User.FindFirstValue("sub");
        }
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }
        
        try
        {
            await _groupService.LeaveGroupAsync(id, userIdClaim, cancellationToken);
            
            _logger.LogInformation("User {UserId} left group {GroupId}", userIdClaim, id);
            
            return NoContent();
        }
        catch (GroupNotFoundException ex)
        {
            _logger.LogWarning(ex, "Group not found: {GroupId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/members")]
    [ProducesResponseType(typeof(IEnumerable<GroupMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<GroupMemberDto>>> GetGroupMembers(
        string id,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = User.FindFirstValue("sub");
        }
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }
        
        try
        {
            var members = await _groupService.GetGroupMembersAsync(id, userIdClaim, cancellationToken);
            return Ok(members);
        }
        catch (GroupNotFoundException ex)
        {
            _logger.LogWarning(ex, "Group not found: {GroupId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access: Group {GroupId} by user {UserId}", id, userIdClaim);
            return Unauthorized(new { error = ex.Message });
        }
    }
}
