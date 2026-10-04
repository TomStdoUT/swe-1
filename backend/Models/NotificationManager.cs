using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PollSystem.Domain
{
    public class Notification
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid TargetUserId { get; init; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        public Notification() { }

        public Notification(Guid targetUserId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) 
                throw new ArgumentException("Notification content cannot be empty.");

            TargetUserId = targetUserId;
            Content = content;
        }
    }

    public class NotificationManager : IEnumerable<Notification>
    {
        private readonly List<Notification> _notifications = new();

        public Notification CreateNotification(Guid targetUserId, string content)
        {
            var notification = new Notification(targetUserId, content);
            _notifications.Add(notification);
            return notification;
        }

        // Only fetches notifications specifically belonging to the requesting user
        public IEnumerable<Notification> GetUserNotifications(Guid targetUserId) =>
            _notifications.Where(n => n.TargetUserId == targetUserId).OrderByDescending(n => n.CreatedAt);

        public IEnumerator<Notification> GetEnumerator() => _notifications.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}