using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PollSystem.Models.DTOs;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;
using PollSystem.Services;
using Xunit;

namespace PollSystem.Tests.Unit
{
    public class PollServiceTests
    {
        private readonly Mock<IPollRepository> _pollRepo;
        private readonly Mock<IUserRepository> _userRepo;
        private readonly Mock<INotificationRepository> _notificationRepo;
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly PollService _sut;

        public PollServiceTests()
        {
            _pollRepo = new Mock<IPollRepository>();
            _userRepo = new Mock<IUserRepository>();
            _notificationRepo = new Mock<INotificationRepository>();
            _unitOfWork = new Mock<IUnitOfWork>();

            _sut = new PollService(
                _pollRepo.Object,
                _userRepo.Object,
                _notificationRepo.Object,
                _unitOfWork.Object);
        }

        [Fact]
        public async Task CreatePollAsync_ValidDto_SavesAndReturnsPoll()
        {
            var creatorId = Guid.NewGuid();
            var dto = new PollCreateDto
            {
                Question = "Which database?",
                Category = "Technology",
                Tags = new List<string> { "sql", "db" },
                Options = new List<string> { "SQLite", "PostgreSQL" },
                DurationSeconds = 120,
                AllowMultiple = false
            };

            var poll = await _sut.CreatePollAsync(creatorId, dto);

            Assert.Equal(creatorId, poll.CreatorId);
            Assert.Equal("Which database?", poll.Question);
            Assert.Equal(2, poll.Options.Count);
            _pollRepo.Verify(r => r.Add(It.IsAny<Poll>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreatePollAsync_DurationUnder60Seconds_ThrowsArgumentException()
        {
            var dto = new PollCreateDto
            {
                Question = "Too short?",
                Category = "Fun",
                Options = new List<string> { "A", "B" },
                DurationSeconds = 30
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _sut.CreatePollAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task CastVoteAsync_ValidRequest_UpdatesBalanceAndSavesVote()
        {
            var user = new User("alice", "hash", 100);
            var poll = new Poll
            {
                Question = "Vote test?",
                Category = "Tech",
                ClosingTime = DateTime.UtcNow.AddHours(1),
                Options = new List<PollOption>
                {
                    new("Yes") { Position = 0 },
                    new("No") { Position = 1 }
                }
            };

            _pollRepo.Setup(r => r.GetByIdAsync(poll.Id)).ReturnsAsync(poll);
            _userRepo.Setup(r => r.FindByIdAsync(user.Id)).ReturnsAsync(user);

            var (remainingPoints, isEarlyBird) = await _sut.CastVoteAsync(
                poll.Id,
                user.Id,
                new VoteDto { OptionIndices = new List<int> { 0 }, BettedPoints = 20 });

            Assert.Equal(80, remainingPoints);
            Assert.True(isEarlyBird);
            _pollRepo.Verify(r => r.AddVote(It.IsAny<Vote>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeletePollAsync_OwnerRequestsDelete_RemovesPollAndSaves()
        {
            var ownerId = Guid.NewGuid();
            var poll = new Poll { CreatorId = ownerId };
            _pollRepo.Setup(r => r.GetByIdAsync(poll.Id)).ReturnsAsync(poll);

            await _sut.DeletePollAsync(poll.Id, ownerId);

            _pollRepo.Verify(r => r.Remove(poll), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeletePollAsync_DifferentUserRequestsDelete_ThrowsUnauthorizedAccessException()
        {
            var poll = new Poll { CreatorId = Guid.NewGuid() };
            _pollRepo.Setup(r => r.GetByIdAsync(poll.Id)).ReturnsAsync(poll);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sut.DeletePollAsync(poll.Id, Guid.NewGuid()));
        }

        [Fact]
        public async Task AddCommentAsync_ValidPoll_AddsCommentAndSaves()
        {
            var poll = new Poll { Question = "Comment test?" };
            _pollRepo.Setup(r => r.GetByIdAsync(poll.Id)).ReturnsAsync(poll);

            var comment = await _sut.AddCommentAsync(poll.Id, Guid.NewGuid(), "alice", "Great poll!");

            Assert.Equal("alice", comment.AuthorName);
            Assert.Equal("Great poll!", comment.Content);
            Assert.Single(poll.Comments);
            _pollRepo.Verify(r => r.AddComment(It.IsAny<Comment>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ResolveClosedPollsAsync_UnresolvedClosedPoll_AwardsPayoutsAndCreatesNotifications()
        {
            var winner = new User("winner", "hash", 80);
            var loser = new User("loser", "hash", 80);

            var poll = new Poll
            {
                Question = "Closed poll?",
                ClosingTime = DateTime.UtcNow.AddMinutes(-5),
                Options = new List<PollOption>
                {
                    new("Win") { Position = 0 },
                    new("Lose") { Position = 1 }
                }
            };
            poll.Votes.Add(new Vote(winner.Id, new List<int> { 0 }, 20) { IsEarlyBird = false });
            poll.Votes.Add(new Vote(Guid.NewGuid(), new List<int> { 0 }, 0));
            poll.Votes.Add(new Vote(loser.Id, new List<int> { 1 }, 20));

            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { winner, loser });
            _pollRepo.Setup(r => r.GetUnresolvedClosedPolls()).Returns(new List<Poll> { poll });

            await _sut.ResolveClosedPollsAsync();

            Assert.True(poll.IsResolved);
            Assert.Equal(120, winner.PointsBalance);
            Assert.Equal(80, loser.PointsBalance);
            _notificationRepo.Verify(n => n.Add(It.IsAny<Notification>()), Times.Exactly(3));
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EnsureDailyQuestionAsync_NoChallengeToday_CreatesDailyPoll()
        {
            _pollRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Poll>());

            await _sut.EnsureDailyQuestionAsync();

            _pollRepo.Verify(r => r.Add(It.Is<Poll>(p => p.IsDailyChallenge)), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EnsureDailyQuestionAsync_ChallengeAlreadyExistsToday_DoesNothing()
        {
            var existingDaily = new Poll
            {
                IsDailyChallenge = true,
                CreatedAt = DateTime.UtcNow
            };
            _pollRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Poll> { existingDaily });

            await _sut.EnsureDailyQuestionAsync();

            _pollRepo.Verify(r => r.Add(It.IsAny<Poll>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetFeedForUserAsync_ReturnsMappedFeedDtos()
        {
            var user = new User("alice", "hash");
            var poll = new Poll
            {
                Question = "Feed poll?",
                Category = "Sport",
                ClosingTime = DateTime.UtcNow.AddHours(2),
                Options = new List<PollOption>
                {
                    new("Team A") { Position = 0 },
                    new("Team B") { Position = 1 }
                }
            };
            poll.Votes.Add(new Vote(user.Id, new List<int> { 0 }, 15));

            _userRepo.Setup(r => r.FindByUsernameAsync("alice")).ReturnsAsync(user);
            _pollRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Poll> { poll });

            var feed = (await _sut.GetFeedForUserAsync("alice")).ToList();

            Assert.Single(feed);
            Assert.Equal("Feed poll?", feed[0].Question);
            Assert.Single(feed[0].Votes);
            Assert.Equal(1, feed[0].TotalVotesCount);
        }
    }
}