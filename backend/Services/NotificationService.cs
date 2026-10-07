using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;
using PollSystem.Services.Interfaces;

namespace PollSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;

        public NotificationService(INotificationRepository notificationRepo)
        {
            _notificationRepo = notificationRepo;
        }

        public Notification CreateNotification(Guid targetUserId, string content)
        {
            var notification = new Notification(targetUserId, content);
            _notificationRepo.Add(notification);
            return notification;
        }

        public IEnumerable<Notification> GetUserNotifications(Guid targetUserId)
        {
            return _notificationRepo.GetByUserId(targetUserId);
        }
    }
}