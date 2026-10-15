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
                var profile = _userService.GetUserProfile(user.Username);
                
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
                var profile = _userService.GetUserProfile(user.Username);
                
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
            var username = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = _userService.GetUserProfile(username);
            
            if (profile == null)
                return Unauthorized();
                
            return Ok(profile);
        }

        [HttpGet("users")]
        public IActionResult GetProfiles()
        {
            return Ok(_userService.GetAllUserProfiles());
        }

        [HttpGet("users/{username}")]
        public IActionResult GetProfile(string username)
        {
            var profile = _userService.GetUserProfile(username);
            
            if (profile == null)
                return NotFound();
                
            return Ok(profile);
        }

        [Authorize]
        [HttpPost("users/{username}/follow")]
        public async Task<IActionResult> FollowUser(string username)
        {
            try
            {
                await _userService.FollowUserAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), username);
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
        [HttpDelete("users/{username}/follow")]
        public async Task<IActionResult> UnfollowUser(string username)
        {
            try
            {
                await _userService.UnfollowUserAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), username);
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
        [HttpGet("users/following")]
        public IActionResult GetFollowing()
        {
            return Ok(_userService.GetFollowingProfiles(User.FindFirstValue(ClaimTypes.NameIdentifier)));
        }

        [Authorize]
        [HttpGet("users/following/votes")]
        public IActionResult GetFollowingVotes()
        {
            return Ok(_userService.GetFollowingVotes(User.FindFirstValue(ClaimTypes.NameIdentifier)));
        }
    }
}