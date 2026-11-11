using PollSystem.Models.Entities;

namespace PollSystem.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        void Add(Notification notification);
        IEnumerable<Notification> GetByUserId(Guid targetUserId);
    }
}