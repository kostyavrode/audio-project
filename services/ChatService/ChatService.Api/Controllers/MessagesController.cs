using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Application.DTOs;
using ChatService.Application.Services;
using ChatService.Infrastructure.Messaging;
using ChatService.Api.Security;
using ChatService.Domain.Interfaces;
using System.Text.Json;

namespace ChatService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IRabbitMQPublisher _rabbitMQPublisher;
    private readonly ILogger<MessagesController> _logger;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly GroupMembershipVerifier _membershipVerifier;

    public MessagesController(
        IMessageService messageService,
        IRabbitMQPublisher rabbitMQPublisher,
        IGroupMemberRepository groupMemberRepository,
        GroupMembershipVerifier membershipVerifier,
        ILogger<MessagesController> logger)
    {
        _groupMemberRepository = groupMemberRepository ?? throw new ArgumentNullException(nameof(groupMemberRepository));
        _membershipVerifier = membershipVerifier ?? throw new ArgumentNullException(nameof(membershipVerifier));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _rabbitMQPublisher = rabbitMQPublisher ?? throw new ArgumentNullException(nameof(rabbitMQPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private async Task<bool> IsGroupMemberAsync(string groupId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        // Сначала локальная копия участников; если в ней нет (вступил секунду назад,
        // событие ещё не дошло) - спрашиваем GroupsService
        if (await _groupMemberRepository.ExistsAsync(groupId, userId, cancellationToken))
        {
            return true;
        }

        return await _membershipVerifier.IsMemberAsync(
            groupId, userId, GroupMembershipVerifier.ExtractToken(Request), cancellationToken);
    }

    [HttpGet("{groupId}")]
    [ProducesResponseType(typeof(GetMessagesResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetMessagesResultDto>> GetMessages(
        string groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            return BadRequest(new { error = "Group ID is required" });
        }

        // Читать переписку могут только участники группы. Раньше проверки не было, и любой
        // вошедший пользователь мог получить историю любой группы, включая закрытые паролем.
        if (!await IsGroupMemberAsync(groupId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "User must be a member of the group" });
        }

        pageSize = Math.Clamp(pageSize, 1, 200);
        page = Math.Max(page, 1);

        var getMessagesDto = new GetMessagesDto
        {
            GroupId = groupId,
            Page = page,
            PageSize = pageSize
        };

        try
        {
            var result = await _messageService.GetMessagesAsync(getMessagesDto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for group {GroupId}", groupId);
            throw;
        }
    }

    [HttpGet("message/{messageId}")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageDto>> GetMessageById(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return BadRequest(new { error = "Message ID is required" });
        }

        try
        {
            var message = await _messageService.GetMessageByIdAsync(messageId, cancellationToken);
            
            // Чужое сообщение не отличаем от несуществующего
            if (message == null || !await IsGroupMemberAsync(message.GroupId, cancellationToken))
            {
                return NotFound(new { error = "Message not found" });
            }

            return Ok(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message {MessageId}", messageId);
            throw;
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageDto>> SendMessage(
        [FromBody] SendMessageDto sendMessageDto,
        CancellationToken cancellationToken = default)
    {
        if (sendMessageDto == null)
        {
            return BadRequest(new { error = "Message data is required" });
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            userId = User.FindFirst("sub")?.Value;
        }

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }

        var userNickName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(userNickName))
        {
            userNickName = User.FindFirst("nickname")?.Value;
        }

        if (string.IsNullOrEmpty(userNickName))
        {
            return Unauthorized(new { error = "User nickname not found in token" });
        }

        try
        {
            var messageDto = await _messageService.SendMessageAsync(sendMessageDto, userId, userNickName, cancellationToken);
            
            var messageJson = JsonSerializer.Serialize(messageDto);
            await _rabbitMQPublisher.PublishAsync("chat-messages", "ChatMessage", messageJson, cancellationToken);
            
            _logger.LogInformation("Message {MessageId} published to RabbitMQ for group {GroupId} by user {UserId}", 
                messageDto.Id, sendMessageDto.GroupId, userId);
            
            return Ok(messageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to group {GroupId}", sendMessageDto.GroupId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
