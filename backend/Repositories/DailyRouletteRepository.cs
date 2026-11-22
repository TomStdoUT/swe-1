using Microsoft.EntityFrameworkCore;
using PollSystem.Data;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;

namespace PollSystem.Repositories
{
    public sealed class DailyRouletteRepository : RepositoryBase<DailyRouletteSpin>, IDailyRouletteRepository
    {
        public DailyRouletteRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<DailyRouletteSpin?> GetLatestByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(spin => spin.UserId == userId)
                .OrderByDescending(spin => spin.SpunAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}