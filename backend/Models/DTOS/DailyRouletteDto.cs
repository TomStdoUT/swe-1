namespace PollSystem.Models.DTOs
{
    public sealed record DailyRouletteStatusDto(
        bool CanSpin,
        DateTime? NextSpinAvailableAtUtc,
        int? LastAwardedPoints,
        IReadOnlyList<int> WheelSegments
    );

    public sealed record DailyRouletteSpinResultDto(
        int AwardedPoints,
        int WinningSegmentIndex,
        int NewPointsBalance,
        DateTime NextSpinAvailableAtUtc
    );
}