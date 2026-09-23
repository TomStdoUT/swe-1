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

    // Represents a user with points
    public class User
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public int PointsBalance { get; set; } = 100; // Default starting balance

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

        public void CastVote(User user, Vote vote)
        {
            if (this.IsClosed())
            {
                throw new InvalidOperationException("Voting for this poll has ended.");
            }

            if (vote.OptionIndex < 0 || vote.OptionIndex >= Options.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(vote.OptionIndex), "Option doesn't exist, invalid option selected.");
            }

            if (vote.BettedPoints > 0)
            {
                if (user.PointsBalance < vote.BettedPoints)
                {
                    throw new InvalidOperationException("Insufficient points balance to place this bet.");
                }

                user.PointsBalance -= vote.BettedPoints;
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
        static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Cors so react can comunicate with backend
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });

            var app = builder.Build();
            app.UseCors();

            // Storing app state in mem
            var manager = new PollManager();
            var currentUser = new User("Povias", 100);

            // API endpoint to get the points balance
            app.MapGet("/api/user", () => new { currentUser.Username, currentUser.PointsBalance });

            // API endpoint for poll creation point withdrawal
            app.MapPost("/api/polls", (PollCreateDto dto) =>
            {
                if (dto.BettedPoints > currentUser.PointsBalance)
                {
                    return Results.BadRequest(new { error = "Neturite pakankamai taškų!" });
                }

                currentUser.PointsBalance -= dto.BettedPoints;

                return Results.Ok(new { 
                    message = "Apklausa sukurta sėkmingai!", 
                    remainingPoints = currentUser.PointsBalance 
                });
            });

            app.Run("http://localhost:5000");
        }
    }

    // Data transfer object for requests from react
    public record PollCreateDto(string Question, string Category, List<string> Tags, int BettedPoints);
}