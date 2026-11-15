using Microsoft.EntityFrameworkCore;
using PollSystem.Data;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;

namespace PollSystem.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _db;

        public NotificationRepository(AppDbContext db)
        {
            _db = db;
        }

        public void Add(Notification notification)
        {
            _db.Notifications.Add(notification);
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid targetUserId)
        {
            return await _db.Notifications
                .Where(notification => notification.TargetUserId == targetUserId)
                .OrderByDescending(notification => notification.CreatedAt)
                .ToListAsync();
        }
    }
}
