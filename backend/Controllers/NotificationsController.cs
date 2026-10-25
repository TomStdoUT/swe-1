using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PollSystem.Services.Interfaces;
using System.Security.Claims;

namespace PollSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;

        public NotificationsController(INotificationService notificationService, IUserService userService)
        {
            _notificationService = notificationService;
            _userService = userService;
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetNotifications()
        {
            var username = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _userService.GetUser(username);

            if (user == null)
                return Unauthorized();

            var notifications = _notificationService.GetUserNotifications(user.Id);
            return Ok(notifications);
        }
    }
}
