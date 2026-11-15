using PollSystem.Models.Entities;

namespace PollSystem.Services.Interfaces
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(Guid targetUserId, string content);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid targetUserId);
    }
}
