using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PollSystem.Domain
{
    // Represents a user with points
    public class User
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public int PointsBalance { get; set; } = 100;

        public User() { }

        public User(string username, int initialPoints = 100)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username cannot be empty.", nameof(username));
            Username = username;
            PointsBalance = initialPoints;
        }
    }

    // Record representing a poll choice
    public record PollOption(string text);

    // Readonly struct representing a vote
    public struct Vote
    {
        public Guid UserId { get; set; }
        public List<int> OptionIndices { get; set; } = new();
        public int BettedPoints { get; set; }

        public Vote() { }
        public Vote(Guid userId, List<int> optionIndices, int pointsBet = 0)
        {
            UserId = userId;
            OptionIndices = optionIndices ?? new List<int>();
            BettedPoints = pointsBet;
        }
    }

    // Extension method to check if a poll's time limit has expired
    public static class PollExtension
    {
        public static bool IsClosed(this Poll poll) => DateTime.UtcNow >= poll.ClosingTime;
    }

    // Poll Domain Model supporting custom time limits
    public class Poll : IEquatable<Poll>, IComparable<Poll>
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid CreatorId { get; init; }
        public string Question { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public List<PollOption> Options { get; set; } = new();
        public DateTime ClosingTime { get; set; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public bool AllowMultiple { get; set; }

        public List<Comment> Comments { get; set; } = new();

        [JsonInclude]
        
        private readonly List<Vote> _votes = new();
        public IEnumerable<Vote> Votes => _votes.AsReadOnly();

        public bool CastVote(User user, Vote vote)
        {
            if (this.IsClosed()) throw new InvalidOperationException("Voting for this poll has ended.");
            
if (!vote.OptionIndices.Any())
                throw new ArgumentException("You must select at least one option.");

            if (!AllowMultiple && vote.OptionIndices.Count > 1)
                throw new InvalidOperationException("This poll only allows a single choice.");

            foreach (var index in vote.OptionIndices)
            {
                if (index < 0 || index >= Options.Count)
                    throw new ArgumentOutOfRangeException(nameof(vote.OptionIndices), "Option doesn't exist.");
            }

            if (vote.BettedPoints > 0)
            {
                if (user.PointsBalance < vote.BettedPoints)
                    throw new InvalidOperationException("Insufficient points balance.");

                user.PointsBalance -= vote.BettedPoints;
            }

            _votes.Add(vote);

            // Count all the votes in the poll
            var voteCounts = new Dictionary<int, int>();
            foreach (var v in _votes)
            {
                foreach (var idx in v.OptionIndices)
                {
                    if (!voteCounts.ContainsKey(idx)) voteCounts[idx] = 0;
                    voteCounts[idx]++;
                }
            }
            
            int maxVotes = voteCounts.Any() ? voteCounts.Values.Max() : 0;
            var winningIndices = voteCounts.Where(kv => kv.Value == maxVotes).Select(kv => kv.Key).ToList();
            bool isWinner = vote.OptionIndices.Any(idx => winningIndices.Contains(idx));

            if (isWinner && vote.BettedPoints > 0)
            {
                user.PointsBalance += (vote.BettedPoints * 2);
            }

            return isWinner;
        }

        // MISSING METHODS RESTORED HERE
        public bool Equals(Poll? other) => other != null && Id == other.Id;
        public int CompareTo(Poll? other) => ClosingTime.CompareTo(other?.ClosingTime);
    }

    // PollManager handling timed poll creation & state persistence
    public class PollManager : IEnumerable<Poll>
    {
        private readonly List<Poll> _polls = new();

        public Poll CreatePoll(Guid creatorId, string question, string category, List<string> tags, List<PollOption> options, DateTime closingTime, bool allowMultiple)
        {
            if (closingTime <= DateTime.UtcNow) throw new ArgumentException("The closing time should be set in the future.");
            if (options == null || options.Count < 2 || options.Count > 6) throw new ArgumentException("A poll requires between 2 and 6 options.");

            var poll = new Poll
            {
                CreatorId = creatorId,
                Question = question,
                Category = category,
                Tags = tags,
                Options = options,
                ClosingTime = closingTime,
                AllowMultiple = allowMultiple 
            };

            _polls.Add(poll);
            return poll;
        }

        public IEnumerable<Poll> GetCreatorPolls(Guid creatorId) => _polls.Where(p => p.CreatorId == creatorId).OrderByDescending(p => p.ClosingTime);
        
        public IEnumerable<Poll> GetAllPolls() => _polls.OrderByDescending(p => p.ClosingTime);
        
        public Comment AddComment(Guid pollId, Guid authorId, string authorName, string content)
        {
            var poll = _polls.FirstOrDefault(p => p.Id == pollId);
            if (poll == null) throw new KeyNotFoundException("Poll not found.");
            var comment = new Comment(authorId, authorName, content);
            poll.Comments.Add(comment);
            return comment;
        }

        public async Task SaveStateAsync(string filePath = "poll_snapshot.json")
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            using FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, _polls, options);
        }

        public async Task LoadStateAsync(string filePath = "poll_snapshot.json")
        {
            if (!File.Exists(filePath)) return;

            using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var loadedPolls = await JsonSerializer.DeserializeAsync<List<Poll>>(stream);
        
            if (loadedPolls != null)
            {
                _polls.Clear();
                _polls.AddRange(loadedPolls);
            }
        }

        public async Task<bool> DeletePollAsync(Guid pollId)
        {
            var poll = _polls.FirstOrDefault(p => p.Id == pollId);
            if (poll != null)
            {
                _polls.Remove(poll);
                await SaveStateAsync();
                return true;
            }
            return false;
        }

        public IEnumerator<Poll> GetEnumerator() => _polls.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
 
    // MISSING DTOS RESTORED HERE
    public record PollCreateDto(string Question, string Category, List<string> Tags, List<string> Options, int DurationSeconds, bool AllowMultiple);
    public record LoginDto(string Username, string? Password = null);
    public record PostCreateDto(string Content);
}