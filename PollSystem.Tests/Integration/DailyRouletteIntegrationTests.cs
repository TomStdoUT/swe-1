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
using PollSystem.Models.Entities;
using Xunit;

namespace PollSystem.Tests.Integration
{
    public class DailyRouletteIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public DailyRouletteIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task<(HttpClient Client, Guid UserId)> RegisterAuthenticatedUserAsync(string prefix)
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
            return (client, userId);
        }

        [Fact]
        public async Task RouletteEndpoints_WithoutToken_ReturnUnauthorized()
        {
            var client = _factory.CreateClient();

            var statusResponse = await client.GetAsync("/api/roulette/status");
            var spinResponse = await client.PostAsync("/api/roulette/spin", null);

            Assert.Equal(HttpStatusCode.Unauthorized, statusResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, spinResponse.StatusCode);
        }

        [Fact]
        public async Task StatusAndSpin_ValidFlowAndCooldownEnforcement_UpdatesBalanceAndBlocksSecondSpin()
        {
            var (client, _) = await RegisterAuthenticatedUserAsync("spin");

            var initialStatusResponse = await client.GetAsync("/api/roulette/status");
            Assert.Equal(HttpStatusCode.OK, initialStatusResponse.StatusCode);

            var initialStatus = await initialStatusResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(initialStatus.GetProperty("canSpin").GetBoolean());
            Assert.Equal(JsonValueKind.Null, initialStatus.GetProperty("nextSpinAvailableAtUtc").ValueKind);
            Assert.Equal(JsonValueKind.Null, initialStatus.GetProperty("lastAwardedPoints").ValueKind);
            var segments = initialStatus.GetProperty("wheelSegments").EnumerateArray().Select(x => x.GetInt32()).ToList();
            Assert.Equal(8, segments.Count);

            var spinResponse = await client.PostAsync("/api/roulette/spin", null);
            Assert.Equal(HttpStatusCode.OK, spinResponse.StatusCode);

            var spinResult = await spinResponse.Content.ReadFromJsonAsync<JsonElement>();
            var awardedPoints = spinResult.GetProperty("awardedPoints").GetInt32();
            var winningIndex = spinResult.GetProperty("winningSegmentIndex").GetInt32();
            var newBalance = spinResult.GetProperty("newPointsBalance").GetInt32();

            Assert.Equal(segments[winningIndex], awardedPoints);
            Assert.Equal(100 + awardedPoints, newBalance);

            var cooldownStatus = await client.GetFromJsonAsync<JsonElement>("/api/roulette/status");
            Assert.False(cooldownStatus.GetProperty("canSpin").GetBoolean());
            Assert.Equal(awardedPoints, cooldownStatus.GetProperty("lastAwardedPoints").GetInt32());
            Assert.NotEqual(JsonValueKind.Null, cooldownStatus.GetProperty("nextSpinAvailableAtUtc").ValueKind);

            var secondSpinResponse = await client.PostAsync("/api/roulette/spin", null);
            Assert.Equal(HttpStatusCode.BadRequest, secondSpinResponse.StatusCode);

            var profile = await client.GetFromJsonAsync<JsonElement>("/api/user");
            Assert.Equal(100 + awardedPoints, profile.GetProperty("pointsBalance").GetInt32());
        }

        [Fact]
        public async Task Spin_AfterCooldownExpires_AllowsSpinningAgain()
        {
            var (client, userId) = await RegisterAuthenticatedUserAsync("cool");

            var firstSpinResponse = await client.PostAsync("/api/roulette/spin", null);
            firstSpinResponse.EnsureSuccessStatusCode();
            var firstSpin = await firstSpinResponse.Content.ReadFromJsonAsync<JsonElement>();
            var firstAward = firstSpin.GetProperty("awardedPoints").GetInt32();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var existingSpin = await db.DailyRouletteSpins.FirstAsync(s => s.UserId == userId);
                db.DailyRouletteSpins.Remove(existingSpin);
                db.DailyRouletteSpins.Add(new DailyRouletteSpin(
                    userId,
                    firstAward,
                    DateTime.UtcNow.AddHours(-25)));
                await db.SaveChangesAsync();
            }

            var statusAfterCooldown = await client.GetFromJsonAsync<JsonElement>("/api/roulette/status");
            Assert.True(statusAfterCooldown.GetProperty("canSpin").GetBoolean());

            var secondSpinResponse = await client.PostAsync("/api/roulette/spin", null);
            Assert.Equal(HttpStatusCode.OK, secondSpinResponse.StatusCode);

            var secondSpin = await secondSpinResponse.Content.ReadFromJsonAsync<JsonElement>();
            var secondAward = secondSpin.GetProperty("awardedPoints").GetInt32();
            Assert.Equal(100 + firstAward + secondAward, secondSpin.GetProperty("newPointsBalance").GetInt32());
        }
    }
}