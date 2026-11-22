using Microsoft.Extensions.Configuration;
using Moq;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;
using PollSystem.Services;

namespace PollSystem.Tests.Unit
{
    public class DailyRouletteServiceTests
    {
        private readonly Mock<IDailyRouletteRepository> _rouletteRepo;
        private readonly Mock<IUserRepository> _userRepo;
        private readonly Mock<IUnitOfWork> _unitOfWork;

        public DailyRouletteServiceTests()
        {
            _rouletteRepo = new Mock<IDailyRouletteRepository>();
            _userRepo = new Mock<IUserRepository>();
            _unitOfWork = new Mock<IUnitOfWork>();
        }

        private DailyRouletteService CreateSut(Dictionary<string, string?>? settings = null)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings ?? new Dictionary<string, string?>())
                .Build();

            return new DailyRouletteService(
                _rouletteRepo.Object,
                _userRepo.Object,
                _unitOfWork.Object,
                config);
        }

        [Theory]
        [InlineData("0", "100", "8", "24")]
        [InlineData("50", "10", "8", "24")]
        [InlineData("10", "100", "1", "24")]
        [InlineData("10", "100", "8", "0")]
        public void Constructor_InvalidConfiguration_ThrowsInvalidOperationException(
            string minPoints,
            string maxPoints,
            string segmentCount,
            string cooldownHours)
        {
            var settings = new Dictionary<string, string?>
            {
                ["DailyRoulette:MinPoints"] = minPoints,
                ["DailyRoulette:MaxPoints"] = maxPoints,
                ["DailyRoulette:SegmentCount"] = segmentCount,
                ["DailyRoulette:CooldownHours"] = cooldownHours
            };

            Assert.Throws<InvalidOperationException>(() => CreateSut(settings));
        }

        [Fact]
        public async Task GetStatusAsync_SmallMinPoints_ClampsSegmentsToMinimum()
        {
            var user = new User("alice", "hash", 100);
            _userRepo.Setup(r => r.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _rouletteRepo.Setup(r => r.GetLatestByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((DailyRouletteSpin?)null);

            var sut = CreateSut(new Dictionary<string, string?>
            {
                ["DailyRoulette:MinPoints"] = "2",
                ["DailyRoulette:MaxPoints"] = "20",
                ["DailyRoulette:SegmentCount"] = "8"
            });

            var status = await sut.GetStatusAsync(user.Id);

            Assert.All(status.WheelSegments, segment => Assert.True(segment >= 2));
            Assert.Equal(2, status.WheelSegments.First());
            Assert.Equal(20, status.WheelSegments.Last());
        }

        [Fact]
        public async Task GetStatusAsync_UserNotFound_ThrowsUnauthorizedAccessException()
        {
            _userRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
            var sut = CreateSut();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                sut.GetStatusAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetStatusAsync_NoPreviousSpin_ReturnsCanSpinTrue()
        {
            var user = new User("alice", "hash", 100);
            _userRepo.Setup(r => r.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _rouletteRepo.Setup(r => r.GetLatestByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((DailyRouletteSpin?)null);

            var sut = CreateSut();
            var status = await sut.GetStatusAsync(user.Id);

            Assert.True(status.CanSpin);
            Assert.Null(status.NextSpinAvailableAtUtc);
            Assert.Null(status.LastAwardedPoints);
            Assert.Equal(8, status.WheelSegments.Count);
        }

        [Fact]
        public async Task GetStatusAsync_ActiveCooldown_ReturnsCanSpinFalseWithNextAvailableTime()
        {
            var user = new User("alice", "hash", 100);
            var spunAt = DateTime.UtcNow.AddHours(-6);
            var latestSpin = new DailyRouletteSpin(user.Id, 40, spunAt);

            _userRepo.Setup(r => r.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _rouletteRepo.Setup(r => r.GetLatestByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(latestSpin);

            var sut = CreateSut();
            var status = await sut.GetStatusAsync(user.Id);

            Assert.False(status.CanSpin);
            Assert.Equal(spunAt.AddHours(24), status.NextSpinAvailableAtUtc);
            Assert.Equal(40, status.LastAwardedPoints);
        }

        [Fact]
        public async Task GetStatusAsync_CooldownExpired_ReturnsCanSpinTrueWithLastAwardedPoints()
        {
            var user = new User("alice", "hash", 100);
            var latestSpin = new DailyRouletteSpin(user.Id, 55, DateTime.UtcNow.AddHours(-25));

            _userRepo.Setup(r => r.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _rouletteRepo.Setup(r => r.GetLatestByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(latestSpin);

            var sut = CreateSut();
            var status = await sut.GetStatusAsync(user.Id);

            Assert.True(status.CanSpin);
            Assert.Null(status.NextSpinAvailableAtUtc);
            Assert.Equal(55, status.LastAwardedPoints);
        }

        [Fact]
        public async Task SpinAsync_FirstSpin_AwardsPointsSavesSpinAndReturnsResult()
        {
            var user = new User("alice", "hash", 100);
            _userRepo.Setup(r => r.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _rouletteRepo.Setup(r => r.GetLatestByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((DailyRouletteSpin?)null);

            var sut = CreateSut();
            var status = await sut.GetStatusAsync(user.Id);
            var result = await sut.SpinAsync(user.Id);

            Assert.InRange(result.WinningSegmentIndex, 0, status.WheelSegments.Count - 1);
            Assert.Equal(status.WheelSegments[result.WinningSegmentIndex], result.AwardedPoints);
            Assert.Equal(100 + result.AwardedPoints, user.PointsBalance);
            Assert.Equal(user.PointsBalance, result.NewPointsBalance);
            _rouletteRepo.Verify(r => r.Add(It.Is<DailyRouletteSpin>(s =>
                s.UserId == user.Id && s.AwardedPoints == result.AwardedPoints)), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SpinAsync_CooldownActive_ThrowsInvalidOperationException()
        {
            var user = new User("alice", "hash", 100);
            var recentSpin = new DailyRouletteSpin(user.Id, 25, DateTime.UtcNow.AddHours(-2));

            _userRepo.Setup(r => r.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _rouletteRepo.Setup(r => r.GetLatestByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(recentSpin);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SpinAsync(user.Id));
            _rouletteRepo.Verify(r => r.Add(It.IsAny<DailyRouletteSpin>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SpinAsync_CooldownExpired_AllowsSecondSpin()
        {
            var user = new User("alice", "hash", 125);
            var expiredSpin = new DailyRouletteSpin(user.Id, 25, DateTime.UtcNow.AddHours(-24).AddMinutes(-1));

            _userRepo.Setup(r => r.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _rouletteRepo.Setup(r => r.GetLatestByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expiredSpin);

            var sut = CreateSut();
            var result = await sut.SpinAsync(user.Id);

            Assert.Equal(125 + result.AwardedPoints, user.PointsBalance);
            _rouletteRepo.Verify(r => r.Add(It.IsAny<DailyRouletteSpin>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SpinAsync_UserNotFound_ThrowsUnauthorizedAccessException()
        {
            _userRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
            var sut = CreateSut();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                sut.SpinAsync(Guid.NewGuid()));
        }
    }
}