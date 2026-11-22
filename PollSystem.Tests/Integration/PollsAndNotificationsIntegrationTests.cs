using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PollSystem.Data;
using PollSystem.Services.Interfaces;
using Xunit;

namespace PollSystem.Tests.Integration
{
    public class PollsAndNotificationsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public PollsAndNotificationsIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task<(HttpClient Client, Guid UserId, string Username)> RegisterAuthenticatedUserAsync(string prefix)
        {
            var client = _factory.CreateClient();
            var username = $"{prefix}_{Guid.NewGuid():N}"[..16];
            var password = "StrongPassword123";

            var response = await client.PostAsJsonAsync("/api/register", new { username, password });
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
            var token = payload.GetProperty("token").GetString();
            var userId = payload.GetProperty("profile").GetProperty("id").GetGuid();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, userId, username);
        }

        [Fact]
        public async Task CreatePollVoteAndComment_ValidFlow_PersistsAndUpdatesUserBalance()
        {
            var (client, _, username) = await RegisterAuthenticatedUserAsync("poll");
            var question = $"Integration Question {Guid.NewGuid():N}";

            var createResponse = await client.PostAsJsonAsync("/api/polls", new
            {
                question,
                category = "Technology",
                tags = new[] { "csharp", "testing" },
                options = new[] { "Option A", "Option B" },
                durationSeconds = 300,
                allowMultiple = false
            });
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            var feed = await client.GetFromJsonAsync<JsonElement>("/api/polls");
            var createdPoll = feed.EnumerateArray()
                .First(p => p.GetProperty("question").GetString() == question);
            var pollId = createdPoll.GetProperty("id").GetGuid();

            var voteResponse = await client.PostAsJsonAsync($"/api/polls/{pollId}/vote", new
            {
                optionIndices = new[] { 0 },
                bettedPoints = 25
            });
            Assert.Equal(HttpStatusCode.OK, voteResponse.StatusCode);

            var votePayload = await voteResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(75, votePayload.GetProperty("remainingPoints").GetInt32());
            Assert.True(votePayload.GetProperty("isEarlyBird").GetBoolean());

            var commentResponse = await client.PostAsJsonAsync($"/api/polls/{pollId}/comments", new
            {
                content = "Option A is clearly ahead."
            });
            Assert.Equal(HttpStatusCode.OK, commentResponse.StatusCode);

            var commentPayload = await commentResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(username, commentPayload.GetProperty("authorName").GetString());
            Assert.Equal("Option A is clearly ahead.", commentPayload.GetProperty("content").GetString());
        }

        [Fact]
        public async Task DeletePoll_NonOwnerForbiddenAndOwnerSucceeds()
        {
            var (ownerClient, _, _) = await RegisterAuthenticatedUserAsync("ownr");
            var (otherClient, _, _) = await RegisterAuthenticatedUserAsync("othr");
            var question = $"Delete Test {Guid.NewGuid():N}";

            var createResponse = await ownerClient.PostAsJsonAsync("/api/polls", new
            {
                question,
                category = "Fun",
                tags = new[] { "delete" },
                options = new[] { "Yes", "No" },
                durationSeconds = 180,
                allowMultiple = false
            });
            createResponse.EnsureSuccessStatusCode();

            var feed = await ownerClient.GetFromJsonAsync<JsonElement>("/api/polls");
            var pollId = feed.EnumerateArray()
                .First(p => p.GetProperty("question").GetString() == question)
                .GetProperty("id").GetGuid();

            var forbiddenDelete = await otherClient.DeleteAsync($"/api/polls/{pollId}");
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenDelete.StatusCode);

            var ownerDelete = await ownerClient.DeleteAsync($"/api/polls/{pollId}");
            Assert.Equal(HttpStatusCode.OK, ownerDelete.StatusCode);

            var notFoundDelete = await ownerClient.DeleteAsync($"/api/polls/{pollId}");
            Assert.Equal(HttpStatusCode.NotFound, notFoundDelete.StatusCode);
        }

        [Fact]
        public async Task ResolveClosedPoll_CreatesNotificationsAndPopulatesFollowingVotes()
        {
            var (followerClient, _, _) = await RegisterAuthenticatedUserAsync("foll");
            var (voterClient, voterId, voterUsername) = await RegisterAuthenticatedUserAsync("votr");

            var followResponse = await followerClient.PostAsync($"/api/users/{voterId}/follow", null);
            Assert.Equal(HttpStatusCode.NoContent, followResponse.StatusCode);

            var question = $"Resolve Test {Guid.NewGuid():N}";
            var createResponse = await followerClient.PostAsJsonAsync("/api/polls", new
            {
                question,
                category = "Sport",
                tags = new[] { "finals" },
                options = new[] { "Team Red", "Team Blue" },
                durationSeconds = 300,
                allowMultiple = false
            });
            createResponse.EnsureSuccessStatusCode();

            var feed = await voterClient.GetFromJsonAsync<JsonElement>("/api/polls");
            var pollId = feed.EnumerateArray()
                .First(p => p.GetProperty("question").GetString() == question)
                .GetProperty("id").GetGuid();

            var voteResponse = await voterClient.PostAsJsonAsync($"/api/polls/{pollId}/vote", new
            {
                optionIndices = new[] { 0 },
                bettedPoints = 20
            });
            voteResponse.EnsureSuccessStatusCode();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var pollService = scope.ServiceProvider.GetRequiredService<IPollService>();

                var poll = await db.Polls.FirstAsync(p => p.Id == pollId);
                poll.ClosingTime = DateTime.UtcNow.AddSeconds(-5);
                await db.SaveChangesAsync();

                await pollService.ResolveClosedPollsAsync();
            }

            var notifications = await voterClient.GetFromJsonAsync<JsonElement>("/api/notifications");
            Assert.True(notifications.GetArrayLength() >= 1);
            Assert.Contains(question, notifications[0].GetProperty("content").GetString());

            var voterProfile = await voterClient.GetFromJsonAsync<JsonElement>("/api/user");
            Assert.Equal(130, voterProfile.GetProperty("pointsBalance").GetInt32());

            var followingVotes = await followerClient.GetFromJsonAsync<JsonElement>("/api/users/following/votes");
            var resolvedVote = followingVotes.EnumerateArray()
                .First(v => v.GetProperty("pollId").GetGuid() == pollId);
            Assert.Equal(voterUsername, resolvedVote.GetProperty("username").GetString());
            Assert.True(resolvedVote.GetProperty("wasCorrect").GetBoolean());
        }

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

        [Fact]
        public async Task BoostPoll_ValidAndDuplicateRequests_UpdatesPointsSortsFeedAndEnforcesDomainRules()
        {
            var (client, _, _) = await RegisterAuthenticatedUserAsync("bost");
            var boostedQuestion = $"Boosted Question {Guid.NewGuid():N}";
            var laterRegularQuestion = $"Later Regular Question {Guid.NewGuid():N}";

            var firstCreate = await client.PostAsJsonAsync("/api/polls", new
            {
                question = boostedQuestion,
                category = "Technology",
                tags = new[] { "boost" },
                options = new[] { "Yes", "No" },
                durationSeconds = 300,
                allowMultiple = false
            });
            firstCreate.EnsureSuccessStatusCode();

            var secondCreate = await client.PostAsJsonAsync("/api/polls", new
            {
                question = laterRegularQuestion,
                category = "Technology",
                tags = new[] { "regular" },
                options = new[] { "Yes", "No" },
                durationSeconds = 3600,
                allowMultiple = false
            });
            secondCreate.EnsureSuccessStatusCode();

            var initialFeed = await client.GetFromJsonAsync<JsonElement>("/api/polls");
            var boostedPollId = initialFeed.EnumerateArray()
                .First(p => p.GetProperty("question").GetString() == boostedQuestion)
                .GetProperty("id").GetGuid();

            var boostResponse = await client.PostAsync($"/api/polls/{boostedPollId}/boost", null);
            Assert.Equal(HttpStatusCode.OK, boostResponse.StatusCode);

            var boostPayload = await boostResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(boostPayload.GetProperty("isBoosted").GetBoolean());
            Assert.Equal(70, boostPayload.GetProperty("remainingPoints").GetInt32());

            var updatedFeed = await client.GetFromJsonAsync<JsonElement>("/api/polls");
            var firstItem = updatedFeed.EnumerateArray().First();
            Assert.Equal(boostedPollId, firstItem.GetProperty("id").GetGuid());
            Assert.True(firstItem.GetProperty("isBoosted").GetBoolean());

            var duplicateBoostResponse = await client.PostAsync($"/api/polls/{boostedPollId}/boost", null);
            Assert.Equal(HttpStatusCode.BadRequest, duplicateBoostResponse.StatusCode);

            var errorPayload = await duplicateBoostResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("POLL_ALREADY_BOOSTED", errorPayload.GetProperty("errorCode").GetString());
        }

        [Fact]
        public async Task BoostPoll_DailyChallenge_ReturnsBadRequestWithDomainError()
        {
            var (client, _, _) = await RegisterAuthenticatedUserAsync("dlyb");

            using (var scope = _factory.Services.CreateScope())
            {
                var pollService = scope.ServiceProvider.GetRequiredService<IPollService>();
                await pollService.EnsureDailyQuestionAsync();
            }

            var feed = await client.GetFromJsonAsync<JsonElement>("/api/polls");
            var dailyPollId = feed.EnumerateArray()
                .First(p => p.GetProperty("isDailyChallenge").GetBoolean())
                .GetProperty("id").GetGuid();

            var boostResponse = await client.PostAsync($"/api/polls/{dailyPollId}/boost", null);
            Assert.Equal(HttpStatusCode.BadRequest, boostResponse.StatusCode);

            var errorPayload = await boostResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Daily challenges cannot be boosted.", errorPayload.GetProperty("error").GetString());
            Assert.Equal("DAILY_CHALLENGE_CANNOT_BE_BOOSTED", errorPayload.GetProperty("errorCode").GetString());
            Assert.Equal(dailyPollId, errorPayload.GetProperty("pollId").GetGuid());
        }
    }
}