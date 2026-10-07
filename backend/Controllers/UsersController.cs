using Microsoft.AspNetCore.Mvc;
using PollSystem.Models.DTOs;
using PollSystem.Services.Interfaces;

namespace PollSystem.Controllers
{
    [ApiController]
    [Route("api")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Trim().Length > 40)
            {
                return BadRequest(new { error = "Enter a username of 1 to 40 characters." });
            }

            var user = await _userService.LoginAsync(dto.Username);
            var profile = _userService.GetUserProfile(user.Username);
            
            return Ok(profile);
        }

        [HttpGet("user")]
        public IActionResult GetUser([FromHeader(Name = "X-Username")] string? username)
        {
            var profile = _userService.GetUserProfile(username);
            return profile is null ? Unauthorized() : Ok(profile);
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
            return profile is null ? NotFound() : Ok(profile);
        }
    }
}