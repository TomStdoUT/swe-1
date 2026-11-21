using Microsoft.EntityFrameworkCore;
using PollSystem.Data;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;

namespace PollSystem.Repositories
{
    public sealed class DailyRouletteRepository : IDailyRouletteRepository
    {
        private readonly AppDbContext _db;

        public DailyRouletteRepository(AppDbContext db)
        {
            _db = db;
        }

        public void Add(DailyRouletteSpin spin)
        {
            _db.DailyRouletteSpins.Add(spin);
        }

        public async Task<DailyRouletteSpin?> GetLatestByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _db.DailyRouletteSpins
                .Where(spin => spin.UserId == userId)
                .OrderByDescending(spin => spin.SpunAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}