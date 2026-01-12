using ChatService.Application.DTOs;

namespace ChatService.Application.Services;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(SendMessageDto sendMessageDto, string userId, CancellationToken cancellationToken = default);
    Task<MessageDto?> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default);
    Task<GetMessagesResultDto> GetMessagesAsync(GetMessagesDto getMessagesDto, CancellationToken cancellationToken = default);
}
