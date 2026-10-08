using Microsoft.AspNetCore.Mvc;
using PollSystem.Services.Interfaces;

namespace PollSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly IPollService _pollService;

        public NotificationsController(
            INotificationService notificationService, 
            IUserService userService, 
            IPollService pollService)
        {
            _notificationService = notificationService;
            _userService = userService;
            _pollService = pollService;
        }

        [HttpGet]
        public IActionResult GetNotifications([FromHeader(Name = "X-Username")] string? username)
        {
            var user = _userService.GetUser(username);
            if (user == null) return Unauthorized();

            _pollService.GetFeedForUser(username);

            var notifications = _notificationService.GetUserNotifications(user.Id);
            return Ok(notifications);
        }
    }
}