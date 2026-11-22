using PollSystem.Models.Entities;

namespace PollSystem.Repositories.Interfaces
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetByUserIdAsync(Guid targetUserId);
    }
}