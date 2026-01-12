using ChatService.Application.DTOs;
using ChatService.Domain.Entities;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Interfaces;

namespace ChatService.Application.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;

    public MessageService(
        IMessageRepository messageRepository,
        IGroupMemberRepository groupMemberRepository)
    {
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _groupMemberRepository = groupMemberRepository ?? throw new ArgumentNullException(nameof(groupMemberRepository));
    }

    public async Task<MessageDto> SendMessageAsync(SendMessageDto sendMessageDto, string userId, CancellationToken cancellationToken = default)
    {
        if (sendMessageDto == null)
        {
            throw new ArgumentNullException(nameof(sendMessageDto));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        var isMember = await _groupMemberRepository.ExistsAsync(sendMessageDto.GroupId, userId, cancellationToken);
        
        if (!isMember)
        {
            throw new UnauthorizedToSendMessageException($"User {userId} is not a member of group {sendMessageDto.GroupId}");
        }

        var messageId = Guid.NewGuid().ToString();
        var message = Message.Create(messageId, sendMessageDto.GroupId, userId, sendMessageDto.Content);

        await _messageRepository.AddAsync(message, cancellationToken);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        return MapToMessageDto(message);
    }

    public async Task<MessageDto?> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Message ID cannot be empty", nameof(messageId));
        }

        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken);
        
        if (message == null)
        {
            return null;
        }

        return MapToMessageDto(message);
    }

    public async Task<GetMessagesResultDto> GetMessagesAsync(GetMessagesDto getMessagesDto, CancellationToken cancellationToken = default)
    {
        if (getMessagesDto == null)
        {
            throw new ArgumentNullException(nameof(getMessagesDto));
        }

        var totalCount = await _messageRepository.CountByGroupIdAsync(getMessagesDto.GroupId, cancellationToken);
        
        var offset = (getMessagesDto.Page - 1) * getMessagesDto.PageSize;
        var messages = await _messageRepository.GetByGroupIdAsync(
            getMessagesDto.GroupId, 
            getMessagesDto.PageSize, 
            offset, 
            cancellationToken);

        var messageDtos = messages.Select(MapToMessageDto).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)getMessagesDto.PageSize);

        return new GetMessagesResultDto
        {
            Messages = messageDtos,
            TotalCount = totalCount,
            Page = getMessagesDto.Page,
            PageSize = getMessagesDto.PageSize,
            TotalPages = totalPages
        };
    }

    private static MessageDto MapToMessageDto(Message message)
    {
        return new MessageDto
        {
            Id = message.Id,
            GroupId = message.GroupId,
            UserId = message.UserId,
            Content = message.Content,
            CreatedAt = message.CreatedAt
        };
    }
}
