using PollSystem.Models.Entities;

namespace PollSystem.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task LoadStateAsync();
        Task SaveStateAsync();
        void Add(User user);
        User? FindByUsername(string? username);
        IEnumerable<User> GetAll();
    }
}