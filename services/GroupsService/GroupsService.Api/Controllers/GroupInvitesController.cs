using System.Security.Claims;
using GroupsService.Application.DTOs;
using GroupsService.Application.Services;
using GroupsService.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroupsService.Api.Controllers;

// Под /api/Groups, потому что nginx пробрасывает в GroupsService этот префикс.
// Литеральный сегмент "invites" приоритетнее, чем {id} в GroupsController.
[ApiController]
[Route("api/Groups/invites")]
[Authorize]
public class GroupInvitesController : ControllerBase
{
    private readonly IGroupInviteService _invites;

    public GroupInvitesController(IGroupInviteService invites)
    {
        _invites = invites;
    }

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    private string? NickName => User.FindFirstValue("nickname") ?? User.FindFirstValue(ClaimTypes.Name);

    [HttpPost]
    public async Task<ActionResult<GroupInviteDto>> Create([FromBody] CreateGroupInviteDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(NickName))
        {
            return Unauthorized(new { error = "User not found in token" });
        }
        if (string.IsNullOrWhiteSpace(dto.NickName) || dto.NickName.Length > 50 || string.IsNullOrWhiteSpace(dto.GroupId))
        {
            return BadRequest(new { error = "Укажите никнейм" });
        }

        try
        {
            return Ok(await _invites.CreateAsync(dto, UserId, NickName, cancellationToken));
        }
        catch (GroupNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Приглашения, на которые текущий пользователь ещё не ответил.</summary>
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<IReadOnlyList<GroupInviteDto>>> GetPending(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(NickName))
        {
            return Unauthorized(new { error = "User not found in token" });
        }
        return Ok(await _invites.GetPendingAsync(NickName, cancellationToken));
    }

    [HttpPost("{inviteId}/accept")]
    public async Task<ActionResult<AcceptedInviteDto>> Accept(string inviteId, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _invites.AcceptAsync(inviteId, NickName ?? string.Empty, cancellationToken));
        }
        catch (GroupNotFoundException)
        {
            return NotFound(new { error = "Приглашение больше не действует" });
        }
    }

    [HttpPost("{inviteId}/decline")]
    public async Task<IActionResult> Decline(string inviteId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _invites.DeclineAsync(inviteId, NickName ?? string.Empty, cancellationToken);
        }
        catch (GroupNotFoundException)
        {
            // Уже отвечено - для пользователя результат тот же
        }
        return NoContent();
    }
}
