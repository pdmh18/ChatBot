namespace Domain.Entities;

public class Conversation : BaseEntity
{
    public string Title { get; set; } = "New Chat";
    public Guid UserId { get; set; }
    public Guid? ProjectId { get; set; }

    public User User { get; set; } = null!;
    public Project? Project { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
