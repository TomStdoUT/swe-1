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
        private readonly INotificationRepository _notificationRepo;

        public PollService(IPollRepository pollRepo, IUserRepository userRepo, INotificationRepository notificationRepo)
        {
            _pollRepo = pollRepo;
            _userRepo = userRepo;
            _notificationRepo = notificationRepo;
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
                    p.IsDailyChallenge,
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
    
        public async Task ResolveClosedPollsAsync()
        {
            bool anyResolved = false;
            var allUsers = _userRepo.GetAll().ToDictionary(u => u.Id);

            foreach (var poll in _pollRepo.GetAll().Where(p => p.IsClosed() && !p.IsResolved))
            {
                var payouts = poll.Resolve();
                
                int totalVotes = poll.Votes.Count();
                int totalComments = poll.Comments?.Count ?? 0;

                foreach (var payout in payouts)
                {
                    if (allUsers.TryGetValue(payout.Key, out var user))
                    {
                        user.PointsBalance += payout.Value;
                    }
                }

                var userVotes = poll.Votes.GroupBy(v => v.UserId);
                
                foreach (var userVoteGroup in userVotes)
                {
                    var userId = userVoteGroup.Key;
                    bool isWinner = payouts.ContainsKey(userId);
                    int totalBetted = userVoteGroup.Sum(v => v.BettedPoints);
                    int totalPayout = isWinner ? payouts[userId] : 0;
                    int netProfit = totalPayout - totalBetted;

                    string content = isWinner && netProfit >= 0
                        ? $"Poll '{poll.Question}' has ended. Your choice won! You won {netProfit} points. Total votes: {totalVotes}, Comments: {totalComments}."
                        : $"Poll '{poll.Question}' has ended. Your choice lost! You lost your {totalBetted} betted points. Total votes: {totalVotes}, Comments: {totalComments}.";

                    _notificationRepo.Add(new Notification(
                        targetUserId: userId,
                        content: content,
                        type: NotificationType.PollResult
                    ));
                }

                anyResolved = true;
            }

            if (anyResolved)
            {
                await _pollRepo.SaveStateAsync();
                await _userRepo.SaveStateAsync();
            }
        }

        public async Task EnsureDailyQuestionAsync()
        {
            var localNow = DateTime.Now;
            bool exists = _pollRepo.GetAll().Any(p => p.IsDailyChallenge && p.CreatedAt.ToLocalTime().Date == localNow.Date);
            if (exists) return;

            var questions = new[]
            {
                new { Question = "Is today a nice day?", Category = "Fun", Options = new List<string> { "Yes", "No" } },
                new { Question = "Will it rain tomorrow?", Category = "Fun", Options = new List<string> { "Yes", "No" } },
                new { Question = "Is remote work better than office work?", Category = "Technology", Options = new List<string> { "Remote", "Office" } },
                new { Question = "Will artificial intelligence transform software engineering completely by 2030?", Category = "Technology", Options = new List<string> { "Yes", "No" } },
                new { Question = "Is coffee better than tea?", Category = "Fun", Options = new List<string> { "Coffee", "Tea" } }
            };

            var random = new Random();
            var selected = questions[random.Next(questions.Length)];

            var poll = new Poll
            {
                CreatorId = Guid.Empty,
                Question = "[Daily Challenge] " + selected.Question,
                Category = selected.Category,
                Tags = new List<string> { "DailyChallenge", "Featured" },
                Options = selected.Options.Select(opt => new PollOption(opt)).ToList(),
                ClosingTime = localNow.Date.AddDays(1).ToUniversalTime(),
                AllowMultiple = false,
                IsDailyChallenge = true
            };

            _pollRepo.Add(poll);
            await _pollRepo.SaveStateAsync();
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