using PollSystem.Models.DTOs;
using PollSystem.Models.Entities;

namespace PollSystem.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetUserAsync(string? username);
        Task<(User User, string Token)> RegisterAsync(string username, string password);
        Task<(User User, string Token)> LoginAsync(string username, string password);
        Task<UserProfileDto?> GetUserProfileAsync(Guid userId);
        Task<IEnumerable<UserProfileDto>> GetAllUserProfilesAsync();
        Task FollowUserAsync(Guid followerId, Guid followedId);
        Task UnfollowUserAsync(Guid followerId, Guid followedId);
        Task SaveStateAsync();
        Task<PollSystem.Models.Common.OperationResult<(int RemainingPoints, List<string> Cosmetics)>> BuyCosmeticAsync(Guid userId, string item);
        Task<IEnumerable<UserProfileDto>> GetFollowingProfilesAsync(Guid userId);
        Task<IEnumerable<FollowingVoteDto>> GetFollowingVotesAsync(Guid userId);
    }
}