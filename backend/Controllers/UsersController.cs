using Microsoft.AspNetCore.Mvc;
using PollSystem.Domain;

namespace swe_1.Controllers
{
    [ApiController]
    [Route("api")]
    public class UsersController : ControllerBase
    {
        private readonly User _currentUser;

        public UsersController(User currentUser)
        {
            _currentUser = currentUser;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Trim().Length > 40)
            {
                return BadRequest(new { error = "Enter a username of 1 to 40 characters." });
            }

            _currentUser.Username = dto.Username.Trim();
            return Ok(new { _currentUser.Username, _currentUser.PointsBalance });
        }

        [HttpGet("user")]
        public IActionResult GetUser()
        {
            return Ok(new { _currentUser.Username, _currentUser.PointsBalance });
        }
    }
}