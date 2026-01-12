using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Domain.Interfaces;
using ChatService.Domain.Entities;
using System.Security.Claims;

namespace ChatService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        IGroupMemberRepository groupMemberRepository,
        ILogger<SyncController> logger)
    {
        _groupMemberRepository = groupMemberRepository ?? throw new ArgumentNullException(nameof(groupMemberRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("group/{groupId}/member")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> SyncGroupMember(
        string groupId,
        [FromQuery] string? role = "Member",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            return BadRequest(new { error = "Group ID is required" });
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        var exists = await _groupMemberRepository.ExistsAsync(groupId, userId, cancellationToken);
        if (exists)
        {
            return Ok(new { message = "User is already a member of the group" });
        }

        GroupMemberRole memberRole;
        if (Enum.TryParse<GroupMemberRole>(role, true, out var parsedRole))
        {
            memberRole = parsedRole;
        }
        else
        {
            memberRole = GroupMemberRole.Member;
        }

        var groupMember = GroupMember.Create(
            Guid.NewGuid().ToString(),
            groupId,
            userId,
            memberRole
        );

        await _groupMemberRepository.AddAsync(groupMember, cancellationToken);
        await _groupMemberRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Synced group member. GroupId: {GroupId}, UserId: {UserId}, Role: {Role}", 
            groupId, userId, memberRole);

        return Ok(new { message = "Group member synced successfully" });
    }

    private string? GetUserId()
    {
        if (User == null)
        {
            return null;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            userId = User.FindFirstValue("sub");
        }
        return userId;
    }
}
