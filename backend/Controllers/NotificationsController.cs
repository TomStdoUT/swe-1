using Microsoft.AspNetCore.Mvc;
using PollSystem.Domain;

namespace swe_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationManager _manager;
        private readonly UserManager _users;

        public NotificationsController(NotificationManager manager, UserManager users)
        {
            _manager = manager;
            _users = users;
        }

        [HttpGet]
        public IActionResult GetNotifications([FromHeader(Name = "X-Username")] string? username)
        {
            // 1. Authenticate the user requesting their notifications
            var currentUser = _users.Find(username);
            if (currentUser is null) return Unauthorized();

            // 2. Return only the notifications targeting their specific ID
            return Ok(_manager.GetUserNotifications(currentUser.Id));
        }
    }
}