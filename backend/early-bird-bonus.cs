using System;

namespace PollSystem.Domain
{
    // Rewards users with bonus points for staking early in a poll's lifetime, per the
    // project brief: "Earn early-bird bonus points for early participation."
    //
    // Design decisions (documented for reviewers):
    //  - "Early" means the stake landed within the first EarlyWindowFraction of the poll's
    //    total open window (CreatedAt -> ClosingTime), not a fixed clock time — that way
    //    it behaves the same for a 10-minute poll and a 10-day poll.
    //  - The bonus is a flat percentage of the staked points, with a minimum floor so even
    //    a small stake earns something worthwhile. It's credited immediately and does not
    //    depend on whether the stake ends up winning — it's a reward for showing up early,
    //    not for predicting correctly.
    //  - This depends on UserAccountManager from issue #6 (staking-system.cs) but doesn't
    //    modify StakingService or Poll at all. Call TryAwardBonus right after
    //    StakingService.PlaceStake succeeds.
    public class EarlyBirdBonusService
    {
        public const double EarlyWindowFraction = 0.25; // first 25% of the poll's open window
        public const double BonusRate = 0.10;            // 10% of the staked points
        public const int MinimumBonus = 5;

        private readonly UserAccountManager _accountManager;

        public EarlyBirdBonusService(UserAccountManager accountManager)
        {
            _accountManager = accountManager;
        }

        // Awards the bonus if the stake qualifies as early-bird. Returns false (and awards
        // nothing) if it was staked too late or for zero/negative points.
        public bool TryAwardBonus(Poll poll, Guid userId, int stakedPoints, out int bonusAwarded)
        {
            bonusAwarded = 0;

            if (stakedPoints <= 0 || !IsEarly(poll, DateTime.UtcNow))
            {
                return false;
            }

            bonusAwarded = Math.Max(MinimumBonus, (int)Math.Round(stakedPoints * BonusRate));
            _accountManager.Credit(userId, bonusAwarded);
            return true;
        }

        // Whether `stakeTime` falls within the first EarlyWindowFraction of the poll's
        // total open window (CreatedAt to ClosingTime).
        public static bool IsEarly(Poll poll, DateTime stakeTime)
        {
            var totalWindow = poll.ClosingTime - poll.CreatedAt;
            if (totalWindow <= TimeSpan.Zero)
            {
                return false;
            }

            var earlyWindowEnd = poll.CreatedAt + TimeSpan.FromTicks((long)(totalWindow.Ticks * EarlyWindowFraction));
            return stakeTime <= earlyWindowEnd;
        }
    }
}
