using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PollSystem.Domain
{
    public record PollOption(string text);
    // Creating a struct that can only be read and not changed
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

public static class PollExtension
    {
        public static bool IsClosed(this Poll poll)
        {
            return DateTime.UtcNow >= poll.ClosingTime;
        }
    }

public class Poll : IEquatable<Poll>, IComparable<Poll>
    {
        // Every poll has a unique id
        public Guid Id { get;  init; } = Guid.NewGuid();
        // Every poll creator has a unique id
        public Guid CreatorId { get; init; }
        // A list storing questions 
        public string Question { get; set; } =  string.Empty;
        // A list storing poll options
        public List<PollOption> Options { get; set; } = new();
        // Stores the timestamp when voting ends
        public DateTime ClosingTime { get; set; }
        
        private readonly List<Vote> _votes = new();
        public IEnumerable<Vote> Votes => _votes.AsReadOnly();

        public void CastVote(Vote vote)
        {
            if(this.IsClosed())
            {
                throw new InvalidOperationException("Voting for this poll has ended.");
            }

            if(vote.OptionIndex < 0 || vote.OptionIndex >= Options.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(vote.OptionIndex), "Option doesn't exist, invalid option selected.");
            }
        
        _votes.Add(vote);
        
        }
        public bool Equals(Poll? other) => other != null && Id == other.Id;
        public int CompareTo(Poll? other) => ClosingTime.CompareTo(other?.ClosingTime);
    }
    
    public class PollManager : IEnumerable<Poll>
    {
        private readonly List<Poll> _polls = new();
        public Poll CreatePoll(Guid creatorId, string question, List<PollOption> options, DateTime closingTime)
        {
            if(closingTime <= DateTime.UtcNow)
            {
                throw new ArgumentException("The closing time should be set in the future.");
            }

            if(options == null || options.Count < 2 || options.Count > 6)
            {
                throw new ArgumentException("A poll requires between 2 or 6 options.");
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

        public async Task SaveState(string filePath)
        {
            using FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, _polls);
        }

        public IEnumerator<Poll> GetEnumerator()
        {
            foreach(var poll in _polls)
            {
                yield return poll;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    class Program
    {
        static void Main(string[] args)
        {
            var manager = new PollManager();
            Console.WriteLine("Polling System initialized successfully.");
        }
    }
}
