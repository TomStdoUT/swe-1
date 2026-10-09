using PollSystem.Models.DTOs;
using PollSystem.Models.Entities;

namespace PollSystem.Services.Interfaces
{
    public interface IUserService
    {
        User? GetUser(string? username);
        Task<(User User, string Token)> RegisterAsync(string username, string password);
        Task<(User User, string Token)> LoginAsync(string username, string password);
        UserProfileDto? GetUserProfile(string? username);
        IEnumerable<UserProfileDto> GetAllUserProfiles();
    }
}