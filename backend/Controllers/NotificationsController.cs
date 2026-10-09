using Microsoft.AspNetCore.Mvc;
using PollSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var username = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _userService.GetUser(username);
            
            if (user == null)
                return Unauthorized();

            await _pollService.ResolveClosedPollsAsync();

            var notifications = _notificationService.GetUserNotifications(user.Id);
            return Ok(notifications);
        }
    }
}