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

        public async Task<Notification> CreateNotificationAsync(Guid targetUserId, string content)
        {
            var notification = new Notification(targetUserId, content);
            _notificationRepo.Add(notification);
            await _notificationRepo.SaveStateAsync();
            return notification;
        }

        public IEnumerable<Notification> GetUserNotifications(Guid targetUserId)
        {
            return _notificationRepo.GetByUserId(targetUserId);
        }
    }
}
