using Microsoft.EntityFrameworkCore;
using PollSystem.Data;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;

namespace PollSystem.Repositories
{
    public sealed class UserRepository : RepositoryBase<User>, IUserRepository
    {
        public UserRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public void AddCosmetic(UserCosmetic cosmetic) => DbContext.UserCosmetics.Add(cosmetic);

        public async Task<User?> FindByUsernameAsync(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return await QueryUsers().FirstOrDefaultAsync(user => user.Username == username.Trim());
        }

        public override async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            await QueryUsers().FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

        public async Task<IEnumerable<User>> GetAllAsync() => await QueryUsers()
            .Include(user => user.Followers)
            .OrderBy(user => user.Username)
            .AsSplitQuery()
            .ToListAsync();

        private IQueryable<User> QueryUsers() => DbContext.Users
            .Include(user => user.Following)
            .Include(user => user.Cosmetics);
    }
}