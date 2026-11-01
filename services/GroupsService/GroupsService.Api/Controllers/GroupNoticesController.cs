using System.Security.Claims;
using GroupsService.Application.DTOs;
using GroupsService.Application.Services;
using GroupsService.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroupsService.Api.Controllers;

// Под /api/Groups, потому что nginx пробрасывает в GroupsService этот префикс.
// Литеральный сегмент "notices" приоритетнее, чем {id} в GroupsController.
[ApiController]
[Route("api/Groups/notices")]
[Authorize]
public class GroupNoticesController : ControllerBase
{
    private readonly IGroupNoticeService _notices;

    public GroupNoticesController(IGroupNoticeService notices)
    {
        _notices = notices;
    }

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    private string? NickName => User.FindFirstValue("nickname") ?? User.FindFirstValue(ClaimTypes.Name);

    [HttpPost]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("group-notices")]
    public async Task<IActionResult> Send([FromBody] SendGroupNoticeDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(NickName))
        {
            return Unauthorized(new { error = "User not found in token" });
        }

        // Гость заходит одним кликом - иначе рассылка стала бы готовым каналом для спама
        if (User.FindFirstValue("guest") == "true")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Гости не могут отправлять уведомления" });
        }

        try
        {
            return Ok(new { sent = await _notices.SendAsync(dto, UserId, NickName, cancellationToken) });
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

    /// <summary>Уведомления, которые текущий пользователь ещё не закрыл.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GroupNoticeDto>>> GetPending(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(UserId))
        {
            return Unauthorized(new { error = "User not found in token" });
        }
        return Ok(await _notices.GetPendingAsync(UserId, cancellationToken));
    }

    [HttpPost("{noticeId}/dismiss")]
    public async Task<IActionResult> Dismiss(string noticeId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(UserId))
        {
            return Unauthorized(new { error = "User not found in token" });
        }
        await _notices.DismissAsync(noticeId, UserId, cancellationToken);
        return NoContent();
    }
}
