using Microsoft.EntityFrameworkCore;
using PollSystem.Data;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;

namespace PollSystem.Repositories
{
    public class NotificationRepository : RepositoryBase<Notification>, INotificationRepository
    {
        public NotificationRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid targetUserId)
        {
            return await DbSet
                .Where(notification => notification.TargetUserId == targetUserId)
                .OrderByDescending(notification => notification.CreatedAt)
                .ToListAsync();
        }
    }
}