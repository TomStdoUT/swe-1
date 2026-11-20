using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PollSystem.Services.Interfaces;

namespace PollSystem.Controllers
{
    [ApiController]
    [Route("api/roulette")]
    [Authorize]
    public sealed class DailyRouletteController : ControllerBase
    {
        private readonly IDailyRouletteService _rouletteService;

        public DailyRouletteController(IDailyRouletteService rouletteService)
        {
            _rouletteService = rouletteService;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var status = await _rouletteService.GetStatusAsync(userId, cancellationToken);
            return Ok(status);
        }

        [HttpPost("spin")]
        public async Task<IActionResult> Spin(CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            try
            {
                var result = await _rouletteService.SpinAsync(userId, cancellationToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private bool TryGetUserId(out Guid userId) =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}