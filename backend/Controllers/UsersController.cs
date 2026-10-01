using Microsoft.AspNetCore.Mvc;
using PollSystem.Domain;

namespace swe_1.Controllers
{
    [ApiController]
    [Route("api")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager _users;
        private readonly PollManager _polls;

        public UsersController(UserManager users, PollManager polls)
        {
            _users = users;
            _polls = polls;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Trim().Length > 40)
            {
                return BadRequest(new { error = "Enter a username of 1 to 40 characters." });
            }

            var user = _users.GetOrCreate(dto.Username);
            await _users.SaveStateAsync();
            return Ok(CreateProfile(user));
        }

        [HttpGet("user")]
        public IActionResult GetUser([FromHeader(Name = "X-Username")] string? username)
        {
            var user = _users.Find(username);
            return user is null ? Unauthorized() : Ok(CreateProfile(user));
        }

        [HttpGet("users")]
        public IActionResult GetProfiles()
        {
            return Ok(_users.GetAll().Select(CreateProfile));
        }

        [HttpGet("users/{username}")]
        public IActionResult GetProfile(string username)
        {
            var user = _users.Find(username);
            return user is null ? NotFound() : Ok(CreateProfile(user));
        }

        private UserProfileDto CreateProfile(User user)
        {
            var (correctPredictions, totalResolvedVotes) = _polls.GetAccuracyStats(user.Id);
            var accuracyRate = totalResolvedVotes == 0
                ? (double?)null
                : Math.Round(correctPredictions * 100.0 / totalResolvedVotes, 1);

            return new UserProfileDto(
                user.Id,
                user.Username,
                user.PointsBalance,
                correctPredictions,
                totalResolvedVotes,
                accuracyRate
            );
        }
    }

    public record UserProfileDto(
        Guid Id,
        string Username,
        int PointsBalance,
        int CorrectPredictions,
        int TotalResolvedVotes,
        double? AccuracyRate
    );
}
