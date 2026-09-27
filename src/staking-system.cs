using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PollSystem.Domain
{
    // Thrown when a user tries to stake more points than they currently hold.
    public class InsufficientPointsException : Exception
    {
        public InsufficientPointsException(string message) : base(message) { }
    }

    // Thrown when a poll can't be resolved yet (still open).
    public class PollResolutionException : Exception
    {
        public PollResolutionException(string message) : base(message) { }
    }

    // A user's running point balance. Points are the currency staked on poll predictions.
    public class UserAccount
    {
        public Guid UserId { get; init; }
        public int Points { get; set; }

        // Needed for JSON (de)serialization
        public UserAccount() { }

        public UserAccount(Guid userId, int startingPoints)
        {
            UserId = userId;
            Points = startingPoints;
        }
    }

    // Tracks and persists every user's point balance.
    public class UserAccountManager
    {
        public const int DefaultStartingPoints = 1000;

        private readonly Dictionary<Guid, UserAccount> _accounts = new();

        // Returns the user's account, creating one with the default starting balance if needed.
        public UserAccount GetOrCreateAccount(Guid userId, int startingPoints = DefaultStartingPoints)
        {
            if (!_accounts.TryGetValue(userId, out var account))
            {
                account = new UserAccount(userId, startingPoints);
                _accounts[userId] = account;
            }

            return account;
        }

        public int GetBalance(Guid userId) => GetOrCreateAccount(userId).Points;

        // Removes points from a user's balance. Used the moment a stake is placed.
        public void Deduct(Guid userId, int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount to deduct must be greater than zero.", nameof(amount));
            }

            var account = GetOrCreateAccount(userId);
            if (account.Points < amount)
            {
                throw new InsufficientPointsException(
                    $"User {userId} has {account.Points} points, which isn't enough to stake {amount}.");
            }

            account.Points -= amount;
        }

        // Adds points to a user's balance. Used to pay out winning stakes, or to issue refunds.
        public void Credit(Guid userId, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var account = GetOrCreateAccount(userId);
            account.Points += amount;
        }

        public async Task SaveStateAsync(string filePath = "user_accounts.json")
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            using FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, _accounts.Values.ToList(), options);
        }

        public async Task LoadStateAsync(string filePath = "user_accounts.json")
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var loaded = await JsonSerializer.DeserializeAsync<List<UserAccount>>(stream);
            if (loaded == null)
            {
                return;
            }

            _accounts.Clear();
            foreach (var account in loaded)
            {
                _accounts[account.UserId] = account;
            }
        }
    }

    // What happened to a single user's stake once a poll has been resolved.
    public record PayoutRecord(Guid UserId, int OptionIndex, int PointsStaked, int PointsAwarded, bool Won);

    // The outcome of resolving a poll: who guessed the majority, and what everyone was paid.
    public record PollResolutionResult(
        Guid PollId,
        bool WasTie,
        int? WinningOptionIndex,
        IReadOnlyList<PayoutRecord> Payouts);

    // Looks a poll up by id without requiring any changes to PollManager itself,
    // since PollManager already exposes its polls via IEnumerable<Poll>.
    public static class PollManagerExtensions
    {
        public static Poll? GetPollById(this PollManager manager, Guid pollId)
        {
            return manager.FirstOrDefault(p => p.Id == pollId);
        }
    }

    // Ties polls and point balances together: lets users stake points on an answer,
    // and pays out winners once the poll closes.
    //
    // Design decisions (documented for reviewers):
    //  - Points are deducted the moment a stake is placed, not at resolution time. Otherwise
    //    a user could stake more points than they actually have across several open polls.
    //  - "Majority" means the option with the most votes cast, matching the project brief:
    //    "Stake points to double your balance upon guessing the majority."
    //  - If two or more options tie for the most votes, nobody can be said to have guessed
    //    the majority, so every stake is refunded in full instead of doubled or lost.
    //  - A poll can only be resolved once. Calling ResolvePoll again just returns the
    //    original result instead of paying anyone out twice.
    public class StakingService
    {
        private readonly PollManager _pollManager;
        private readonly UserAccountManager _accountManager;
        private readonly Dictionary<Guid, PollResolutionResult> _resolutions = new();

        public StakingService(PollManager pollManager, UserAccountManager accountManager)
        {
            _pollManager = pollManager;
            _accountManager = accountManager;
        }

        // Places a stake for a user on one option of an open poll.
        // Deducts the points immediately; refunds them if the vote itself turns out invalid
        // (e.g. a bad option index), so a failed stake never costs the user anything.
        public Vote PlaceStake(Guid pollId, Guid userId, int optionIndex, int points)
        {
            if (points <= 0)
            {
                throw new ArgumentException("Staked points must be greater than zero.", nameof(points));
            }

            var poll = _pollManager.GetPollById(pollId)
                ?? throw new ArgumentException($"No poll found with id {pollId}.", nameof(pollId));

            if (poll.IsClosed())
            {
                throw new InvalidOperationException("This poll is no longer accepting stakes.");
            }

            _accountManager.Deduct(userId, points);

            var vote = new Vote(userId, optionIndex, points);
            try
            {
                poll.CastVote(vote);
            }
            catch
            {
                // The vote didn't go through (e.g. invalid option) — give the points back.
                _accountManager.Credit(userId, points);
                throw;
            }

            return vote;
        }

        // True once ResolvePoll has been called for this poll.
        public bool IsResolved(Guid pollId) => _resolutions.ContainsKey(pollId);

        // Resolves a closed poll: works out the majority option and pays out winners
        // double their stake. Safe to call more than once — later calls just return the
        // original result instead of paying out again.
        public PollResolutionResult ResolvePoll(Guid pollId)
        {
            if (_resolutions.TryGetValue(pollId, out var existing))
            {
                return existing;
            }

            var poll = _pollManager.GetPollById(pollId)
                ?? throw new ArgumentException($"No poll found with id {pollId}.", nameof(pollId));

            if (!poll.IsClosed())
            {
                throw new PollResolutionException("This poll is still open and can't be resolved yet.");
            }

            var votes = poll.Votes.ToList();

            if (votes.Count == 0)
            {
                var emptyResult = new PollResolutionResult(pollId, WasTie: false, WinningOptionIndex: null, Payouts: Array.Empty<PayoutRecord>());
                _resolutions[pollId] = emptyResult;
                return emptyResult;
            }

            var counts = votes
                .GroupBy(v => v.OptionIndex)
                .Select(g => new { OptionIndex = g.Key, Count = g.Count() })
                .ToList();

            var topCount = counts.Max(c => c.Count);
            var topOptions = counts.Where(c => c.Count == topCount).ToList();
            var wasTie = topOptions.Count > 1;

            var payouts = new List<PayoutRecord>();

            if (wasTie)
            {
                // No clear majority: refund every stake, nobody wins or loses points.
                foreach (var vote in votes)
                {
                    _accountManager.Credit(vote.UserId, vote.BettedPoints);
                    payouts.Add(new PayoutRecord(vote.UserId, vote.OptionIndex, vote.BettedPoints, vote.BettedPoints, Won: false));
                }

                var tieResult = new PollResolutionResult(pollId, WasTie: true, WinningOptionIndex: null, Payouts: payouts);
                _resolutions[pollId] = tieResult;
                return tieResult;
            }

            var winningOptionIndex = topOptions[0].OptionIndex;

            foreach (var vote in votes)
            {
                var won = vote.OptionIndex == winningOptionIndex;
                var awarded = won ? vote.BettedPoints * 2 : 0;

                if (won)
                {
                    _accountManager.Credit(vote.UserId, awarded);
                }

                payouts.Add(new PayoutRecord(vote.UserId, vote.OptionIndex, vote.BettedPoints, awarded, won));
            }

            var result = new PollResolutionResult(pollId, WasTie: false, winningOptionIndex, payouts);
            _resolutions[pollId] = result;
            return result;
        }
    }
}
