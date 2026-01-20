using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Application.DTOs;
using ChatService.Application.Services;
using ChatService.Infrastructure.Messaging;
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

    public MessagesController(
        IMessageService messageService,
        IRabbitMQPublisher rabbitMQPublisher,
        ILogger<MessagesController> logger)
    {
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _rabbitMQPublisher = rabbitMQPublisher ?? throw new ArgumentNullException(nameof(rabbitMQPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            
            if (message == null)
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

        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            userId = User.FindFirstValue("sub");
        }

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }

        var userNickName = User.FindFirstValue(System.Security.Claims.ClaimTypes.Name);
        if (string.IsNullOrEmpty(userNickName))
        {
            userNickName = User.FindFirstValue("nickname");
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
