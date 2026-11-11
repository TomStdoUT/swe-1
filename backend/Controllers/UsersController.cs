using Microsoft.AspNetCore.Mvc;
using PollSystem.Models.DTOs;
using PollSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PollSystem.Controllers
{
    [ApiController]
    [Route("api")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IPollService _pollService;

        public UsersController(IUserService userService, IPollService pollService)
        {
            _userService = userService;
            _pollService = pollService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var (user, token) = await _userService.RegisterAsync(dto.Username, dto.Password);
                var profile = await _userService.GetUserProfileAsync(user.Id);

                return Ok(new { Profile = profile, Token = token });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var (user, token) = await _userService.LoginAsync(dto.Username, dto.Password);
                var profile = await _userService.GetUserProfileAsync(user.Id);

                return Ok(new { Profile = profile, Token = token });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("user")]
        public async Task<IActionResult> GetUser()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var profile = await _userService.GetUserProfileAsync(userId);

            if (profile == null)
                return Unauthorized();

            return Ok(profile);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetProfiles()
        {
            var profiles = await _userService.GetAllUserProfilesAsync();
            return Ok(profiles);
        }

        [HttpGet("users/{id:guid}")]
        public async Task<IActionResult> GetProfile(Guid id)
        {
            var profile = await _userService.GetUserProfileAsync(id);

            if (profile == null)
                return NotFound();

            return Ok(profile);
        }

        [Authorize]
        [HttpPost("users/{id:guid}/follow")]
        public async Task<IActionResult> FollowUser(Guid id)
        {
            if (!TryGetUserId(out var followerId))
                return Unauthorized();

            try
            {
                await _userService.FollowUserAsync(followerId, id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "User not found." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("users/{id:guid}/follow")]
        public async Task<IActionResult> UnfollowUser(Guid id)
        {
            if (!TryGetUserId(out var followerId))
                return Unauthorized();

            try
            {
                await _userService.UnfollowUserAsync(followerId, id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "User not found." });
            }
        }

        [Authorize]
        [HttpPost("users/buy/{item}")]
        public async Task<IActionResult> BuyCosmetic(string item)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            try
            {
                var result = await _userService.BuyCosmeticAsync(userId, item);

                return result.Map<IActionResult>(
                    data => Ok(new
                    {
                        message = "Purchased successfully!",
                        remainingPoints = data.RemainingPoints,
                        purchasedCosmetics = data.Cosmetics
                    }),
                    error => BadRequest(new { error })
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("users/following")]
        public async Task<IActionResult> GetFollowing()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var following = await _userService.GetFollowingProfilesAsync(userId);
            return Ok(following);
        }

        [Authorize]
        [HttpGet("users/following/votes")]
        public async Task<IActionResult> GetFollowingVotes()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var votes = await _userService.GetFollowingVotesAsync(userId);
            return Ok(votes);
        }

        private bool TryGetUserId(out Guid userId) =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}