1. Replace backend/Models/Exeptions/PollDomainExeption.cs with backend/Models/Exceptions/PollDomainException.csDelete the misspelled folder and file backend/Models/Exeptions/PollDomainExeption.cs, then create backend/Models/Exceptions/PollDomainException.cs with structured domain error metadata (ErrorCode and PollId):   

namespace PollSystem.Models.Exceptions
{
    public class PollDomainException : Exception
    {
        public string ErrorCode { get; }
        public Guid? PollId { get; }

        public PollDomainException(string message, string errorCode = "POLL_DOMAIN_ERROR", Guid? pollId = null)
            : base(message)
        {
            ErrorCode = errorCode;
            PollId = pollId;
        }
    }
}


2. Throw PollDomainException in backend/Models/Entities/Poll.cs
Add using PollSystem.Models.Exceptions; at the top of backend/Models/Entities/Poll.cs and throw PollDomainException for domain rule violations in CastVote and Resolve

using PollSystem.Models.Exceptions;

namespace PollSystem.Models.Entities
{
    public class Poll : IEntity, IEquatable<Poll>, IComparable<Poll>
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
                throw new PollDomainException("Voting for this poll has ended.", "POLL_CLOSED", Id);

            if (!vote.OptionIndices.Any())
                throw new ArgumentException("You must select at least one option.");

            if (!AllowMultiple && vote.OptionIndices.Count > 1)
                throw new PollDomainException("This poll only allows a single choice.", "MULTIPLE_CHOICES_NOT_ALLOWED", Id);

            foreach (var index in vote.OptionIndices)
            {
                if (index < 0 || index >= Options.Count)
                    throw new ArgumentOutOfRangeException(nameof(vote.OptionIndices), "Option doesn't exist.");
            }

            if (vote.BettedPoints > 0)
            {
                if (user.PointsBalance < vote.BettedPoints)
                    throw new PollDomainException("Insufficient points balance.", "INSUFFICIENT_POINTS", Id);

                user.PointsBalance -= vote.BettedPoints;
            }

            var totalSeconds = (ClosingTime - CreatedAt).TotalSeconds;
            var elapsedSeconds = (DateTime.UtcNow - CreatedAt).TotalSeconds;

            var finalizedVote = new Vote(vote.UserId, vote.OptionIndices, vote.BettedPoints)
            {
                IsEarlyBird = totalSeconds > 0 && (elapsedSeconds / totalSeconds) <= 0.20
            };

            Votes.Add(finalizedVote);
        }

        public Dictionary<Guid, int> Resolve()
        {
            if (!IsClosed())
                throw new PollDomainException("Poll is not closed yet.", "POLL_NOT_CLOSED", Id);

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


3. Create Custom Exception Filter backend/Filters/PollDomainExceptionFilter.cs
Create backend/Filters/PollDomainExceptionFilter.cs to intercept PollDomainException, log structured domain diagnostics, and return a standardized domain error response:

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PollSystem.Models.Exceptions;

namespace PollSystem.Filters
{
    public sealed class PollDomainExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<PollDomainExceptionFilter> _logger;

        public PollDomainExceptionFilter(ILogger<PollDomainExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not PollDomainException domainException)
                return;

            _logger.LogWarning(
                domainException,
                "Poll domain rule violated ({ErrorCode}) for poll {PollId}: {Message}",
                domainException.ErrorCode,
                domainException.PollId,
                domainException.Message);

            context.Result = new BadRequestObjectResult(new
            {
                error = domainException.Message,
                errorCode = domainException.ErrorCode,
                pollId = domainException.PollId
            });

            context.ExceptionHandled = true;
        }
    }
}

4. Register the Filter in backend/Program.cs
Add using PollSystem.Filters; at the top of backend/Program.cs and attach PollDomainExceptionFilter to AddControllers

using PollSystem.Filters;


builder.Services.AddControllers(options =>
{
    options.Filters.Add<PollDomainExceptionFilter>();
});


5. Let PollDomainException Bubble Up in backend/Controllers/PollsController.cs
Add using PollSystem.Models.Exceptions; to backend/Controllers/PollsController.cs and exclude PollDomainException from the generic catch block in Vote (lines 48–62) so PollDomainExceptionFilter handles it:

using PollSystem.Models.Exceptions;

	[Authorize]
        [HttpPost("{id}/vote")]
        public async Task<IActionResult> Vote(Guid id, [FromBody] VoteDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            try
            {
                var (remainingPoints, isEarlyBird) = await _pollService.CastVoteAsync(id, userId, dto);
                return Ok(new { message = "Vote accepted!", remainingPoints, isEarlyBird });
            }
            catch (Exception ex) when (ex is not PollDomainException)
            {
                return BadRequest(new { error = ex.Message });
            }
        }



