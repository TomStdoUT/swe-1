namespace PollSystem.Models.DTOs
{
    public record LoginDto(string Username, string? Password = null);
    
    public record UserProfileDto(
        Guid Id,
        string Username,
        int PointsBalance,
        int CorrectPredictions,
        int TotalResolvedVotes,
        double? AccuracyRate
    );
}