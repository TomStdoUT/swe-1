using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PollSystem.Domain
{

    //Tomas V part from poll-system branch

    public record PollOption(string text);

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
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid CreatorId { get; init; }
        public string Question { get; set; } = string.Empty;
        public List<PollOption> Options { get; set; } = new();
        public DateTime ClosingTime { get; set; }

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

        public async Task SaveStateAsync(string filePath = "polls.json")
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            using FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, _polls, options);
        }

        public IEnumerator<Poll> GetEnumerator() => _polls.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    //poll-system branch ends HERE
    

   class Program
    {
        static async Task Main(string[] args)
        {
            //from poll-systems branch
            var manager = new PollManager();
            var creatorId = Guid.NewGuid();
            var voterId = Guid.NewGuid();

            try
            {
                var poll = manager.CreatePoll(
                    creatorId: creatorId,
                    question: "Ar C# gera kalba?",
                    options: new List<PollOption> { new("Taip"), new("Ne") },
                    closingTime: DateTime.UtcNow.AddMinutes(5)
                );

                Console.WriteLine($"[Success] Poll created with closing time: {poll.ClosingTime}");

                var creatorPolls = manager.GetCreatorPolls(creatorId);

                var validVote = new Vote(voterId, optionIndex: 0, pointsBet: 50);
                poll.CastVote(validVote);

                poll.ClosingTime = DateTime.UtcNow.AddMinutes(-1);

                Console.WriteLine($"Poll closed status: {poll.IsClosed()}");
                var lateVote = new Vote(Guid.NewGuid(), 1);

                poll.CastVote(lateVote);
            }
            catch (Exception)
            {
                Console.WriteLine("An exception has occured, due to late vote");
            }
            await manager.SaveStateAsync("poll_snapshot.json");
            Console.WriteLine("\nState serialized to stream (poll_snapshot.json).\n");

            //poll system branch ends here
            
            var postManager = new PostManager();
            var pollManager = new PollManager();

            // Load existing posts from file at startup
            await postManager.LoadStateAsync("posts.json");

            Console.WriteLine("=== Social Media & Polling System ===");

            // Display existing feed from previous runs
            Console.WriteLine("\n--- CURRENT FEED ---");
            var currentFeed = postManager.GetFeed().ToList();
            if (currentFeed.Count == 0)
            {
                Console.WriteLine("No posts yet.");
            }
            else
            {
                foreach (var post in currentFeed)
                {
                    Console.WriteLine($"[{post.CreatedAt:yyyy-MM-dd HH:mm}] {post.AuthorName}: {post.Content}");
                }
            }

            // ask user for a new post
            Console.WriteLine("\n--- CREATE A NEW POST ---");
            Console.Write("Enter your name: ");
            string? authorName = Console.ReadLine();

            Console.Write("Write your post content: ");
            string? content = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(authorName) && !string.IsNullOrWhiteSpace(content))
            {
                var userId = Guid.NewGuid();
                postManager.CreatePost(userId, authorName, content);

                // Save posts to file
                await postManager.SaveStateAsync("posts.json");
                Console.WriteLine("\nPost created and saved successfully!");

                // updated feed
                Console.WriteLine("\n--- UPDATED FEED ---");
                foreach (var post in postManager.GetFeed())
                {
                    Console.WriteLine($"[{post.CreatedAt:yyyy-MM-dd HH:mm}] {post.AuthorName}: {post.Content}");
                }
            }
            else
            {
                Console.WriteLine("Error: Name and content cannot be empty.");
            }
        }
    }
}
