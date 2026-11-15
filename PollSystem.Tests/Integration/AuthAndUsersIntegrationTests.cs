using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace PollSystem.Tests.Integration
{
    public class AuthAndUsersIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthAndUsersIntegrationTests(CustomWebApplicationFactory factory)
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
        public async Task RegisterAndLogin_ValidAndInvalidCredentials_ReturnsExpectedStatusCodes()
        {
            var client = _factory.CreateClient();
            var username = $"auth_{Guid.NewGuid():N}"[..16];
            var password = "Password123";

            var registerResponse = await client.PostAsJsonAsync("/api/register", new { username, password });
            Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

            var duplicateResponse = await client.PostAsJsonAsync("/api/register", new { username, password });
            Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);

            var validLogin = await client.PostAsJsonAsync("/api/login", new { username, password });
            Assert.Equal(HttpStatusCode.OK, validLogin.StatusCode);

            var invalidLogin = await client.PostAsJsonAsync("/api/login", new { username, password = "WrongPassword" });
            Assert.Equal(HttpStatusCode.Unauthorized, invalidLogin.StatusCode);
        }

        [Fact]
        public async Task GetUser_WithoutAndWithToken_EnforcesJwtAuthentication()
        {
            var unauthenticatedClient = _factory.CreateClient();
            var unauthorizedResponse = await unauthenticatedClient.GetAsync("/api/user");
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);

            var (authenticatedClient, userId, username) = await RegisterAuthenticatedUserAsync("jwt");
            var profileResponse = await authenticatedClient.GetAsync("/api/user");
            Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

            var profile = await profileResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(userId, profile.GetProperty("id").GetGuid());
            Assert.Equal(username, profile.GetProperty("username").GetString());
            Assert.Equal(100, profile.GetProperty("pointsBalance").GetInt32());
        }

        [Fact]
        public async Task FollowAndUnfollow_ValidUsers_UpdatesFollowerCountsAndFollowingList()
        {
            var (followerClient, followerId, _) = await RegisterAuthenticatedUserAsync("flwr");
            var (_, targetId, targetUsername) = await RegisterAuthenticatedUserAsync("trgt");

            var selfFollowResponse = await followerClient.PostAsync($"/api/users/{followerId}/follow", null);
            Assert.Equal(HttpStatusCode.BadRequest, selfFollowResponse.StatusCode);

            var followResponse = await followerClient.PostAsync($"/api/users/{targetId}/follow", null);
            Assert.Equal(HttpStatusCode.NoContent, followResponse.StatusCode);

            var followingList = await followerClient.GetFromJsonAsync<JsonElement>("/api/users/following");
            Assert.Equal(1, followingList.GetArrayLength());
            Assert.Equal(targetUsername, followingList[0].GetProperty("username").GetString());

            var targetProfile = await followerClient.GetFromJsonAsync<JsonElement>($"/api/users/{targetId}");
            Assert.Equal(1, targetProfile.GetProperty("followerCount").GetInt32());

            var unfollowResponse = await followerClient.DeleteAsync($"/api/users/{targetId}/follow");
            Assert.Equal(HttpStatusCode.NoContent, unfollowResponse.StatusCode);

            var updatedTargetProfile = await followerClient.GetFromJsonAsync<JsonElement>($"/api/users/{targetId}");
            Assert.Equal(0, updatedTargetProfile.GetProperty("followerCount").GetInt32());
        }

        [Fact]
        public async Task BuyCosmetic_ValidAndDuplicatePurchases_UpdatesBalanceAndReturnsErrors()
        {
            var (client, _, _) = await RegisterAuthenticatedUserAsync("shop");

            var buyResponse = await client.PostAsync("/api/users/buy/gold_name", null);
            Assert.Equal(HttpStatusCode.OK, buyResponse.StatusCode);

            var buyPayload = await buyResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(80, buyPayload.GetProperty("remainingPoints").GetInt32());

            var duplicateBuyResponse = await client.PostAsync("/api/users/buy/gold_name", null);
            Assert.Equal(HttpStatusCode.BadRequest, duplicateBuyResponse.StatusCode);

            var unknownItemResponse = await client.PostAsync("/api/users/buy/invalid_item", null);
            Assert.Equal(HttpStatusCode.BadRequest, unknownItemResponse.StatusCode);

            var profile = await client.GetFromJsonAsync<JsonElement>("/api/user");
            Assert.Equal(80, profile.GetProperty("pointsBalance").GetInt32());
            Assert.Contains("gold_name", profile.GetProperty("purchasedCosmetics").ToString());
        }
    }
}