6. Update Unit Tests in PollSystem.Tests/Unit/PollDomainTests.cs
Add using PollSystem.Models.Exceptions; to PollSystem.Tests/Unit/PollDomainTests.cs and update the four domain-rule tests (CastVote_ClosedPoll, CastVote_MultipleOptionsWhenDisallowed, CastVote_InsufficientPoints, Resolve_PollNotClosed) to assert PollDomainException, ErrorCode, and PollId

using PollSystem.Models.Exceptions;

	[Fact]
        public void CastVote_ClosedPoll_ThrowsPollDomainException()
        {
            var user = new User("alice", "hashed_password", 100);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddMinutes(-1),
                Options = new List<PollOption> { new("Yes") { Position = 0 } }
            };

            var ex = Assert.Throws<PollDomainException>(() =>
                poll.CastVote(user, new Vote(user.Id, new List<int> { 0 }, 10)));

            Assert.Equal("POLL_CLOSED", ex.ErrorCode);
            Assert.Equal(poll.Id, ex.PollId);
        }


	[Fact]
        public void CastVote_MultipleOptionsWhenDisallowed_ThrowsPollDomainException()
        {
            var user = new User("alice", "hashed_password", 100);
            var poll = new Poll
            {
                AllowMultiple = false,
                ClosingTime = DateTime.UtcNow.AddHours(1),
                Options = new List<PollOption>
                {
                    new("A") { Position = 0 },
                    new("B") { Position = 1 }
                }
            };

            var ex = Assert.Throws<PollDomainException>(() =>
                poll.CastVote(user, new Vote(user.Id, new List<int> { 0, 1 }, 0)));

            Assert.Equal("MULTIPLE_CHOICES_NOT_ALLOWED", ex.ErrorCode);
            Assert.Equal(poll.Id, ex.PollId);
        }


	[Fact]
        public void CastVote_InsufficientPoints_ThrowsPollDomainException()
        {
            var user = new User("alice", "hashed_password", 15);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddHours(1),
                Options = new List<PollOption> { new("A") { Position = 0 } }
            };

            var ex = Assert.Throws<PollDomainException>(() =>
                poll.CastVote(user, new Vote(user.Id, new List<int> { 0 }, 30)));

            Assert.Equal("INSUFFICIENT_POINTS", ex.ErrorCode);
            Assert.Equal(poll.Id, ex.PollId);
        }


        [Fact]
        public void Resolve_PollNotClosed_ThrowsPollDomainException()
        {
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddMinutes(15)
            };

            var ex = Assert.Throws<PollDomainException>(() => poll.Resolve());

            Assert.Equal("POLL_NOT_CLOSED", ex.ErrorCode);
            Assert.Equal(poll.Id, ex.PollId);
        }

7. Add Integration Test in PollSystem.Tests/Integration/PollsAndNotificationsIntegrationTests.cs
Append this integration test to PollSystem.Tests/Integration/PollsAndNotificationsIntegrationTests.cs to verify that PollDomainExceptionFilter intercepts PollDomainException over HTTP and returns the structured error, errorCode, and pollId payload:

	[Fact]
        public async Task Vote_InsufficientPoints_HandledByPollDomainExceptionFilter()
        {
            var (client, _, _) = await RegisterAuthenticatedUserAsync("domn");
            var question = $"Domain Exception Test {Guid.NewGuid():N}";

            var createResponse = await client.PostAsJsonAsync("/api/polls", new
            {
                question,
                category = "Technology",
                tags = new[] { "domain" },
                options = new[] { "Yes", "No" },
                durationSeconds = 300,
                allowMultiple = false
            });
            createResponse.EnsureSuccessStatusCode();

            var feed = await client.GetFromJsonAsync<JsonElement>("/api/polls");
            var pollId = feed.EnumerateArray()
                .First(p => p.GetProperty("question").GetString() == question)
                .GetProperty("id").GetGuid();

            var voteResponse = await client.PostAsJsonAsync($"/api/polls/{pollId}/vote", new
            {
                optionIndices = new[] { 0 },
                bettedPoints = 500
            });

            Assert.Equal(HttpStatusCode.BadRequest, voteResponse.StatusCode);

            var errorPayload = await voteResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Insufficient points balance.", errorPayload.GetProperty("error").GetString());
            Assert.Equal("INSUFFICIENT_POINTS", errorPayload.GetProperty("errorCode").GetString());
            Assert.Equal(pollId, errorPayload.GetProperty("pollId").GetGuid());
        }
