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
            if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Trim().Length > 40)
                return BadRequest(new { error = "Enter a valid username (1-40 characters)." });

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return BadRequest(new { error = "Password must be at least 6 characters long." });

            try
            {
                var (user, token) = await _userService.RegisterAsync(dto.Username, dto.Password);
                var profile = _userService.GetUserProfile(user.Id);
                
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
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { error = "Username and password are required." });

            try
            {
                var (user, token) = await _userService.LoginAsync(dto.Username, dto.Password);
                var profile = _userService.GetUserProfile(user.Id);
                
                return Ok(new { Profile = profile, Token = token });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("user")]
        public IActionResult GetUser()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var profile = _userService.GetUserProfile(userId);
            
            if (profile == null)
                return Unauthorized();
                
            return Ok(profile);
        }

        [HttpGet("users")]
        public IActionResult GetProfiles()
        {
            return Ok(_userService.GetAllUserProfiles());
        }

        [HttpGet("users/{id:guid}")]
        public IActionResult GetProfile(Guid id)
        {
            var profile = _userService.GetUserProfile(id);
            
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
                
                return Ok(new { 
                    message = "Purchased successfully!", 
                    remainingPoints = result.RemainingPoints,
                    purchasedCosmetics = result.Cosmetics 
                });
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
        public IActionResult GetFollowing()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            return Ok(_userService.GetFollowingProfiles(userId));
        }

        [Authorize]
        [HttpGet("users/following/votes")]
        public IActionResult GetFollowingVotes()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            return Ok(_userService.GetFollowingVotes(userId));
        }

        private bool TryGetUserId(out Guid userId) =>
            Guid.TryParse(User.FindFirstValue("userId"), out userId);
    }
}
