using PollSystem.Models.DTOs;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;
using PollSystem.Services.Interfaces;

namespace PollSystem.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IPollRepository _pollRepo;

        public UserService(IUserRepository userRepo, IPollRepository pollRepo)
        {
            _userRepo = userRepo;
            _pollRepo = pollRepo;
        }

        public User? GetUser(string? username) => _userRepo.FindByUsername(username);

        public async Task<User> LoginAsync(string username)
        {
            var user = _userRepo.GetOrCreate(username);
            await _userRepo.SaveStateAsync();
            return user;
        }

        public UserProfileDto? GetUserProfile(string? username)
        {
            var user = _userRepo.FindByUsername(username);
            if (user == null) return null;
            return CreateProfileDto(user);
        }

        public IEnumerable<UserProfileDto> GetAllUserProfiles()
        {
            return _userRepo.GetAll().Select(CreateProfileDto);
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
                accuracyRate
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
    }
}