using Domain.Enums;
namespace Domain.Entities;

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? TokensUsed { get; set; }

    public Conversation Conversation { get; set; } = null!;
}
