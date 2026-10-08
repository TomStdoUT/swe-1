using PollSystem.Models.Entities;
using PollSystem.Models.DTOs;
using PollSystem.Repositories.Interfaces;
using PollSystem.Services.Interfaces;

namespace PollSystem.Services
{
    public class PollService : IPollService
    {
        private readonly IPollRepository _pollRepo;
        private readonly IUserRepository _userRepo;

        public PollService(IPollRepository pollRepo, IUserRepository userRepo)
        {
            _pollRepo = pollRepo;
            _userRepo = userRepo;
        }

        public IEnumerable<object> GetFeedForUser(string? username)
        {
            var user = _userRepo.FindByUsername(username);
            var activeUserId = user?.Id ?? GenerateDeterministicId(username);

            return _pollRepo.GetAll()
                .OrderByDescending(p => p.ClosingTime)
                .Select(p => new
                {
                    p.Id,
                    p.CreatorId,
                    p.Question,
                    p.Category,
                    p.Tags,
                    p.Options,
                    p.ClosingTime,
                    p.CreatedAt,
                    p.AllowMultiple,
                    p.Comments,
                    p.IsResolved,
                    WinningOptions = p.IsResolved ? p.GetWinningOptions() : new List<int>(),
                    Votes = p.Votes.Where(v => v.UserId == activeUserId).ToList(),
                    TotalVotesCount = p.Votes.Count()
                });
        }

        public async Task<Poll> CreatePollAsync(Guid creatorId, PollCreateDto dto)
        {
            var totalSeconds = dto.DurationSeconds;

            if (totalSeconds < 60)
                throw new ArgumentException("Poll must be at least 60 seconds.");

            var poll = new Poll
            {
                CreatorId = creatorId,
                Question = dto.Question,
                Category = dto.Category,
                Tags = dto.Tags,
                Options = dto.Options.Select(opt => new PollOption(opt)).ToList(),
                ClosingTime = DateTime.UtcNow.AddSeconds(totalSeconds),
                AllowMultiple = dto.AllowMultiple
            };

            _pollRepo.Add(poll);
            await _pollRepo.SaveStateAsync();
            return poll;
        }

        public async Task<(int remainingPoints, bool isEarlyBird)> CastVoteAsync(Guid pollId, User user, VoteDto dto)
        {
            var poll = _pollRepo.GetById(pollId);

            if (poll == null)
                throw new KeyNotFoundException("Poll not found.");

            var vote = new Vote(user.Id, dto.OptionIndices, dto.BettedPoints);

            poll.CastVote(user, vote);

            var processedVote = poll.Votes.Last(v => v.UserId == user.Id);

            await _pollRepo.SaveStateAsync();
            await _userRepo.SaveStateAsync();

            return (user.PointsBalance, processedVote.IsEarlyBird);
        }

        public async Task DeletePollAsync(Guid pollId, Guid requestingUserId)
        {
            var poll = _pollRepo.GetById(pollId);

            if (poll == null)
                throw new KeyNotFoundException("Poll not found.");

            if (poll.CreatorId != requestingUserId)
                throw new UnauthorizedAccessException("Not poll owner.");

            _pollRepo.Remove(poll);
            await _pollRepo.SaveStateAsync();
        }

        public async Task<Comment> AddCommentAsync(Guid pollId, Guid authorId, string authorName, string content)
        {
            var poll = _pollRepo.GetById(pollId);

            if (poll == null)
                throw new KeyNotFoundException("Poll not found.");

            var comment = new Comment(authorId, authorName, content);
            poll.Comments.Add(comment);

            await _pollRepo.SaveStateAsync();
            return comment;
        }

        // Safely orchestrates payouts across the userbase
        public async Task ResolveClosedPollsAsync()
        {
            bool anyResolved = false;
            var allUsers = _userRepo.GetAll().ToDictionary(u => u.Id);

            foreach (var poll in _pollRepo.GetAll().Where(p => p.IsClosed() && !p.IsResolved))
            {
                var payouts = poll.Resolve();

                foreach (var payout in payouts)
                {
                    if (allUsers.TryGetValue(payout.Key, out var user))
                    {
                        user.PointsBalance += payout.Value;
                    }
                }
                anyResolved = true;
            }

            if (anyResolved)
            {
                await _pollRepo.SaveStateAsync();
                await _userRepo.SaveStateAsync();
            }
        }

        private Guid GenerateDeterministicId(string? username)
        {
            if (string.IsNullOrWhiteSpace(username)) return Guid.Empty;
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(System.Text.Encoding.Default.GetBytes(username.Trim().ToLower()));
            return new Guid(hash);
        }
    }
}