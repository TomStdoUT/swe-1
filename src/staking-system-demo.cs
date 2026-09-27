using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PollSystem.Domain
{
    // Walks through the full staking lifecycle: create a poll, have three users stake points
    // on different options, close the poll, and resolve it to see who doubled their points.
    //
    // This is deliberately NOT a `Main` method, since poll-system.cs already defines one for
    // the project's console entry point. Call StakingSystemDemo.Run() from that existing
    // Main (or from a throwaway test project) to see it in action:
    //
    //     await StakingSystemDemo.Run();
    //
    public static class StakingSystemDemo
    {
        public static async Task Run()
        {
            Console.WriteLine("=== Testing Issue #6: Stake Points on an Answer ===");

            var pollManager = new PollManager();
            var accountManager = new UserAccountManager();
            var stakingService = new StakingService(pollManager, accountManager);

            var creatorId = Guid.NewGuid();
            var alice = Guid.NewGuid();
            var bob = Guid.NewGuid();
            var carol = Guid.NewGuid();

            // 1. Create a poll that closes almost immediately so we can resolve it below.
            var poll = pollManager.CreatePoll(
                creatorId: creatorId,
                question: "Will it rain tomorrow?",
                options: new List<PollOption> { new("Yes"), new("No") },
                closingTime: DateTime.UtcNow.AddSeconds(1)
            );
            Console.WriteLine($"[Created] Poll: \"{poll.Question}\" (Id: {poll.Id})");

            // 2. Give everyone a starting balance and show it.
            Console.WriteLine($"[Balances] Alice: {accountManager.GetBalance(alice)}, Bob: {accountManager.GetBalance(bob)}, Carol: {accountManager.GetBalance(carol)}");

            // 3. Three users stake points. Alice and Bob both bet "Yes" (option 0), Carol bets "No" (option 1).
            stakingService.PlaceStake(poll.Id, alice, optionIndex: 0, points: 200);
            stakingService.PlaceStake(poll.Id, bob, optionIndex: 0, points: 100);
            stakingService.PlaceStake(poll.Id, carol, optionIndex: 1, points: 300);
            Console.WriteLine("[Success] All stakes placed.");
            Console.WriteLine($"[Balances after staking] Alice: {accountManager.GetBalance(alice)}, Bob: {accountManager.GetBalance(bob)}, Carol: {accountManager.GetBalance(carol)}");

            // 4. Wait for the poll to actually close, then try staking again to prove it's rejected.
            await Task.Delay(1200);
            try
            {
                stakingService.PlaceStake(poll.Id, carol, optionIndex: 0, points: 50);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[Expected failure] Late stake rejected: {ex.Message}");
            }

            // 5. Resolve the poll. "Yes" has 2 votes vs. "No"'s 1 vote, so "Yes" is the majority.
            var result = stakingService.ResolvePoll(poll.Id);
            Console.WriteLine($"[Resolved] Winning option index: {result.WinningOptionIndex} (tie: {result.WasTie})");
            foreach (var payout in result.Payouts)
            {
                Console.WriteLine($"  - User {payout.UserId}: staked {payout.PointsStaked}, awarded {payout.PointsAwarded}, won: {payout.Won}");
            }

            Console.WriteLine($"[Balances after resolution] Alice: {accountManager.GetBalance(alice)}, Bob: {accountManager.GetBalance(bob)}, Carol: {accountManager.GetBalance(carol)}");

            // 6. Resolving again should be a no-op that returns the same result, not a second payout.
            var secondCall = stakingService.ResolvePoll(poll.Id);
            Console.WriteLine($"[Idempotency check] Second ResolvePoll call returned the same result: {ReferenceEquals(result, secondCall)}");
            Console.WriteLine($"[Final balances] Alice: {accountManager.GetBalance(alice)}, Bob: {accountManager.GetBalance(bob)}, Carol: {accountManager.GetBalance(carol)}");
        }
    }
}
