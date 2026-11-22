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
        public async Task CreatePollAsync_DurationUnder60Seconds_ThrowsArgumentException()
        {
            var dto = new PollCreateDto
            {
                Question = "Too short?",
                Category = "Fun",
                Tags = new List<string> { "quick" },
                Options = new List<string> { "Yes", "No" },
                DurationSeconds = 59,
                AllowMultiple = false
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _sut.CreatePollAsync(Guid.NewGuid(), dto));

            _pollRepo.Verify(r => r.Add(It.IsAny<Poll>()), Times.Never);
        }

        [Fact]
        public async Task CreatePollAsync_ValidDto_AddsPollAndSavesChanges()
        {
            var creatorId = Guid.NewGuid();
            var dto = new PollCreateDto
            {
                Question = "Valid poll?",
                Category = "Technology",
                Tags = new List<string> { "dotnet" },
                Options = new List<string> { "Yes", "No" },
                DurationSeconds = 120,
                AllowMultiple = true
            };

            var poll = await _sut.CreatePollAsync(creatorId, dto);

            Assert.Equal(creatorId, poll.CreatorId);
            Assert.Equal("Valid poll?", poll.Question);
            Assert.Equal(2, poll.Options.Count);
            Assert.True(poll.AllowMultiple);
            _pollRepo.Verify(r => r.Add(poll), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CastVoteAsync_ValidVote_DeductsPointsSavesAndReturnsResult()
        {
            var user = new User("alice", "hash", 100);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddHours(1),
                Options = new List<PollOption>
                {
                    new("Option A") { Position = 0 },
                    new("Option B") { Position = 1 }
                }
            };

            _pollRepo.Setup(r => r.GetByIdAsync(poll.Id)).ReturnsAsync(poll);
            _userRepo.Setup(r => r.FindByIdAsync(user.Id)).ReturnsAsync(user);

            var (remainingPoints, isEarlyBird) = await _sut.CastVoteAsync(
                poll.Id,
                user.Id,
                new VoteDto { OptionIndices = new List<int> { 0 }, BettedPoints = 25 });

            Assert.Equal(75, remainingPoints);
            Assert.True(isEarlyBird);
            _pollRepo.Verify(r => r.AddVote(It.IsAny<Vote>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BoostPollAsync_ValidRequest_DeductsPointsAndReturnsSuccess()
        {
            var user = new User("alice", "hash", 100);
            var poll = new Poll
            {
                Question = "Boost me",
                ClosingTime = DateTime.UtcNow.AddHours(1)
            };

            _pollRepo.Setup(r => r.GetByIdAsync(poll.Id)).ReturnsAsync(poll);
            _userRepo.Setup(r => r.FindByIdAsync(user.Id)).ReturnsAsync(user);

            var result = await _sut.BoostPollAsync(poll.Id, user.Id);

            Assert.True(result.IsSuccess);
            Assert.True(poll.IsBoosted);
            Assert.Equal(70, user.PointsBalance);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BoostPollAsync_MissingPollOrUser_ReturnsFailure()
        {
            _pollRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Poll?)null);
            var missingPollResult = await _sut.BoostPollAsync(Guid.NewGuid(), Guid.NewGuid());
            Assert.False(missingPollResult.IsSuccess);

            var poll = new Poll { ClosingTime = DateTime.UtcNow.AddHours(1) };
            _pollRepo.Setup(r => r.GetByIdAsync(poll.Id)).ReturnsAsync(poll);
            _userRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            var missingUserResult = await _sut.BoostPollAsync(poll.Id, Guid.NewGuid());
            Assert.False(missingUserResult.IsSuccess);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetFeedForUserAsync_ActiveBoostedPoll_AppearsBeforeNewerUnboostedPoll()
        {
            var user = new User("alice", "hash");
            var boostedEarlierClosing = new Poll
            {
                Question = "Boosted Poll",
                Category = "Tech",
                IsBoosted = true,
                ClosingTime = DateTime.UtcNow.AddHours(1),
                Options = new List<PollOption>
                {
                    new("A") { Position = 0 },
                    new("B") { Position = 1 }
                }
            };
            var regularLaterClosing = new Poll
            {
                Question = "Regular Poll",
                Category = "Tech",
                IsBoosted = false,
                ClosingTime = DateTime.UtcNow.AddHours(5),
                Options = new List<PollOption>
                {
                    new("A") { Position = 0 },
                    new("B") { Position = 1 }
                }
            };

            _userRepo.Setup(r => r.FindByUsernameAsync("alice")).ReturnsAsync(user);
            _pollRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Poll> { regularLaterClosing, boostedEarlierClosing });

            var feed = (await _sut.GetFeedForUserAsync("alice")).ToList();

            Assert.Equal(2, feed.Count);
            Assert.Equal("Boosted Poll", feed[0].Question);
            Assert.True(feed[0].IsBoosted);
            Assert.Equal("Regular Poll", feed[1].Question);
            Assert.False(feed[1].IsBoosted);
        }

        [Fact]
        public async Task DeletePollAsync_ValidatesExistenceAndOwnership()
        {
            var ownerId = Guid.NewGuid();
            var poll = new Poll { CreatorId = ownerId };

            _pollRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Poll?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.DeletePollAsync(Guid.NewGuid(), ownerId));

            _pollRepo.Setup(r => r.GetByIdAsync(poll.Id)).ReturnsAsync(poll);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sut.DeletePollAsync(poll.Id, Guid.NewGuid()));

            await _sut.DeletePollAsync(poll.Id, ownerId);
            _pollRepo.Verify(r => r.Remove(poll), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ResolveClosedPollsAsync_AwardsWinnersAndGeneratesNotifications()
        {
            var winner = new User("winner", "hash", 100);
            var loser = new User("loser", "hash", 100);
            var poll = new Poll
            {
                Question = "Resolved question",
                ClosingTime = DateTime.UtcNow.AddMinutes(-5),
                Options = new List<PollOption>
                {
                    new("Win") { Position = 0 },
                    new("Lose") { Position = 1 }
                }
            };
            poll.Votes.Add(new Vote(winner.Id, new List<int> { 0 }, 20));
            poll.Votes.Add(new Vote(Guid.NewGuid(), new List<int> { 0 }, 0));
            poll.Votes.Add(new Vote(loser.Id, new List<int> { 1 }, 15));

            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { winner, loser });
            _pollRepo.Setup(r => r.GetUnresolvedClosedPollsAsync()).ReturnsAsync(new List<Poll> { poll });

            await _sut.ResolveClosedPollsAsync();

            Assert.True(poll.IsResolved);
            Assert.Equal(140, winner.PointsBalance);
            Assert.Equal(100, loser.PointsBalance);
            _notificationRepo.Verify(n => n.Add(It.Is<Notification>(notif => notif.TargetUserId == winner.Id)), Times.Once);
            _notificationRepo.Verify(n => n.Add(It.Is<Notification>(notif => notif.TargetUserId == loser.Id)), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EnsureDailyQuestionAsync_CreatesOnlyWhenNoneExistsToday()
        {
            _pollRepo.Setup(r => r.ProjectWhereAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Poll, bool>>>(),
                    It.IsAny<System.Linq.Expressions.Expression<Func<Poll, DateTime>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DateTime>());

            await _sut.EnsureDailyQuestionAsync();

            _pollRepo.Verify(r => r.Add(It.Is<Poll>(p => p.IsDailyChallenge)), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            var existingDaily = new Poll
            {
                IsDailyChallenge = true,
                CreatedAt = DateTime.UtcNow
            };
            _pollRepo.Setup(r => r.ProjectWhereAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Poll, bool>>>(),
                    It.IsAny<System.Linq.Expressions.Expression<Func<Poll, DateTime>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DateTime> { existingDaily.CreatedAt });

            await _sut.EnsureDailyQuestionAsync();

            _pollRepo.Verify(r => r.Add(It.IsAny<Poll>()), Times.Once);
        }
    }
}
