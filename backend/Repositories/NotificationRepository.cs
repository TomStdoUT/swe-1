using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;

namespace PollSystem.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly List<Notification> _notifications = new();

        public void Add(Notification notification)
        {
            _notifications.Add(notification);
        }

        public IEnumerable<Notification> GetByUserId(Guid targetUserId)
        {
            return _notifications
                .Where(n => n.TargetUserId == targetUserId)
                .OrderByDescending(n => n.CreatedAt);
        }
    }
}