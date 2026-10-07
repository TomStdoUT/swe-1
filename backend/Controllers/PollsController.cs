using Microsoft.AspNetCore.Mvc;
using PollSystem.Models.DTOs;
using PollSystem.Services.Interfaces;

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

        [HttpGet]
        public IActionResult GetAllPolls([FromHeader(Name = "X-Username")] string? username)
        {
            var feed = _pollService.GetFeedForUser(username);
            return Ok(feed);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePoll(
            [FromHeader(Name = "X-Username")] string? username,
            [FromBody] PollCreateDto dto)
        {
            var user = _userService.GetUser(username);
            if (user == null) return Unauthorized();

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

        [HttpPost("{id}/vote")]
        public async Task<IActionResult> Vote(
            Guid id,
            [FromHeader(Name = "X-Username")] string? username,
            [FromBody] VoteDto dto)
        {
            var user = _userService.GetUser(username);
            if (user == null) return Unauthorized();

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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePoll(Guid id, [FromHeader(Name = "X-Username")] string? username)
        {
            var user = _userService.GetUser(username);
            if (user == null) return Unauthorized();

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

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(
            Guid id,
            [FromHeader(Name = "X-Username")] string? username,
            [FromBody] CommentCreateDto dto)
        {
            var user = _userService.GetUser(username);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { error = "Comment content cannot be empty." });
            }

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