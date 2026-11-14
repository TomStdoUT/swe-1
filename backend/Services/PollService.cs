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
        private readonly IUnitOfWork _unitOfWork;

        public PollService(IPollRepository pollRepo, IUserRepository userRepo, INotificationRepository notificationRepo, IUnitOfWork unitOfWork)
        {
            _pollRepo = pollRepo;
            _userRepo = userRepo;
            _notificationRepo = notificationRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<PollFeedDto>> GetFeedForUserAsync(string? username)
        {
            var user = await _userRepo.FindByUsernameAsync(username);
            var activeUserId = user?.Id ?? GenerateDeterministicId(username);

            var polls = await _pollRepo.GetAllAsync();

            return polls
            .OrderByDescending(p => p.ClosingTime)
            .Select(p => new PollFeedDto(
                p.Id,
                p.CreatorId,
                p.Question,
                p.Category,
                p.Tags.OrderBy(tag => tag.Value).Select(tag => tag.Value).ToList(),
                p.Options.OrderBy(option => option.Position).Select(option => option.text).ToList(),
                p.ClosingTime,
                p.CreatedAt,
                p.AllowMultiple,
                p.IsDailyChallenge,
                p.IsResolved,
                p.Comments.Select(c => new CommentDto(c.AuthorName, c.Content, c.CreatedAt)).ToList(),
                p.IsResolved ? p.GetWinningOptions() : new List<int>(),
                p.Votes.Where(v => v.UserId == activeUserId)
                       .Select(v => new UserVoteDto(v.OptionIndices, v.BettedPoints, v.IsEarlyBird))
                       .ToList(),
                p.Votes.Count()
            ));
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
                Tags = dto.Tags.Select(tag => new PollTag { Value = tag }).ToList(),
                Options = dto.Options.Select((option, position) => new PollOption(option)
                {
                    Position = position
                }).ToList(),
                ClosingTime = DateTime.UtcNow.AddSeconds(totalSeconds),
                AllowMultiple = dto.AllowMultiple
            };

            _pollRepo.Add(poll);
            await _unitOfWork.SaveChangesAsync();
            return poll;
        }

        public async Task<(int remainingPoints, bool isEarlyBird)> CastVoteAsync(Guid pollId, Guid userId, VoteDto dto)
        {
            var poll = await _pollRepo.GetByIdAsync(pollId);
            if (poll == null)
                throw new KeyNotFoundException("Poll not found.");

            var user = await _userRepo.FindByIdAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var vote = new Vote(user.Id, dto.OptionIndices, dto.BettedPoints);

            poll.CastVote(user, vote);

            var processedVote = poll.Votes.Last(v => v.UserId == user.Id);
            _pollRepo.AddVote(processedVote);

            await _unitOfWork.SaveChangesAsync();

            return (user.PointsBalance, processedVote.IsEarlyBird);
        }

        public async Task DeletePollAsync(Guid pollId, Guid requestingUserId)
        {
            var poll = await _pollRepo.GetByIdAsync(pollId);

            if (poll == null)
                throw new KeyNotFoundException("Poll not found.");

            if (poll.CreatorId != requestingUserId)
                throw new UnauthorizedAccessException("Not poll owner.");

            _pollRepo.Remove(poll);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<Comment> AddCommentAsync(Guid pollId, Guid authorId, string authorName, string content)
        {
            var poll = await _pollRepo.GetByIdAsync(pollId);

            if (poll == null)
                throw new KeyNotFoundException("Poll not found.");

            var comment = new Comment(authorId, authorName, content);
            poll.Comments.Add(comment);
            _pollRepo.AddComment(comment);

            await _unitOfWork.SaveChangesAsync();
            return comment;
        }

        public async Task ResolveClosedPollsAsync()
        {
            bool anyResolved = false;
            var usersList = await _userRepo.GetAllAsync();
            var allUsers = usersList.ToDictionary(u => u.Id);

            foreach (var poll in _pollRepo.GetUnresolvedClosedPolls())
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
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task EnsureDailyQuestionAsync()
        {
            var localNow = DateTime.Now;
            var polls = await _pollRepo.GetAllAsync();
            bool exists = polls.Any(p => p.IsDailyChallenge && p.CreatedAt.ToLocalTime().Date == localNow.Date);
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
                Tags = new List<PollTag>
                {
                    new() { Value = "DailyChallenge" },
                    new() { Value = "Featured" }
                },
                Options = selected.Options.Select((option, position) => new PollOption(option)
                {
                    Position = position
                }).ToList(),
                ClosingTime = localNow.Date.AddDays(1).ToUniversalTime(),
                AllowMultiple = false,
                IsDailyChallenge = true
            };

            _pollRepo.Add(poll);
            await _unitOfWork.SaveChangesAsync();
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