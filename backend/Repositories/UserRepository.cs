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

        public User? FindByUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return QueryUsers().FirstOrDefault(user => user.Username == username.Trim());
        }

        public User? FindById(Guid id) => QueryUsers().FirstOrDefault(user => user.Id == id);

        public IEnumerable<User> GetAll() => QueryUsers()
            .Include(user => user.Followers)
            .OrderBy(user => user.Username)
            .AsSplitQuery()
            .ToList();

        public Task SaveStateAsync() => _db.SaveChangesAsync();

        private IQueryable<User> QueryUsers() => _db.Users
            .Include(user => user.Following)
            .Include(user => user.Cosmetics);
    }
}
