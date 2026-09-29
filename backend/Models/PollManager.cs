using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace PollSystem.Domain
{

    public class User
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public int PointsBalance { get; set; } = 100; // Default starting balance
        public record PollOption(string text, bool isCorrect = false);
        public User() { }

        public User(string username, int initialPoints = 100)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username cannot be empty.", nameof(username));
            }

            Username = username;
            PointsBalance = initialPoints;
        }
    }


    public record PollOption(string text, bool isCorrect = false);

    public readonly struct Vote
    {
        public Guid UserId { get; }
        public List<int> OptionIndices { get; }
        public int BettedPoints { get; }

        public Vote(Guid userId, List<int> optionIndices, int pointsBet = 0)
        {
            UserId = userId;
            OptionIndices = optionIndices ?? new List<int>();
            BettedPoints = pointsBet;
        }
    }

    public static class PollExtension
    {
        public static bool IsClosed(this Poll poll)
        {
            return DateTime.UtcNow >= poll.ClosingTime;
        }
    }
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

        private readonly List<Vote> _votes = new();
        public IEnumerable<Vote> Votes => _votes.AsReadOnly();

        public bool CastVote(User user, Vote vote)
    {
        if (this.IsClosed()) throw new InvalidOperationException("Voting for this poll has ended.");
        
        if (vote.OptionIndices == null || !vote.OptionIndices.Any())
            throw new ArgumentException("You must select at least one option.");

        if (!AllowMultiple && vote.OptionIndices.Count > 1)
            throw new InvalidOperationException("This poll only allows a single choice.");

        foreach (var index in vote.OptionIndices)
        {
            if (index < 0 || index >= Options.Count)
                throw new ArgumentOutOfRangeException(nameof(vote.OptionIndices), "Option doesn't exist.");
        }

        var correctIndices = Options
            .Select((opt, index) => new { opt, index })
            .Where(x => x.opt.isCorrect)
            .Select(x => x.index)
            .ToList();

        bool isCorrect = vote.OptionIndices.Count == correctIndices.Count && 
                         vote.OptionIndices.All(correctIndices.Contains);

        if (vote.BettedPoints > 0)
        {
            if (user.PointsBalance < vote.BettedPoints)
                throw new InvalidOperationException("Insufficient points balance.");

            user.PointsBalance -= vote.BettedPoints;

            if (isCorrect)
            {
                user.PointsBalance += (vote.BettedPoints * 2);
            }
        }

        _votes.Add(vote);

        return isCorrect;
    }

        public bool Equals(Poll? other) => other != null && Id == other.Id;
        public int CompareTo(Poll? other) => ClosingTime.CompareTo(other?.ClosingTime);
    }

    public class PollManager : IEnumerable<Poll>
    {
        private readonly List<Poll> _polls = new();

        public Poll CreatePoll(Guid creatorId, string question, string category, List<string> tags, List<PollOption> options, DateTime closingTime, bool allowMultiple)
    {
        if (closingTime <= DateTime.UtcNow) throw new ArgumentException("The closing time should be set in the future.");
        if (options == null || options.Count < 2 || options.Count > 6) throw new ArgumentException("A poll requires between 2 and 6 options.");

        var correctCount = options.Count(o => o.isCorrect);
        if (correctCount == 0) throw new ArgumentException("You must select at least one correct answer.");
        if (correctCount == options.Count) throw new ArgumentException("There must always be at least one incorrect choice.");
        if (!allowMultiple && correctCount > 1) throw new ArgumentException("Single choice polls can only have one correct answer.");

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

        public IEnumerator<Poll> GetEnumerator() => _polls.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public IEnumerable<Poll> GetAllPolls()
        {
        
        return _polls.OrderByDescending(p => p.ClosingTime);
        
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
    }
 

    public record PollCreateDto(string Question, string Category, List<string> Tags, List<string> Options, int DurationSeconds, bool AllowMultiple, List<int> CorrectOptionIndices);
    public record LoginDto(string Username, string? Password = null);
    public record PostCreateDto(string Content);
}
