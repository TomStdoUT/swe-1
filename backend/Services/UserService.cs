using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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

        public UserService(IUserRepository userRepo, IPollRepository pollRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _pollRepo = pollRepo;
            _config = config;
        }

        public User? GetUser(string? username) => _userRepo.FindByUsername(username);

        public async Task<(User User, string Token)> RegisterAsync(string username, string password)
        {
            if (_userRepo.FindByUsername(username) != null)
                throw new InvalidOperationException("Username is already taken.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User(username, passwordHash);

            _userRepo.Add(user);
            await _userRepo.SaveStateAsync();

            var token = GenerateJwtToken(user);
            return (user, token);
        }

        public async Task<(User User, string Token)> LoginAsync(string username, string password)
        {
            var user = _userRepo.FindByUsername(username);

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
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Username),
                new Claim("userId", user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public UserProfileDto? GetUserProfile(string? username)
        {
            var user = _userRepo.FindByUsername(username);
            
            if (user == null) 
                return null;
                
            return CreateProfileDto(user);
        }

        public IEnumerable<UserProfileDto> GetAllUserProfiles()
        {
            return _userRepo.GetAll().Select(CreateProfileDto);
        }

        public async Task FollowUserAsync(string? followerUsername, string followedUsername)
        {
            var follower = _userRepo.FindByUsername(followerUsername);
            var followed = _userRepo.FindByUsername(followedUsername);

            if (follower is null)
                throw new UnauthorizedAccessException("Sign in to follow users.");
            if (followed is null)
                throw new KeyNotFoundException("User not found.");
            if (follower.Id == followed.Id)
                throw new InvalidOperationException("You cannot follow yourself.");

            if (!follower.IsFollowing(followed.Id))
            {
                follower.FollowingUserIds.Add(followed.Id);
                await _userRepo.SaveStateAsync();
            }
        }

        public async Task UnfollowUserAsync(string? followerUsername, string followedUsername)
        {
            var follower = _userRepo.FindByUsername(followerUsername);
            var followed = _userRepo.FindByUsername(followedUsername);

            if (follower is null)
                throw new UnauthorizedAccessException("Sign in to follow users.");
            if (followed is null)
                throw new KeyNotFoundException("User not found.");

            if (follower.FollowingUserIds.Remove(followed.Id))
                await _userRepo.SaveStateAsync();
        }

        public async Task SaveStateAsync() => await _userRepo.SaveStateAsync();

        public IEnumerable<UserProfileDto> GetFollowingProfiles(string? username)
        {
            var user = _userRepo.FindByUsername(username);
            if (user is null) return Enumerable.Empty<UserProfileDto>();

            return user.FollowingUserIds
                .Select(_userRepo.FindById)
                .Where(followed => followed is not null)
                .Select(followed => CreateProfileDto(followed!));
        }

        public IEnumerable<FollowingVoteDto> GetFollowingVotes(string? username)
        {
            var user = _userRepo.FindByUsername(username);
            if (user is null) return Enumerable.Empty<FollowingVoteDto>();

            var followedIds = user.FollowingUserIds.ToHashSet();
            var usersById = _userRepo.GetAll().ToDictionary(followed => followed.Id);

            return _pollRepo.GetAll()
                .Where(poll => poll.IsResolved)
                .SelectMany(poll => poll.Votes
                    .Where(vote => followedIds.Contains(vote.UserId) && usersById.ContainsKey(vote.UserId))
                    .Select(vote => new FollowingVoteDto(
                        poll.Id,
                        poll.Question,
                        poll.Category,
                        usersById[vote.UserId].Username,
                        vote.OptionIndices
                            .Where(index => index >= 0 && index < poll.Options.Count)
                            .Select(index => poll.Options[index].text)
                            .ToList(),
                        vote.OptionIndices.Any(poll.GetWinningOptions().Contains),
                        poll.ClosingTime
                    )))
                .OrderByDescending(vote => vote.PollClosedAt)
                .Take(50);
        }

        private UserProfileDto CreateProfileDto(User user)
        {
            var (correctPredictions, totalResolvedVotes) = CalculateAccuracyStats(user.Id);
            var accuracyRate = totalResolvedVotes == 0
                ? (double?)null
                : Math.Round(correctPredictions * 100.0 / totalResolvedVotes, 1);

            return new UserProfileDto(
                user.Id,
                user.Username,
                user.PointsBalance,
                correctPredictions,
                totalResolvedVotes,
                accuracyRate,
                _userRepo.GetAll().Count(follower => follower.IsFollowing(user.Id)),
                user.FollowingUserIds.Count,
                user.PurchasedCosmetics ?? new List<string>()
            );
        }

        private (int CorrectPredictions, int TotalResolvedVotes) CalculateAccuracyStats(Guid userId)
        {
            var correctPredictions = 0;
            var totalResolvedVotes = 0;

            foreach (var poll in _pollRepo.GetAll().Where(p => p.IsClosed()))
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

        public async Task<(int RemainingPoints, List<string> Cosmetics)> BuyCosmeticAsync(string? username, string item)
        {
            var user = _userRepo.FindByUsername(username);

            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var storePrices = new Dictionary<string, int>
            {
                { "gold_name", 20 },
                { "glow_border", 20 },
                { "vip_badge", 20 }
            };

            if (!storePrices.TryGetValue(item, out int cost))
                throw new ArgumentException("Item does not exist.");

            if (user.PurchasedCosmetics.Contains(item))
                throw new InvalidOperationException("You already own this cosmetic!");

            if (user.PointsBalance < cost)
                throw new InvalidOperationException($"Insufficient points balance (costs {cost} points).");

            user.PointsBalance -= cost;
            user.PurchasedCosmetics.Add(item);

            await _userRepo.SaveStateAsync();

            return (user.PointsBalance, user.PurchasedCosmetics);
        }
    }
}