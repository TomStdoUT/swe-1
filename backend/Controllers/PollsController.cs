using Microsoft.AspNetCore.Mvc;
using PollSystem.Domain;

namespace swe_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Automatically maps to /api/polls
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
            if (dto.BettedPoints > _currentUser.PointsBalance)
            {
                return BadRequest(new { error = "Neturite pakankamai taškų!" });
            }

            _currentUser.PointsBalance -= dto.BettedPoints;

            _manager.CreatePoll(
                _currentUser.Id,
                dto.Question,
                dto.Category,
                dto.Tags,
                dto.Options.Select(opt => new PollOption(opt)).ToList(),
                DateTime.UtcNow.AddDays(1)
            );
            
            await _manager.SaveStateAsync();
            
            return Ok(new { 
                message = "Apklausa sukurta sėkmingai!", 
                remainingPoints = _currentUser.PointsBalance 
            });
        }

        [HttpPost("{id}/vote")]
        public async Task<IActionResult> Vote(Guid id, [FromBody] VoteDto dto)
        {
            var poll = _manager.FirstOrDefault(p => p.Id == id);
            if (poll == null)
            {
                return NotFound(new { error = "Apklausa nerasta." });
            }

            try
            {
        
                var vote = new Vote(_currentUser.Id, dto.OptionIndex, dto.BettedPoints);
                poll.CastVote(_currentUser, vote);
        
                await _manager.SaveStateAsync();
        
                return Ok(new { 
                    message = "Balsas priimtas!", 
                    remainingPoints = _currentUser.PointsBalance 
                });
            }
            catch (Exception ex)
            {
            return BadRequest(new { error = ex.Message });
            }
        }
    
    }
    public record VoteDto(int OptionIndex, int BettedPoints);
}