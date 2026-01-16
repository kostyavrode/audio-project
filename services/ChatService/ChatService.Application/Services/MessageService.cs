using ChatService.Application.DTOs;
using ChatService.Domain.Entities;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatService.Application.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly ILogger _logger; 

    public MessageService(
        IMessageRepository messageRepository,
        IGroupMemberRepository groupMemberRepository,
        ILogger<MessageService> logger)
    {
        _messageRepository = messageRepository;
        _groupMemberRepository = groupMemberRepository;
        _logger = logger;
    }

    public async Task<MessageDto> SendMessageAsync(SendMessageDto sendMessageDto, string userId, string userNickName, CancellationToken cancellationToken = default)
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
        var message = Message.Create(messageId, sendMessageDto.GroupId, userId, sendMessageDto.Content, userNickName);

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
        
        _logger.LogInformation("First message UserNickName: '{Nick}'", messages.FirstOrDefault()?.UserNickName);
        
        var messageDtos = messages.Select(m => {
            var dto = MapToMessageDto(m);
            _logger.LogInformation("Mapped: Id={Id}, UserNickName='{Nick}'", dto.Id, dto.UserNickName);
            return dto;
        }).ToList();

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
            CreatedAt = message.CreatedAt,
            UserNickName = message.UserNickName
        };
    }
}
