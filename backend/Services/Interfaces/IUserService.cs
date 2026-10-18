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
        Task FollowUserAsync(string? followerUsername, string followedUsername);
        Task UnfollowUserAsync(string? followerUsername, string followedUsername);
        Task SaveStateAsync();
        Task<(int RemainingPoints, List<string> Cosmetics)> BuyCosmeticAsync(string? username, string item);
        IEnumerable<UserProfileDto> GetFollowingProfiles(string? username);
        IEnumerable<FollowingVoteDto> GetFollowingVotes(string? username);
    }
}