using Microsoft.EntityFrameworkCore;
using PollSystem.Data;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;

namespace PollSystem.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;

        public UserRepository(AppDbContext db)
        {
            _db = db;
        }

        public void Add(User user) => _db.Users.Add(user);

        public void AddCosmetic(UserCosmetic cosmetic) => _db.UserCosmetics.Add(cosmetic);

        public async Task<User?> FindByUsernameAsync(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return await QueryUsers().FirstOrDefaultAsync(user => user.Username == username.Trim());
        }

        public async Task<User?> FindByIdAsync(Guid id) => await QueryUsers().FirstOrDefaultAsync(user => user.Id == id);

        public async Task<IEnumerable<User>> GetAllAsync() => await QueryUsers()
            .Include(user => user.Followers)
            .OrderBy(user => user.Username)
            .AsSplitQuery()
            .ToListAsync();

        private IQueryable<User> QueryUsers() => _db.Users
            .Include(user => user.Following)
            .Include(user => user.Cosmetics);
    }
}