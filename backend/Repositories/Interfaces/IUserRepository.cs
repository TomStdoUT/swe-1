using PollSystem.Models.Entities;

namespace PollSystem.Repositories.Interfaces
{
    public interface IUserRepository
    {
        void Add(User user);
        void AddCosmetic(UserCosmetic cosmetic);
        Task<User?> FindByUsernameAsync(string? username);
        Task<User?> FindByIdAsync(Guid id);
        Task<IEnumerable<User>> GetAllAsync();
    }
}