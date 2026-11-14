using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;
using PollSystem.Services;
using Xunit;

namespace PollSystem.Tests.Unit
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepo;
        private readonly Mock<IPollRepository> _pollRepo;
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _userRepo = new Mock<IUserRepository>();
            _pollRepo = new Mock<IPollRepository>();
            _unitOfWork = new Mock<IUnitOfWork>();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "unit_test_jwt_secret_key_with_sufficient_length_12345!"
                })
                .Build();

            _sut = new UserService(_userRepo.Object, _pollRepo.Object, config, _unitOfWork.Object);
        }

        [Fact]
        public async Task RegisterAsync_ValidNewUser_CreatesUserAndReturnsToken()
        {
            _userRepo.Setup(r => r.FindByUsernameAsync("tomas")).ReturnsAsync((User?)null);

            var (user, token) = await _sut.RegisterAsync("tomas", "Secret123");

            Assert.Equal("tomas", user.Username);
            Assert.NotEqual("Secret123", user.PasswordHash);
            Assert.False(string.IsNullOrWhiteSpace(token));
            _userRepo.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ExistingUsername_ThrowsInvalidOperationException()
        {
            _userRepo.Setup(r => r.FindByUsernameAsync("tomas"))
                .ReturnsAsync(new User("tomas", "hash"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.RegisterAsync("tomas", "Secret123"));
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsUserAndToken()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("Secret123");
            var existingUser = new User("tomas", hash);
            _userRepo.Setup(r => r.FindByUsernameAsync("tomas")).ReturnsAsync(existingUser);

            var (user, token) = await _sut.LoginAsync("tomas", "Secret123");

            Assert.Equal(existingUser.Id, user.Id);
            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("Secret123");
            _userRepo.Setup(r => r.FindByUsernameAsync("tomas"))
                .ReturnsAsync(new User("tomas", hash));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sut.LoginAsync("tomas", "WrongPassword"));
        }

        [Fact]
        public async Task GetUserProfileAsync_UserExists_CalculatesAccuracyAndFollowerCounts()
        {
            var targetUser = new User("alice", "hash", 120);
            var followerUser = new User("bob", "hash", 100);
            followerUser.Following.Add(new UserFollow
            {
                FollowerId = followerUser.Id,
                FollowedId = targetUser.Id
            });

            var closedPoll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddMinutes(-10),
                Options = new List<PollOption>
                {
                    new("A") { Position = 0 },
                    new("B") { Position = 1 }
                }
            };
            closedPoll.Votes.Add(new Vote(targetUser.Id, new List<int> { 0 }, 10));
            closedPoll.Votes.Add(new Vote(followerUser.Id, new List<int> { 0 }, 10));

            _userRepo.Setup(r => r.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { targetUser, followerUser });
            _pollRepo.Setup(r => r.GetClosedPollsAsync()).ReturnsAsync(new List<Poll> { closedPoll });

            var profile = await _sut.GetUserProfileAsync(targetUser.Id);

            Assert.NotNull(profile);
            Assert.Equal("alice", profile.Username);
            Assert.Equal(120, profile.PointsBalance);
            Assert.Equal(1, profile.CorrectPredictions);
            Assert.Equal(1, profile.TotalResolvedVotes);
            Assert.Equal(100.0, profile.AccuracyRate);
            Assert.Equal(1, profile.FollowerCount);
        }

        [Fact]
        public async Task GetUserProfileAsync_UserNotFound_ReturnsNull()
        {
            _userRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            var profile = await _sut.GetUserProfileAsync(Guid.NewGuid());

            Assert.Null(profile);
        }

        [Fact]
        public async Task FollowUserAsync_ValidUsers_AddsFollowLinkAndSaves()
        {
            var follower = new User("alice", "hash");
            var followed = new User("bob", "hash");

            _userRepo.Setup(r => r.FindByIdAsync(follower.Id)).ReturnsAsync(follower);
            _userRepo.Setup(r => r.FindByIdAsync(followed.Id)).ReturnsAsync(followed);

            await _sut.FollowUserAsync(follower.Id, followed.Id);

            Assert.Single(follower.Following);
            Assert.Equal(followed.Id, follower.Following[0].FollowedId);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FollowUserAsync_SelfFollow_ThrowsInvalidOperationException()
        {
            var user = new User("alice", "hash");
            _userRepo.Setup(r => r.FindByIdAsync(user.Id)).ReturnsAsync(user);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.FollowUserAsync(user.Id, user.Id));
        }

        [Fact]
        public async Task UnfollowUserAsync_ExistingFollow_RemovesLinkAndSaves()
        {
            var follower = new User("alice", "hash");
            var followed = new User("bob", "hash");
            follower.Following.Add(new UserFollow { FollowerId = follower.Id, FollowedId = followed.Id });

            _userRepo.Setup(r => r.FindByIdAsync(follower.Id)).ReturnsAsync(follower);
            _userRepo.Setup(r => r.FindByIdAsync(followed.Id)).ReturnsAsync(followed);

            await _sut.UnfollowUserAsync(follower.Id, followed.Id);

            Assert.Empty(follower.Following);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetFollowingVotesAsync_ResolvedPoll_ReturnsFollowedUsersVotes()
        {
            var user = new User("alice", "hash");
            var followed = new User("bob", "hash");
            user.Following.Add(new UserFollow { FollowerId = user.Id, FollowedId = followed.Id });

            var poll = new Poll
            {
                Question = "Test Question",
                Category = "Tech",
                IsResolved = true,
                ClosingTime = DateTime.UtcNow.AddMinutes(-5),
                Options = new List<PollOption>
                {
                    new("First") { Position = 0 },
                    new("Second") { Position = 1 }
                }
            };
            poll.Votes.Add(new Vote(followed.Id, new List<int> { 0 }, 10));

            _userRepo.Setup(r => r.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user, followed });
            _pollRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Poll> { poll });

            var votes = (await _sut.GetFollowingVotesAsync(user.Id)).ToList();

            Assert.Single(votes);
            Assert.Equal("bob", votes[0].Username);
            Assert.Contains("First", votes[0].Selections);
            Assert.True(votes[0].WasCorrect);
        }

        [Fact]
        public async Task BuyCosmeticAsync_ValidPurchase_DeductsPointsAndReturnsSuccess()
        {
            var user = new User("alice", "hash", 50);
            _userRepo.Setup(r => r.FindByIdAsync(user.Id)).ReturnsAsync(user);

            var result = await _sut.BuyCosmeticAsync(user.Id, "gold_name");

            Assert.True(result.IsSuccess);
            Assert.Equal(30, result.Data.RemainingPoints);
            Assert.Contains("gold_name", result.Data.Cosmetics);
            _userRepo.Verify(r => r.AddCosmetic(It.IsAny<UserCosmetic>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BuyCosmeticAsync_FailureCases_ReturnFailureResults()
        {
            _userRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
            var notFound = await _sut.BuyCosmeticAsync(Guid.NewGuid(), "gold_name");
            Assert.False(notFound.IsSuccess);

            var poorUser = new User("poor", "hash", 5);
            poorUser.Cosmetics.Add(new UserCosmetic { UserId = poorUser.Id, Name = "vip_badge" });
            _userRepo.Setup(r => r.FindByIdAsync(poorUser.Id)).ReturnsAsync(poorUser);

            var unknownItem = await _sut.BuyCosmeticAsync(poorUser.Id, "non_existent");
            var alreadyOwned = await _sut.BuyCosmeticAsync(poorUser.Id, "vip_badge");
            var insufficientFunds = await _sut.BuyCosmeticAsync(poorUser.Id, "gold_name");

            Assert.False(unknownItem.IsSuccess);
            Assert.False(alreadyOwned.IsSuccess);
            Assert.False(insufficientFunds.IsSuccess);
        }
    }
}