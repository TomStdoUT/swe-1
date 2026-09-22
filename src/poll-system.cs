using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PollSystem.Domain
{
    // Record representing a poll choice
    public record PollOption(string text);

    // Readonly struct representing a vote
    public readonly struct Vote
    {
        public Guid UserId { get; }
        public int OptionIndex { get; }
        public int BettedPoints { get; }

        public Vote(Guid userId, int optionIndex, int pointsBet = 0)
        {
            UserId = userId;
            OptionIndex = optionIndex;
            BettedPoints = pointsBet;
        }
    }

    // Extension method to check if a poll's time limit has expired
    public static class PollExtension
    {
        public static bool IsClosed(this Poll poll)
        {
            return DateTime.UtcNow >= poll.ClosingTime;
        }
    }

    // Poll Domain Model supporting custom time limits
    public class Poll : IEquatable<Poll>, IComparable<Poll>
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid CreatorId { get; init; }
        public string Question { get; set; } = string.Empty;
        public List<PollOption> Options { get; set; } = new();
        public DateTime ClosingTime { get; set; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        private readonly List<Vote> _votes = new();
        public IEnumerable<Vote> Votes => _votes.AsReadOnly();

        public void CastVote(Vote vote)
        {
            if (this.IsClosed())
            {
                throw new InvalidOperationException("Voting for this poll has ended.");
            }

            if (vote.OptionIndex < 0 || vote.OptionIndex >= Options.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(vote.OptionIndex), "Option doesn't exist, invalid option selected.");
            }

            _votes.Add(vote);
        }

        public bool Equals(Poll? other) => other != null && Id == other.Id;
        public int CompareTo(Poll? other) => ClosingTime.CompareTo(other?.ClosingTime);
    }

    // PollManager handling timed poll creation & state persistence
    public class PollManager : IEnumerable<Poll>
    {
        private readonly List<Poll> _polls = new();

        public Poll CreatePoll(Guid creatorId, string question, List<PollOption> options, DateTime closingTime)
        {
            if (closingTime <= DateTime.UtcNow)
            {
                throw new ArgumentException("The closing time should be set in the future.");
            }

            if (options == null || options.Count < 2 || options.Count > 6)
            {
                throw new ArgumentException("A poll requires between 2 and 6 options.");
            }

            var poll = new Poll
            {
                CreatorId = creatorId,
                Question = question,
                Options = options,
                ClosingTime = closingTime
            };

            _polls.Add(poll);
            return poll;
        }

        public IEnumerable<Poll> GetCreatorPolls(Guid creatorId)
        {
            return _polls.Where(p => p.CreatorId == creatorId).OrderByDescending(p => p.ClosingTime);
        }

        public async Task SaveStateAsync(string filePath = "poll_snapshot.json")
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            using FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, _polls, options);
        }

        public IEnumerator<Poll> GetEnumerator() => _polls.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Testing Issue #2: Create Timed Poll ===");

            var manager = new PollManager();
            var creatorId = Guid.NewGuid();
            var voterId = Guid.NewGuid();

            // 1. Create a poll with a 5-minute duration limit
            var poll = manager.CreatePoll(
                creatorId: creatorId,
                question: "Which feature should we build next?",
                options: new List<PollOption> { new("Dark Mode"), new("Export Stats"), new("Live Chat") },
                closingTime: DateTime.UtcNow.AddMinutes(5)
            );

            Console.WriteLine($"[Created] Poll ID: {poll.Id}");
            Console.WriteLine($"[Question] {poll.Question}");
            Console.WriteLine($"[Closing Time] {poll.ClosingTime.ToLocalTime()} (Is Closed: {poll.IsClosed()})");

            // 2. Cast a valid vote while active
            var validVote = new Vote(voterId, optionIndex: 0, pointsBet: 100);
            poll.CastVote(validVote);
            Console.WriteLine("[Success] Vote cast successfully.");

            // 3. Save state to JSON snapshot
            await manager.SaveStateAsync("poll_snapshot.json");
            Console.WriteLine("[Success] Saved state to poll_snapshot.json.");
        }
    }
}git 