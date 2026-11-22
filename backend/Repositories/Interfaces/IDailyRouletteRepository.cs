using PollSystem.Models.Entities;

namespace PollSystem.Repositories.Interfaces
{
    public interface IDailyRouletteRepository : IRepository<DailyRouletteSpin>
    {
        Task<DailyRouletteSpin?> GetLatestByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}