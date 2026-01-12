using ChatService.Domain.DomainEvents;
using ChatService.Domain.Exceptions;

namespace ChatService.Domain.Entities;

public class Message : BaseEntity
{
    public string GroupId { get; private set; } = string.Empty;
    
    public string UserId {get; private set;} = string.Empty;
    
    public string Content { get; private set; } = string.Empty;
    
    private Message() { }

    public static Message Create(string Id, string groupId, string userId, string content)
    {
        if (string.IsNullOrWhiteSpace(Id))
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
            CreatedAt = DateTime.UtcNow
        };
        
        message.AddDomainEvent(new MessageSentEvent(message.Id, message.GroupId, message.UserId, message.Content, message.CreatedAt));

        return message;
    }
}