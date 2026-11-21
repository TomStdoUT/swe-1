using PollSystem.Models.DTOs;

namespace PollSystem.Services.Interfaces
{
    public interface IDailyRouletteService
    {
        Task<DailyRouletteStatusDto> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<DailyRouletteSpinResultDto> SpinAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}