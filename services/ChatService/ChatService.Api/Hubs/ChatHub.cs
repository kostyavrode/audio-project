using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using ChatService.Application.DTOs;
using ChatService.Application.Services;
using ChatService.Domain.Interfaces;
using ChatService.Domain.Entities;
using ChatService.Infrastructure.Messaging;
using System.Text.Json;

namespace ChatService.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IRabbitMQPublisher _rabbitMQPublisher;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IMessageService messageService,
        IGroupMemberRepository groupMemberRepository,
        IRabbitMQPublisher rabbitMQPublisher,
        ILogger<ChatHub> logger)
    {
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _groupMemberRepository = groupMemberRepository ?? throw new ArgumentNullException(nameof(groupMemberRepository));
        _rabbitMQPublisher = rabbitMQPublisher ?? throw new ArgumentNullException(nameof(rabbitMQPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            Context.Abort();
            return;
        }

        _logger.LogInformation("User {UserId} connected to ChatHub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            await Clients.Caller.SendAsync("Error", "Group ID is required");
            return;
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        var isMember = await _groupMemberRepository.ExistsAsync(groupId, userId);
        if (!isMember)
        {
            _logger.LogWarning("User {UserId} attempted to join group {GroupId} but is not a member", userId, groupId);
            await Clients.Caller.SendAsync("Error", "You are not a member of this group");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        _logger.LogInformation("User {UserId} joined group {GroupId}", userId, groupId);
        
        await Clients.Caller.SendAsync("JoinedGroup", groupId);
    }

    public async Task LeaveGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            await Clients.Caller.SendAsync("Error", "Group ID is required");
            return;
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        _logger.LogInformation("User {UserId} left group {GroupId}", userId, groupId);
        
        await Clients.Caller.SendAsync("LeftGroup", groupId);
    }

    public async Task SendMessage(SendMessageDto sendMessageDto)
    {
        if (sendMessageDto == null)
        {
            await Clients.Caller.SendAsync("Error", "Message data is required");
            return;
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }
        
        var userNickName = GetUserNickName();
        if (string.IsNullOrEmpty(userNickName))
        {
            _logger.LogWarning("User {UserId} has no nickname in token", userId);
            await Clients.Caller.SendAsync("Error", "User nickname not found in token");
            return;
        }

        try
        {
            var messageDto = await _messageService.SendMessageAsync(sendMessageDto, userId, userNickName);
            
            await Clients.Caller.SendAsync("ReceiveMessage", messageDto);
            
            var messageJson = JsonSerializer.Serialize(messageDto);
            await _rabbitMQPublisher.PublishAsync("chat-messages", "ChatMessage", messageJson);
            
            _logger.LogInformation("Message {MessageId} published to RabbitMQ for group {GroupId} by user {UserId}", 
                messageDto.Id, sendMessageDto.GroupId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to group {GroupId}", sendMessageDto.GroupId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    private string? GetUserId()
    {
        if (Context.User == null) return null;

        var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            userId = Context.User.FindFirstValue("sub");
        }
        return userId;
    }

    private string? GetUserNickName()
    {
        if (Context.User == null) return null;

        var userNickName = Context.User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(userNickName))
        {
            userNickName = Context.User.FindFirstValue("nickname");
        }
        return userNickName;
    }
}
