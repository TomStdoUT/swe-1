using PollSystem.Models.Entities;

namespace PollSystem.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        void Add(Notification notification);
        Task<IEnumerable<Notification>> GetByUserIdAsync(Guid targetUserId);
    }
}
