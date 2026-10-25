using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PollSystem.Models.Entities;

namespace PollSystem.Data
{
    public sealed class LegacyJsonImporter
    {
        private readonly AppDbContext _db;
        private readonly string _contentRoot;

        public LegacyJsonImporter(AppDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _contentRoot = environment.ContentRootPath;
        }

        public async Task ImportIfDatabaseIsEmptyAsync(CancellationToken cancellationToken = default)
        {
            if (await _db.Users.AnyAsync(cancellationToken) ||
                await _db.Polls.AnyAsync(cancellationToken))
            {
                return;
            }

            var usersPath = FindLegacyFile("users.json");
            var pollsPath = FindLegacyFile("poll_snapshot.json");
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var users = await ReadAsync<List<LegacyUserData>>(usersPath, jsonOptions, cancellationToken)
                ?? new List<LegacyUserData>();
            var polls = await ReadAsync<List<LegacyPollData>>(pollsPath, jsonOptions, cancellationToken)
                ?? new List<LegacyPollData>();

            if (users.Count == 0 && polls.Count == 0)
                return;

            var importedUsers = users.Select(user => new User
            {
                Id = user.Id,
                Username = user.Username,
                PasswordHash = user.PasswordHash,
                PointsBalance = user.PointsBalance,
                Cosmetics = (user.PurchasedCosmetics ?? new List<string>())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(name => new UserCosmetic { UserId = user.Id, Name = name })
                    .ToList()
            }).ToList();
            var userIds = importedUsers.Select(user => user.Id).ToHashSet();

            foreach (var legacyUser in users)
            {
                var user = importedUsers.First(imported => imported.Id == legacyUser.Id);
                user.Following = (legacyUser.FollowingUserIds ?? new List<Guid>())
                    .Where(followedId => userIds.Contains(followedId) && followedId != user.Id)
                    .Distinct()
                    .Select(followedId => new UserFollow
                    {
                        FollowerId = user.Id,
                        FollowedId = followedId
                    })
                    .ToList();
            }

            var importedPolls = polls.Select(poll => new Poll
            {
                Id = poll.Id,
                CreatorId = poll.CreatorId,
                Question = poll.Question,
                Category = poll.Category,
                Tags = (poll.Tags ?? new List<string>())
                    .Select(value => new PollTag { PollId = poll.Id, Value = value })
                    .ToList(),
                Options = (poll.Options ?? new List<LegacyPollOptionData>())
                    .Select((option, position) => new PollOption(option.text)
                    {
                        PollId = poll.Id,
                        Position = position
                    })
                    .ToList(),
                ClosingTime = poll.ClosingTime,
                CreatedAt = poll.CreatedAt,
                AllowMultiple = poll.AllowMultiple,
                IsDailyChallenge = poll.IsDailyChallenge,
                NotificationsGenerated = poll.NotificationsGenerated,
                IsResolved = poll.IsResolved,
                Comments = (poll.Comments ?? new List<LegacyCommentData>())
                    .Where(comment => userIds.Contains(comment.AuthorId))
                    .Select(comment => new Comment
                    {
                        Id = comment.Id,
                        PollId = poll.Id,
                        AuthorId = comment.AuthorId,
                        AuthorName = comment.AuthorName,
                        Content = comment.Content,
                        CreatedAt = comment.CreatedAt
                    })
                    .ToList(),
                Votes = GetLegacyVotes(poll)
                    .Where(vote => userIds.Contains(vote.UserId))
                    .Select(vote => new Vote(vote.UserId, vote.OptionIndices ?? new List<int>(), vote.BettedPoints)
                    {
                        Id = vote.Id == Guid.Empty ? Guid.NewGuid() : vote.Id,
                        PollId = poll.Id,
                        IsEarlyBird = vote.IsEarlyBird
                    })
                    .ToList()
            }).ToList();

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            _db.Users.AddRange(importedUsers);
            _db.Polls.AddRange(importedPolls);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        private string? FindLegacyFile(string fileName)
        {
            var candidates = new[]
            {
                Path.Combine(_contentRoot, fileName),
                Path.Combine(_contentRoot, "backend", fileName)
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static async Task<T?> ReadAsync<T>(
            string? path,
            JsonSerializerOptions options,
            CancellationToken cancellationToken)
        {
            if (path is null)
                return default;

            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken);
        }

        private static IEnumerable<LegacyVoteData> GetLegacyVotes(LegacyPollData poll)
        {
            var votes = poll.Votes?.Count > 0 ? poll.Votes : poll._votes;
            return votes ?? Enumerable.Empty<LegacyVoteData>();
        }
    }
}
