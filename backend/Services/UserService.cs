using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using PollSystem.Models.DTOs;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;
using PollSystem.Services.Interfaces;
using PollSystem.Extensions;

namespace PollSystem.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IPollRepository _pollRepo;
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUserRepository userRepo, IPollRepository pollRepo, IConfiguration config, IUnitOfWork unitOfWork)
        {
            _userRepo = userRepo;
            _pollRepo = pollRepo;
            _config = config;
            _unitOfWork = unitOfWork;
        }

        public async Task<User?> GetUserAsync(string? username) => await _userRepo.FindByUsernameAsync(username);

        public async Task<(User User, string Token)> RegisterAsync(string username, string password)
        {
            var existingUser = await _userRepo.FindByUsernameAsync(username);
            if (existingUser != null)
                throw new InvalidOperationException("Username is already taken.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User(username, passwordHash);

            _userRepo.Add(user);
            await _unitOfWork.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return (user, token);
        }

        public async Task<(User User, string Token)> LoginAsync(string username, string password)
        {
            var user = await _userRepo.FindByUsernameAsync(username);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid username or password.");

            var token = GenerateJwtToken(user);
            return (user, token);
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? "a_very_long_and_secure_secret_key_for_swe_1_project_12345!";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
        {
            var user = await _userRepo.FindByIdAsync(userId);
            if (user == null) return null;

            var users = await _userRepo.GetAllAsync();
            var closedPolls = await _pollRepo.GetClosedPollsAsync();
            var allUsersList = users.ToList();

            return CreateProfileDto(user, allUsersList, closedPolls);
        }

        public async Task<IEnumerable<UserProfileDto>> GetAllUserProfilesAsync()
        {
            var users = await _userRepo.GetAllAsync();
            var closedPolls = await _pollRepo.GetClosedPollsAsync();
            var allUsersList = users.ToList();

            return allUsersList.Select(user => CreateProfileDto(user, allUsersList, closedPolls));
        }

        public async Task FollowUserAsync(Guid followerId, Guid followedId)
        {
            var follower = await _userRepo.FindByIdAsync(followerId);
            var followed = await _userRepo.FindByIdAsync(followedId);

            if (follower is null)
                throw new UnauthorizedAccessException("Sign in to follow users.");
            if (followed is null)
                throw new KeyNotFoundException("User not found.");
            if (follower.Id == followed.Id)
                throw new InvalidOperationException("You cannot follow yourself.");

            if (!follower.IsFollowing(followed.Id))
            {
                follower.Following.Add(new UserFollow
                {
                    FollowerId = follower.Id,
                    Follower = follower,
                    FollowedId = followed.Id,
                    Followed = followed
                });
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task UnfollowUserAsync(Guid followerId, Guid followedId)
        {
            var follower = await _userRepo.FindByIdAsync(followerId);
            var followed = await _userRepo.FindByIdAsync(followedId);

            if (follower is null)
                throw new UnauthorizedAccessException("Sign in to follow users.");
            if (followed is null)
                throw new KeyNotFoundException("User not found.");

            var follow = follower.Following.FirstOrDefault(link => link.FollowedId == followed.Id);
            if (follow is not null)
            {
                follower.Following.Remove(follow);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task SaveStateAsync() => await _unitOfWork.SaveChangesAsync();

        public async Task<IEnumerable<UserProfileDto>> GetFollowingProfilesAsync(Guid userId)
        {
            var user = await _userRepo.FindByIdAsync(userId);
            if (user is null) return Enumerable.Empty<UserProfileDto>();

            var users = await _userRepo.GetAllAsync();
            var closedPolls = await _pollRepo.GetClosedPollsAsync();
            var allUsersList = users.ToList();

            var profiles = new List<UserProfileDto>();
            foreach (var follow in user.Following)
            {
                var followed = allUsersList.FirstOrDefault(u => u.Id == follow.FollowedId);
                if (followed is not null)
                {
                    profiles.Add(CreateProfileDto(followed, allUsersList, closedPolls));
                }
            }
            return profiles;
        }

        public async Task<IEnumerable<FollowingVoteDto>> GetFollowingVotesAsync(Guid userId)
        {
            var user = await _userRepo.FindByIdAsync(userId);
            if (user is null) return Enumerable.Empty<FollowingVoteDto>();

            var followedIds = user.Following.Select(follow => follow.FollowedId).ToHashSet();
            var allUsers = await _userRepo.GetAllAsync();
            var usersById = allUsers.ToDictionary(followed => followed.Id);

            var polls = await _pollRepo.GetAllAsync();
            var result = new List<FollowingVoteDto>();

            foreach (var poll in polls.Where(poll => poll.IsResolved))
            {
                var options = poll.Options.OrderBy(option => option.Position).ToList();
                foreach (var vote in poll.Votes)
                {
                    if (followedIds.Contains(vote.UserId) && usersById.ContainsKey(vote.UserId))
                    {
                        result.Add(new FollowingVoteDto(
                            poll.Id,
                            poll.Question,
                            poll.Category,
                            usersById[vote.UserId].Username,
                            vote.OptionIndices
                                .Where(index => index >= 0 && index < options.Count)
                                .Select(index => options[index].text)
                                .ToList(),
                            vote.OptionIndices.Any(poll.GetWinningOptions().Contains),
                            poll.ClosingTime
                        ));
                    }
                }
            }

            return result.OrderByDescending(vote => vote.PollClosedAt).Take(50);
        }

        private UserProfileDto CreateProfileDto(User user, IEnumerable<User> allUsers, IEnumerable<Poll> closedPolls)
        {
            var (correctPredictions, totalResolvedVotes) = CalculateAccuracyStats(user.Id, closedPolls);
            var accuracyRate = totalResolvedVotes == 0
                ? (double?)null
                : Math.Round(correctPredictions * 100.0 / totalResolvedVotes, 1);

            int followerCount = allUsers.Count(follower => follower.IsFollowing(user.Id));

            return new UserProfileDto(
                user.Id,
                user.Username,
                user.PointsBalance,
                correctPredictions,
                totalResolvedVotes,
                accuracyRate,
                followerCount,
                user.Following.Count,
                user.Cosmetics.Select(cosmetic => cosmetic.Name).ToList()
            );
        }

        private (int CorrectPredictions, int TotalResolvedVotes) CalculateAccuracyStats(Guid userId, IEnumerable<Poll> closedPolls)
        {
            var correctPredictions = 0;
            var totalResolvedVotes = 0;

            foreach (var poll in closedPolls)
            {
                var votes = poll.Votes.ToList();
                var voteCounts = votes
                    .SelectMany(vote => vote.OptionIndices)
                    .GroupBy(optionIndex => optionIndex)
                    .ToDictionary(group => group.Key, group => group.Count());

                if (voteCounts.Count == 0) continue;

                var highestVoteCount = voteCounts.Values.Max();
                var winningOptions = voteCounts
                    .Where(entry => entry.Value == highestVoteCount)
                    .Select(entry => entry.Key)
                    .ToHashSet();

                foreach (var vote in votes.Where(vote => vote.UserId == userId))
                {
                    totalResolvedVotes++;
                    if (vote.OptionIndices.Any(winningOptions.Contains))
                    {
                        correctPredictions++;
                    }
                }
            }

            return (correctPredictions, totalResolvedVotes);
        }

        public async Task<PollSystem.Models.Common.OperationResult<(int RemainingPoints, List<string> Cosmetics)>> BuyCosmeticAsync(Guid userId, string item)
        {
            var user = await _userRepo.FindByIdAsync(userId);

            if (user == null)
                return PollSystem.Models.Common.OperationResult<(int, List<string>)>.Failure("User not found.");

            var storePrices = new Dictionary<string, int>
            {
                { "gold_name", 20 },
                { "glow_border", 20 },
                { "vip_badge", 20 }
            };

            if (!storePrices.TryGetValue(item, out int cost))
                return PollSystem.Models.Common.OperationResult<(int, List<string>)>.Failure("Item does not exist.");

            if (user.Cosmetics.Any(cosmetic => cosmetic.Name == item))
                return PollSystem.Models.Common.OperationResult<(int, List<string>)>.Failure("You already own this cosmetic!");

            if (user.PointsBalance < cost)
                return PollSystem.Models.Common.OperationResult<(int, List<string>)>.Failure($"Insufficient points balance (costs {cost} points).");

            user.PointsBalance -= cost;
            var cosmetic = new UserCosmetic { UserId = user.Id, Name = item };
            user.Cosmetics.Add(cosmetic);
            _userRepo.AddCosmetic(cosmetic);

            await _unitOfWork.SaveChangesAsync();

            var result = PollSystem.Models.Common.OperationResult<(int, List<string>)>.Success(
                (user.PointsBalance, user.Cosmetics.Select(c => c.Name).ToList())
            );

            return result;
        }
    }
}