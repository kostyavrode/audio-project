using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AudioService.Application.DTOs;
using AudioService.Application.Services;
using AudioService.Domain.Exceptions;
using AudioService.Domain.Interfaces;
using AudioService.Api.Security;

namespace AudioService.Api.Controllers;

[ApiController]
[Route("api/audio/[controller]")]
[Authorize]
public class AudioChannelsController : ControllerBase
{
    private readonly IAudioChannelService _audioChannelService;
    private readonly ILogger<AudioChannelsController> _logger;
    private readonly IGroupAccessChecker _groupAccessChecker;
    private readonly IAudioChannelRepository _channelRepository;
    private readonly GroupMembershipVerifier _membershipVerifier;

    public AudioChannelsController(
        IAudioChannelService audioChannelService,
        IGroupAccessChecker groupAccessChecker,
        IAudioChannelRepository channelRepository,
        GroupMembershipVerifier membershipVerifier,
        ILogger<AudioChannelsController> logger)
    {
        _groupAccessChecker = groupAccessChecker;
        _channelRepository = channelRepository;
        _membershipVerifier = membershipVerifier;
        _audioChannelService = audioChannelService;
        _logger = logger;
    }

    // Список каналов, сами каналы и участники раньше отдавались любому вошедшему пользователю,
    // в том числе по закрытым паролем группам. Теперь - только участникам группы.
    private async Task<bool> IsGroupMemberAsync(string groupId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        // Сначала локальная копия участников; если в ней нет (вступил секунду назад,
        // событие ещё не дошло) - спрашиваем GroupsService
        if (await _groupAccessChecker.IsGroupMemberAsync(groupId, userId, cancellationToken))
        {
            return true;
        }

        return await _membershipVerifier.IsMemberAsync(
            groupId, userId, GroupMembershipVerifier.ExtractToken(Request), cancellationToken);
    }

    private ObjectResult NotAMember() =>
        StatusCode(StatusCodes.Status403Forbidden, new { error = "User must be a member of the group" });

    [HttpPost]
    [ProducesResponseType(typeof(AudioChannelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AudioChannelDto>> CreateAudioChannel(
        [FromBody] CreateAudioChannelDto createDto,
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
            var channelDto = await _audioChannelService.CreateAudioChannelAsync(createDto, userIdClaim, cancellationToken);

            _logger.LogInformation("Audio channel created: {ChannelId} in group {GroupId} by user {UserId}", channelDto.Id, channelDto.GroupId, userIdClaim);

            return CreatedAtAction(nameof(GetAudioChannel), new { id = channelDto.Id }, channelDto);
        }
        catch (UnauthorizedToCreateChannelException ex)
        {
            _logger.LogWarning(ex, "Unauthorized channel creation attempt: Group {GroupId} by user {UserId}", createDto.GroupId, userIdClaim);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error during channel creation for user {UserId}", userIdClaim);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AudioChannelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AudioChannelDto>> GetAudioChannel(string id, CancellationToken cancellationToken = default)
    {
        var channelDto = await _audioChannelService.GetAudioChannelByIdAsync(id, cancellationToken);

        // Чужой канал не отличаем от несуществующего
        if (channelDto == null || !await IsGroupMemberAsync(channelDto.GroupId, cancellationToken))
        {
            return NotFound(new { error = $"Audio channel with ID '{id}' was not found" });
        }

        return Ok(channelDto);
    }

    [HttpGet("groups/{groupId}")]
    [ProducesResponseType(typeof(IEnumerable<AudioChannelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<AudioChannelDto>>> GetChannelsByGroupId(string groupId, CancellationToken cancellationToken = default)
    {
        if (!await IsGroupMemberAsync(groupId, cancellationToken))
        {
            return NotAMember();
        }

        var channels = await _audioChannelService.GetChannelsByGroupIdAsync(groupId, cancellationToken);
        return Ok(channels);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AudioChannelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AudioChannelDto>> UpdateAudioChannel(
        string id,
        [FromBody] UpdateAudioChannelDto updateDto,
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
            var channelDto = await _audioChannelService.UpdateAudioChannelAsync(id, updateDto, userIdClaim, cancellationToken);

            _logger.LogInformation("Audio channel {ChannelId} updated by user {UserId}", id, userIdClaim);

            return Ok(channelDto);
        }
        catch (AudioChannelNotFoundException ex)
        {
            _logger.LogWarning(ex, "Audio channel not found: {ChannelId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized update attempt: Channel {ChannelId} by user {UserId}", id, userIdClaim);
            return Unauthorized(new { error = ex.Message });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error during channel update: Channel {ChannelId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAudioChannel(string id, CancellationToken cancellationToken = default)
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
            await _audioChannelService.DeleteAudioChannelAsync(id, userIdClaim, cancellationToken);

            _logger.LogInformation("Audio channel {ChannelId} deleted by user {UserId}", id, userIdClaim);

            return NoContent();
        }
        catch (AudioChannelNotFoundException ex)
        {
            _logger.LogWarning(ex, "Audio channel not found: {ChannelId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized delete attempt: Channel {ChannelId} by user {UserId}", id, userIdClaim);
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/recreate-room")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecreateJanusRoom(string id, CancellationToken cancellationToken = default)
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
            var success = await _audioChannelService.RecreateJanusRoomAsync(id, userIdClaim, cancellationToken);

            _logger.LogInformation("Janus room recreated for channel {ChannelId} by user {UserId}", id, userIdClaim);

            return Ok(new { success = true, message = "Janus room recreated successfully" });
        }
        catch (AudioChannelNotFoundException ex)
        {
            _logger.LogWarning(ex, "Audio channel not found: {ChannelId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized recreate room attempt: Channel {ChannelId} by user {UserId}", id, userIdClaim);
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for channel {ChannelId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recreate Janus room for channel {ChannelId}", id);
            return StatusCode(500, new { error = "Failed to recreate Janus room" });
        }
    }

    [HttpGet("{id}/participants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetChannelParticipants(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = await _channelRepository.GetByIdAsync(id, cancellationToken);
            if (channel == null || !await IsGroupMemberAsync(channel.GroupId, cancellationToken))
            {
                return NotFound(new { error = $"Audio channel with ID '{id}' was not found" });
            }

            var participants = await _audioChannelService.GetChannelParticipantsAsync(id, cancellationToken);
            return Ok(participants);
        }
        catch (AudioChannelNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // SetParticipantVolume endpoint удален - громкость теперь управляется на клиенте через Web Audio API

    [HttpPost("{id}/participants/joined")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterParticipantJoined(
        string id,
        [FromBody] RegisterParticipantDto dto,
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
            // Имя берём из токена, а не из запроса: иначе можно было представиться кем угодно
            // (и подставить произвольный текст в список участников у всех, кто смотрит группу)
            var nickname = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("nickname");
            if (!string.IsNullOrWhiteSpace(nickname))
            {
                dto.DisplayName = nickname;
            }

            await _audioChannelService.RegisterParticipantJoinedAsync(id, userIdClaim, dto, cancellationToken);
            return Ok(new { success = true });
        }
        catch (AudioChannelNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/participants/{participantId}/left")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterParticipantLeft(
        string id,
        long participantId,
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
            await _audioChannelService.RegisterParticipantLeftAsync(id, userIdClaim, participantId, cancellationToken);
            return Ok(new { success = true });
        }
        catch (AudioChannelNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}
