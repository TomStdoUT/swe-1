using Microsoft.AspNetCore.Mvc;
using PollSystem.Domain;

namespace swe_1.Controllers
{
    [ApiController]
    [Route("api")] // Base route is just /api for these specific endpoints
    public class UsersController : ControllerBase
    {
        private readonly User _currentUser;

        // The Singleton User is injected automatically by Program.cs
        public UsersController(User currentUser)
        {
            _currentUser = currentUser;
        }

        [HttpPost("login")] // Maps to /api/login
        public IActionResult Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Trim().Length > 40)
            {
                return BadRequest(new { error = "Enter a username of 1 to 40 characters." });
            }

            _currentUser.Username = dto.Username.Trim();
            return Ok(new { _currentUser.Username, _currentUser.PointsBalance });
        }

        [HttpGet("user")] // Maps to /api/user
        public IActionResult GetUser()
        {
            return Ok(new { _currentUser.Username, _currentUser.PointsBalance });
        }
    }
}