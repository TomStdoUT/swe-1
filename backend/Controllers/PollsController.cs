using Microsoft.AspNetCore.Mvc;
using PollSystem.Domain;

namespace swe_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class PollsController : ControllerBase
    {
        private readonly PollManager _manager;
        private readonly UserManager _users;

        public PollsController(PollManager manager, UserManager users)
        {
            _manager = manager;
            _users = users;
        }

        [HttpGet]
        public IActionResult GetAllPolls()
        {
            var username = Request.Headers["X-Username"].ToString();
            var activeUserId = GetUserIdFromUsername(username);

            var polls = _manager.GetAllPolls();

            var userPolls = polls.Select(p => new
            {
                p.Id,
                p.CreatorId,
                p.Question,
                p.Category,
                p.Tags,
                p.Options,
                p.ClosingTime,
                p.CreatedAt,
                p.AllowMultiple,
                p.Comments,
                Votes = p.Votes.Where(v => v.UserId == activeUserId).ToList(),
                TotalVotesCount = p.Votes.Count()
            });

            return Ok(userPolls);
        }
        
 [HttpPost]
        public async Task<IActionResult> CreatePoll(
            [FromHeader(Name = "X-Username")] string? username,
            [FromBody] PollCreateDto dto
        )
        {
            var currentUser = _users.Find(username);
            if (currentUser is null) return Unauthorized();

            try 
            {
                // Revert to standard string mapping
                var pollOptions = dto.Options.Select(opt => new PollOption(opt)).ToList();

                _manager.CreatePoll(
                    currentUser.Id,
                    dto.Question,
                    dto.Category,
                    dto.Tags,
                    pollOptions, 
                    DateTime.UtcNow.AddSeconds(dto.DurationSeconds),
                    dto.AllowMultiple
                );
                
                await _manager.SaveStateAsync();
                
                return Ok(new {
                    message = "Apklausa sukurta sėkmingai!", 
                    remainingPoints = currentUser.PointsBalance
                });
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
            [FromBody] VoteDto dto
        )
        {
            var currentUser = _users.Find(username);
            if (currentUser is null) return Unauthorized();

            var poll = _manager.FirstOrDefault(p => p.Id == id);
            if (poll == null) return NotFound(new { error = "Poll not found." });

            try
            {
                var vote = new Vote(currentUser.Id, dto.OptionIndices, dto.BettedPoints);
                
                poll.CastVote(currentUser, vote);

                await _manager.SaveStateAsync();
                await _users.SaveStateAsync();
                return Ok(new { message = "Vote accepted!", remainingPoints = currentUser.PointsBalance });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePoll(
            Guid id,
            [FromHeader(Name = "X-Username")] string? username
        )
        {
            var currentUser = _users.Find(username);
            if (currentUser is null) return Unauthorized();

            var poll = _manager.FirstOrDefault(p => p.Id == id);
            if (poll is null)
            {
                return NotFound(new { error = "Apklausa nerasta arba jau ištrinta." });
            }

            if (poll.CreatorId != currentUser.Id) return Forbid();

            await _manager.DeletePollAsync(id);
            return Ok(new { message = "Apklausa sėkmingai ištrinta." });
        }
    
        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(
            Guid id, 
            [FromHeader(Name = "X-Username")] string? username, 
            [FromBody] CommentCreateDto dto)
        {
            var currentUser = _users.Find(username);
            if (currentUser is null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { error = "Comment content cannot be empty." });
            }

            try
            {
                var comment = _manager.AddComment(id, currentUser.Id, currentUser.Username, dto.Content.Trim());
                await _manager.SaveStateAsync();
                return Ok(comment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
        private Guid GetUserIdFromUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return Guid.Empty;
            // Generate a deterministic Guid based on the username string so it's always the same per user
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(System.Text.Encoding.Default.GetBytes(username.Trim().ToLower()));
                return new Guid(hash);
            }
        }
    }

    
    public record VoteDto(List<int> OptionIndices, int BettedPoints);
    public record CommentCreateDto(string Content);
}
