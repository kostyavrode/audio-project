using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AudioService.Application.DTOs;
using AudioService.Application.Services;
using AudioService.Domain.Exceptions;

namespace AudioService.Api.Controllers;

[ApiController]
[Route("api/audio/[controller]")]
[Authorize]
public class AudioChannelsController : ControllerBase
{
    private readonly IAudioChannelService _audioChannelService;
    private readonly ILogger<AudioChannelsController> _logger;

    public AudioChannelsController(IAudioChannelService audioChannelService, ILogger<AudioChannelsController> logger)
    {
        _audioChannelService = audioChannelService;
        _logger = logger;
    }

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
            return Unauthorized(new { error = ex.Message });
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

        if (channelDto == null)
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
            var participants = await _audioChannelService.GetChannelParticipantsAsync(id, cancellationToken);
            return Ok(participants);
        }
        catch (AudioChannelNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/participants/{participantId}/volume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetParticipantVolume(
        string id,
        long participantId,
        [FromBody] SetParticipantVolumeDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SetParticipantVolume called: ChannelId={ChannelId}, ParticipantId={ParticipantId}, Volume={Volume}", id, participantId, dto?.Volume);
        
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = User.FindFirstValue("sub");
        }

        if (string.IsNullOrEmpty(userIdClaim))
        {
            _logger.LogWarning("User ID not found in token for SetParticipantVolume");
            return Unauthorized(new { error = "User ID not found in token" });
        }

        try
        {
            _logger.LogInformation("Calling SetParticipantVolumeAsync: ChannelId={ChannelId}, ParticipantId={ParticipantId}, Volume={Volume}, UserId={UserId}", id, participantId, dto?.Volume, userIdClaim);
            await _audioChannelService.SetParticipantVolumeAsync(id, participantId, dto.Volume, userIdClaim, cancellationToken);
            _logger.LogInformation("SetParticipantVolumeAsync completed successfully");
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
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

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
