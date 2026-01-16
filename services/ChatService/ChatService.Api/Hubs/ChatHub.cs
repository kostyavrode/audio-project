using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using ChatService.Application.DTOs;
using ChatService.Application.Services;
using ChatService.Domain.Interfaces;
using ChatService.Domain.Entities;

namespace ChatService.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IMessageService messageService,
        IGroupMemberRepository groupMemberRepository,
        ILogger<ChatHub> logger)
    {
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _groupMemberRepository = groupMemberRepository ?? throw new ArgumentNullException(nameof(groupMemberRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine("DEBUG: OnConnectedAsync called");
        var userId = GetUserId();
        Console.WriteLine($"DEBUG: userId = {userId ?? "NULL"}");
        
        if (string.IsNullOrEmpty(userId))
        {
            Context.Abort();
            return;
        }

        Console.WriteLine($"DEBUG: User {userId} connected to ChatHub");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            Console.WriteLine($"DEBUG: User {userId} disconnected from ChatHub");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGroup(string groupId)
    {
        Console.WriteLine($"DEBUG: JoinGroup called with groupId = {groupId}");
        
        if (string.IsNullOrWhiteSpace(groupId))
        {
            await Clients.Caller.SendAsync("Error", "Group ID is required");
            return;
        }

        var userId = GetUserId();
        Console.WriteLine($"DEBUG: JoinGroup userId = {userId ?? "NULL"}");
        
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        var isMember = await _groupMemberRepository.ExistsAsync(groupId, userId);
        Console.WriteLine($"DEBUG: isMember = {isMember}");
        
        if (!isMember)
        {
            Console.WriteLine($"DEBUG: User {userId} is NOT a member of group {groupId}");
            await Clients.Caller.SendAsync("Error", "You are not a member of this group. Please join the group through GroupsService first.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        Console.WriteLine($"DEBUG: User {userId} joined group {groupId}");
        
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
        Console.WriteLine($"DEBUG: User {userId} left group {groupId}");
        
        await Clients.Caller.SendAsync("LeftGroup", groupId);
    }

    public async Task SendMessage(SendMessageDto sendMessageDto)
    {
        Console.WriteLine("DEBUG: SendMessage called");
        
        if (sendMessageDto == null)
        {
            Console.WriteLine("DEBUG: sendMessageDto is NULL");
            await Clients.Caller.SendAsync("Error", "Message data is required");
            return;
        }

        Console.WriteLine($"DEBUG: SendMessage groupId = {sendMessageDto.GroupId}, content = {sendMessageDto.Content}");

        var userId = GetUserId();
        Console.WriteLine($"DEBUG: SendMessage userId = {userId ?? "NULL"}");
        
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }
        
        var userNickName = GetUserNickName();
        Console.WriteLine($"DEBUG: SendMessage userNickName = {userNickName ?? "NULL"}");
        
        if (string.IsNullOrEmpty(userNickName))
        {
            Console.WriteLine("DEBUG: userNickName is empty, returning error");
            await Clients.Caller.SendAsync("Error", "User nickname not found");
            return;
        }

        try
        {
            Console.WriteLine($"DEBUG: Calling _messageService.SendMessageAsync");
            var messageDto = await _messageService.SendMessageAsync(sendMessageDto, userId, userNickName);
            Console.WriteLine($"DEBUG: Message saved with Id = {messageDto.Id}");
            
            await Clients.Group(sendMessageDto.GroupId).SendAsync("ReceiveMessage", messageDto);
            Console.WriteLine($"DEBUG: Message {messageDto.Id} sent to group {sendMessageDto.GroupId} by user {userId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: Error sending message: {ex.Message}");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    private string? GetUserId()
    {
        if (Context.User == null)
        {
            Console.WriteLine("DEBUG: GetUserId - Context.User is NULL");
            return null;
        }

        Console.WriteLine("DEBUG: GetUserId - All claims:");
        foreach (var claim in Context.User.Claims)
        {
            Console.WriteLine($"DEBUG:   {claim.Type} = {claim.Value}");
        }

        var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($"DEBUG: GetUserId - ClaimTypes.NameIdentifier = {userId ?? "NULL"}");
        
        if (string.IsNullOrEmpty(userId))
        {
            userId = Context.User.FindFirstValue("sub");
            Console.WriteLine($"DEBUG: GetUserId - 'sub' claim = {userId ?? "NULL"}");
        }
        
        return userId;
    }

    private string? GetUserNickName()
    {
        if (Context.User == null)
        {
            Console.WriteLine("DEBUG: GetUserNickName - Context.User is NULL");
            return null;
        }

        var userNickName = Context.User.FindFirstValue(ClaimTypes.Name);
        Console.WriteLine($"DEBUG: GetUserNickName - ClaimTypes.Name = {userNickName ?? "NULL"}");

        if (string.IsNullOrEmpty(userNickName))
        {
            userNickName = Context.User.FindFirstValue("nickname");
            Console.WriteLine($"DEBUG: GetUserNickName - 'nickname' claim = {userNickName ?? "NULL"}");
        }
        
        return userNickName;
    }
}
