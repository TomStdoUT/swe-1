using PollSystem.Models.Entities;

namespace PollSystem.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        void AddCosmetic(UserCosmetic cosmetic);
        Task<User?> FindByUsernameAsync(string? username);
        Task<IEnumerable<User>> GetAllAsync();
    }
}