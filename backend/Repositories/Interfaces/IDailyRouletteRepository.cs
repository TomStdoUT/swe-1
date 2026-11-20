using PollSystem.Models.Entities;

namespace PollSystem.Repositories.Interfaces
{
    public interface IDailyRouletteRepository
    {
        void Add(DailyRouletteSpin spin);
        Task<DailyRouletteSpin?> GetLatestByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}