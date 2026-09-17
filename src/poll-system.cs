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
    }
    

    class Program
    {
        
    }
}
