using PollSystem.Models.DTOs;
using PollSystem.Models.Entities;

namespace PollSystem.Services.Interfaces
{
    public interface IUserService
    {
        User? GetUser(string? username);
        Task<User> LoginAsync(string username);
        UserProfileDto? GetUserProfile(string? username);
        IEnumerable<UserProfileDto> GetAllUserProfiles();
    }
}