using Microsoft.AspNetCore.Mvc;
using PollSystem.Domain;

namespace swe_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class PollsController : ControllerBase
    {
        private readonly PollManager _manager;
        private readonly User _currentUser;

        public PollsController(PollManager manager, User currentUser)
        {
            _manager = manager;
            _currentUser = currentUser;
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
        public async Task<IActionResult> CreatePoll([FromBody] PollCreateDto dto)
        {
            try 
            {
                // Revert to standard string mapping
                var pollOptions = dto.Options.Select(opt => new PollOption(opt)).ToList();

                _manager.CreatePoll(
                    _currentUser.Id,
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
                    remainingPoints = _currentUser.PointsBalance 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/vote")]
        public async Task<IActionResult> Vote(Guid id, [FromBody] VoteDto dto)
        {
            var username = Request.Headers["X-Username"].ToString();
            var userId = GetUserIdFromUsername(username);

            if (userId == Guid.Empty)
            {
                return BadRequest(new { error = "Invalid user session." });
            }

            var poll = _manager.FirstOrDefault(p => p.Id == id);
            if (poll == null) return NotFound(new { error = "Poll not found." });

            try
            {
                // Create a temporary user object with the correct Id and Username for casting
                var votingUser = new User(username, _currentUser.PointsBalance) { Id = userId };
                
                var vote = new Vote(userId, dto.OptionIndices, dto.BettedPoints);
                poll.CastVote(votingUser, vote);

                // Sync back points balance if needed
                _currentUser.PointsBalance = votingUser.PointsBalance;

                await _manager.SaveStateAsync();
                return Ok(new { message = "Vote accepted!", remainingPoints = votingUser.PointsBalance });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePoll(Guid id)
        {
            var success = await _manager.DeletePollAsync(id);
            
            if (!success)
            {
                return NotFound(new { error = "Apklausa nerasta arba jau ištrinta." });
            }

            return Ok(new { message = "Apklausa sėkmingai ištrinta." });
        }
    
        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(Guid id, [FromBody] CommentCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { error = "Comment content cannot be empty." });
            }

            try
            {
                var comment = _manager.AddComment(id, _currentUser.Id, _currentUser.Username, dto.Content.Trim());
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