using PollSystem.Models.Entities;

namespace PollSystem.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task LoadStateAsync();
        Task SaveStateAsync();
        User GetOrCreate(string username);
        User? FindByUsername(string? username);
        IEnumerable<User> GetAll();
    }
}