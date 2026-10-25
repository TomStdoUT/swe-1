namespace PollSystem.Models.Entities
{
    public class Notification
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid TargetUserId { get; init; }
        public string Content { get; set; } = string.Empty;
        public NotificationType Type { get; init; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        public Notification() { }

        public Notification(Guid targetUserId, string content, NotificationType type = NotificationType.General)
        {
            if (string.IsNullOrWhiteSpace(content)) 
                throw new ArgumentException("Notification content cannot be empty.");

            TargetUserId = targetUserId;
            Content = content;
            Type = type;
        }
    }
}
