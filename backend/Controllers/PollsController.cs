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
        public async Task<IActionResult> GetAllPolls()
        {
            await _pollService.EnsureDailyQuestionAsync();
            await _pollService.ResolveClosedPollsAsync();

            var username = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var feed = _pollService.GetFeedForUser(username);
            
            return Ok(feed);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePoll([FromBody] PollCreateDto dto)
        {
            var username = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _userService.GetUser(username);

            if (user == null)
                return Unauthorized();

            try
            {
                await _pollService.CreatePollAsync(user.Id, dto);
                return Ok(new { message = "Apklausa sukurta sėkmingai!", remainingPoints = user.PointsBalance });
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
            var username = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _userService.GetUser(username);

            if (user == null)
                return Unauthorized();

            try
            {
                var (remainingPoints, isEarlyBird) = await _pollService.CastVoteAsync(id, user, dto);
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
            var username = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _userService.GetUser(username);

            if (user == null)
                return Unauthorized();

            try
            {
                await _pollService.DeletePollAsync(id, user.Id);
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
            var username = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _userService.GetUser(username);

            if (user == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { error = "Comment content cannot be empty." });

            try
            {
                var comment = await _pollService.AddCommentAsync(
                    id,
                    user.Id,
                    user.Username,
                    dto.Content.Trim()
                );
                return Ok(comment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}