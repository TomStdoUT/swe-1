using PollSystem.Models.Common;
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
        Task<OperationResult<User>> BuyCosmeticAsync(Guid userId, string item);
        Task<IEnumerable<UserProfileDto>> GetFollowingProfilesAsync(Guid userId);
        Task<IEnumerable<FollowingVoteDto>> GetFollowingVotesAsync(Guid userId);
    }
}