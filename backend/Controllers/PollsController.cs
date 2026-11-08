using Microsoft.AspNetCore.Mvc;
using PollSystem.Models.DTOs;
using PollSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PollSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PollsController : ControllerBase
    {
        private readonly IPollService _pollService;
        private readonly IUserService _userService;

        public PollsController(IPollService pollService, IUserService userService)
        {
            _pollService = pollService;
            _userService = userService;
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetAllPolls()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var feed = _pollService.GetFeedForUser(username);
            return Ok(feed);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePoll([FromBody] PollCreateDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            try
            {
                await _pollService.CreatePollAsync(userId, dto);
                var userProfile = _userService.GetUserProfile(userId);
                return Ok(new { message = "Apklausa sukurta sėkmingai!", remainingPoints = userProfile?.PointsBalance ?? 0 });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("{id}/vote")]
        public async Task<IActionResult> Vote(Guid id, [FromBody] VoteDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            try
            {
                var (remainingPoints, isEarlyBird) = await _pollService.CastVoteAsync(id, userId, dto);
                return Ok(new { message = "Vote accepted!", remainingPoints, isEarlyBird });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePoll(Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            try
            {
                await _pollService.DeletePollAsync(id, userId);
                return Ok(new { message = "Apklausa sėkmingai ištrinta." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Apklausa nerasta arba jau ištrinta." });
            }
        }

        [Authorize]
        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(Guid id, [FromBody] CommentCreateDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var username = User.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { error = "Comment content cannot be empty." });

            try
            {
                var comment = await _pollService.AddCommentAsync(id, userId, username ?? "Unknown", dto.Content.Trim());
                return Ok(comment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}