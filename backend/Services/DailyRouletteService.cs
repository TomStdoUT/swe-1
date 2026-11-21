using System.Collections.Concurrent;
using System.Security.Cryptography;
using PollSystem.Models.DTOs;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;
using PollSystem.Services.Interfaces;

namespace PollSystem.Services
{
    public sealed class DailyRouletteService : IDailyRouletteService
    {
        private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> UserLocks = new();

        private readonly IDailyRouletteRepository _rouletteRepo;
        private readonly IUserRepository _userRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReadOnlyList<int> _wheelSegments;
        private readonly int _cooldownHours;

        public DailyRouletteService(
            IDailyRouletteRepository rouletteRepo,
            IUserRepository userRepo,
            IUnitOfWork unitOfWork,
            IConfiguration configuration)
        {
            _rouletteRepo = rouletteRepo;
            _userRepo = userRepo;
            _unitOfWork = unitOfWork;

            var minPoints = configuration.GetValue<int?>("DailyRoulette:MinPoints") ?? 10;
            var maxPoints = configuration.GetValue<int?>("DailyRoulette:MaxPoints") ?? 100;
            var segmentCount = configuration.GetValue<int?>("DailyRoulette:SegmentCount") ?? 8;
            _cooldownHours = configuration.GetValue<int?>("DailyRoulette:CooldownHours") ?? 24;

            if (minPoints <= 0 || maxPoints < minPoints || segmentCount < 2 || _cooldownHours <= 0)
                throw new InvalidOperationException("Invalid DailyRoulette configuration.");

            _wheelSegments = BuildSegments(minPoints, maxPoints, segmentCount);
        }

        public async Task<DailyRouletteStatusDto> GetStatusAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.FindByIdAsync(userId);
            if (user is null)
                throw new UnauthorizedAccessException("User not found.");

            var latestSpin = await _rouletteRepo.GetLatestByUserIdAsync(userId, cancellationToken);
            var nowUtc = DateTime.UtcNow;

            if (latestSpin is null)
            {
                return new DailyRouletteStatusDto(
                    CanSpin: true,
                    NextSpinAvailableAtUtc: null,
                    LastAwardedPoints: null,
                    WheelSegments: _wheelSegments);
            }

            var nextAvailable = latestSpin.SpunAtUtc.AddHours(_cooldownHours);
            var canSpin = nowUtc >= nextAvailable;

            return new DailyRouletteStatusDto(
                CanSpin: canSpin,
                NextSpinAvailableAtUtc: canSpin ? null : nextAvailable,
                LastAwardedPoints: latestSpin.AwardedPoints,
                WheelSegments: _wheelSegments);
        }

        public async Task<DailyRouletteSpinResultDto> SpinAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var userLock = UserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
            await userLock.WaitAsync(cancellationToken);

            try
            {
                var user = await _userRepo.FindByIdAsync(userId);
                if (user is null)
                    throw new UnauthorizedAccessException("User not found.");

                var nowUtc = DateTime.UtcNow;
                var latestSpin = await _rouletteRepo.GetLatestByUserIdAsync(userId, cancellationToken);

                if (latestSpin is not null)
                {
                    var nextAvailable = latestSpin.SpunAtUtc.AddHours(_cooldownHours);
                    if (nowUtc < nextAvailable)
                    {
                        var unit = _cooldownHours == 1 ? "hour" : "hours";
                        throw new InvalidOperationException($"Daily roulette can only be spun once every {_cooldownHours} {unit}.");
                    }
                }

                var winningIndex = RandomNumberGenerator.GetInt32(_wheelSegments.Count);
                var awardedPoints = _wheelSegments[winningIndex];

                user.PointsBalance += awardedPoints;

                var spin = new DailyRouletteSpin(user.Id, awardedPoints, nowUtc);
                _rouletteRepo.Add(spin);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new DailyRouletteSpinResultDto(
                    AwardedPoints: awardedPoints,
                    WinningSegmentIndex: winningIndex,
                    NewPointsBalance: user.PointsBalance,
                    NextSpinAvailableAtUtc: nowUtc.AddHours(_cooldownHours));
            }
            finally
            {
                userLock.Release();
            }
        }

        private static IReadOnlyList<int> BuildSegments(int minPoints, int maxPoints, int segmentCount)
        {
            var segments = new int[segmentCount];
            for (var i = 0; i < segmentCount; i++)
            {
                var t = segmentCount == 1 ? 0.0 : (double)i / (segmentCount - 1);
                var value = minPoints + (maxPoints - minPoints) * t;
                var rounded = (int)Math.Round(value / 5.0) * 5;
                segments[i] = Math.Max(minPoints, rounded);
            }

            segments[0] = minPoints;
            segments[^1] = maxPoints;
            return segments;
        }
    }
}