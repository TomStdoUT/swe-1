using PollSystem.Models.Entities;

namespace PollSystem.Services.Interfaces
{
    public interface INotificationService
    {
        Notification CreateNotification(Guid targetUserId, string content);
        IEnumerable<Notification> GetUserNotifications(Guid targetUserId);
    }
}