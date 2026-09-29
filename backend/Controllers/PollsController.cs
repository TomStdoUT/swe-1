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
    
        return Ok(_manager.GetAllPolls());
    
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
            var poll = _manager.FirstOrDefault(p => p.Id == id);
            if (poll == null) return NotFound(new { error = "Poll not found." });

            try
            {
                var vote = new Vote(_currentUser.Id, dto.OptionIndices, dto.BettedPoints);
                
                poll.CastVote(_currentUser, vote);

                await _manager.SaveStateAsync();
                return Ok(new { message = "Vote accepted!", remainingPoints = _currentUser.PointsBalance });
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
    
    }
    public record VoteDto(List<int> OptionIndices, int BettedPoints);
}