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
    
        return Ok(_manager.GetAllPolls());
    
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
    
    }
    public record VoteDto(List<int> OptionIndices, int BettedPoints);
}
