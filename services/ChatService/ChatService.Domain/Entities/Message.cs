using ChatService.Domain.DomainEvents;
using ChatService.Domain.Exceptions;

namespace ChatService.Domain.Entities;

public class Message : BaseEntity
{
    public string GroupId { get; private set; } = string.Empty;
    
    public string UserId {get; private set;} = string.Empty;
    
    public string UserNickName { get; private set; } = string.Empty;
    
    public string Content { get; private set; } = string.Empty;

    public MessageType Type { get; private set; } = MessageType.Text;

    // Глобальный ID стикера из каталога (только для Type == Sticker)
    public int? StickerId { get; private set; }

    // Текст стикера в Content: его покажут клиенты, которые не знают про стикеры (старые версии приложения)
    public const string StickerFallbackContent = "[Стикер]";

    private Message() { }

    public static Message Create(string Id, string groupId, string userId, string content, string userNickName)
    {
        EnsureIds(Id, groupId, userId);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Message content cannot be empty");
        }

        if (content.Length > 2000)
        {
            throw new DomainException("Message content cannot exceed 2000 characters");
        }

        var message = new Message
        {
            Id = Id,
            GroupId = groupId,
            UserId = userId,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow,
            UserNickName = userNickName
        };
        
        message.AddDomainEvent(new MessageSentEvent(message.Id, message.GroupId, message.UserId, message.Content, message.CreatedAt));

        return message;
    }

    public static Message CreateSticker(string Id, string groupId, string userId, int stickerId, string userNickName)
    {
        EnsureIds(Id, groupId, userId);

        if (stickerId <= 0)
        {
            throw new DomainException("Sticker ID must be positive");
        }

        var message = new Message
        {
            Id = Id,
            GroupId = groupId,
            UserId = userId,
            Content = StickerFallbackContent,
            Type = MessageType.Sticker,
            StickerId = stickerId,
            CreatedAt = DateTime.UtcNow,
            UserNickName = userNickName
        };

        message.AddDomainEvent(new MessageSentEvent(message.Id, message.GroupId, message.UserId, message.Content, message.CreatedAt, stickerId));

        return message;
    }

    private static void EnsureIds(string id, string groupId, string userId)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainException("Message ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(groupId))
        {
            throw new DomainException("Group ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("User ID cannot be empty");
        }
    }
}