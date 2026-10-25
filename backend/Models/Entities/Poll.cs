namespace PollSystem.Models.Entities
{
    public class Poll : IEquatable<Poll>, IComparable<Poll>
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid CreatorId { get; init; }
        public string Question { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<PollTag> Tags { get; set; } = new();
        public List<PollOption> Options { get; set; } = new();
        public DateTime ClosingTime { get; set; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public bool AllowMultiple { get; set; }
        public bool IsDailyChallenge { get; set; }
        public bool NotificationsGenerated { get; set; } = false;
        public bool IsResolved { get; set; }

        public List<Comment> Comments { get; set; } = new();

        public List<Vote> Votes { get; set; } = new();

        public bool IsClosed() => DateTime.UtcNow >= ClosingTime;

        public void CastVote(User user, Vote vote)
        {
            if (IsClosed())
                throw new InvalidOperationException("Voting for this poll has ended.");

            if (!vote.OptionIndices.Any())
                throw new ArgumentException("You must select at least one option.");

            if (!AllowMultiple && vote.OptionIndices.Count > 1)
                throw new InvalidOperationException("This poll only allows a single choice.");

            foreach (var index in vote.OptionIndices)
            {
                if (index < 0 || index >= Options.Count)
                    throw new ArgumentOutOfRangeException(nameof(vote.OptionIndices), "Option doesn't exist.");
            }

            // Deduct the points immediately, but do NOT award winnings yet
            if (vote.BettedPoints > 0)
            {
                if (user.PointsBalance < vote.BettedPoints)
                    throw new InvalidOperationException("Insufficient points balance.");

                user.PointsBalance -= vote.BettedPoints;
            }

            // Domain Logic: The Poll strictly calculates its own early bird window
            var totalSeconds = (ClosingTime - CreatedAt).TotalSeconds;
            var elapsedSeconds = (DateTime.UtcNow - CreatedAt).TotalSeconds;

            var finalizedVote = new Vote(vote.UserId, vote.OptionIndices, vote.BettedPoints)
            {
                IsEarlyBird = totalSeconds > 0 && (elapsedSeconds / totalSeconds) <= 0.20
            };

            Votes.Add(finalizedVote);
        }

        // Domain Logic: Resolves the poll securely and generates a payout dictionary
        public Dictionary<Guid, int> Resolve()
        {
            if (!IsClosed())
                throw new InvalidOperationException("Poll is not closed yet.");

            if (IsResolved)
                return new Dictionary<Guid, int>();

            var winningIndices = GetWinningOptions();
            var payouts = new Dictionary<Guid, int>();

            foreach (var vote in Votes)
            {
                bool isWinner = vote.OptionIndices.Any(idx => winningIndices.Contains(idx));

                if (isWinner && vote.BettedPoints > 0)
                {
                    double profitMultiplier = IsDailyChallenge ? 2.0 : (vote.IsEarlyBird ? 1.5 : 1.0);
                    int profit = (int)(vote.BettedPoints * profitMultiplier);
                    int totalPayout = vote.BettedPoints + profit;

                    if (payouts.ContainsKey(vote.UserId))
                        payouts[vote.UserId] += totalPayout;
                    else
                        payouts.Add(vote.UserId, totalPayout);
                }
            }

            IsResolved = true;
            return payouts;
            }

        public List<int> GetWinningOptions()
        {
            var voteCounts = new Dictionary<int, int>();
            foreach (var v in Votes)
            {
                foreach (var idx in v.OptionIndices)
                {
                    if (!voteCounts.ContainsKey(idx)) voteCounts[idx] = 0;
                    voteCounts[idx]++;
                }
            }

            int maxVotes = voteCounts.Any() ? voteCounts.Values.Max() : 0;
            return voteCounts.Where(kv => kv.Value == maxVotes).Select(kv => kv.Key).ToList();
        }

        public bool Equals(Poll? other) => other != null && Id == other.Id;
        public int CompareTo(Poll? other) => ClosingTime.CompareTo(other?.ClosingTime);
    }
}
