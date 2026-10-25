using PollSystem.Models.DTOs;
using PollSystem.Models.Entities;

namespace PollSystem.Services.Interfaces
{
    public interface IUserService
    {
        User? GetUser(string? username);
        Task<(User User, string Token)> RegisterAsync(string username, string password);
        Task<(User User, string Token)> LoginAsync(string username, string password);
        UserProfileDto? GetUserProfile(Guid userId);
        IEnumerable<UserProfileDto> GetAllUserProfiles();
        Task FollowUserAsync(Guid followerId, Guid followedId);
        Task UnfollowUserAsync(Guid followerId, Guid followedId);
        Task SaveStateAsync();
        Task<(int RemainingPoints, List<string> Cosmetics)> BuyCosmeticAsync(Guid userId, string item);
        IEnumerable<UserProfileDto> GetFollowingProfiles(Guid userId);
        IEnumerable<FollowingVoteDto> GetFollowingVotes(Guid userId);
    }
}
