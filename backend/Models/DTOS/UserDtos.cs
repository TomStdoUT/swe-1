namespace PollSystem.Models.DTOs
{
    public record LoginDto(string Username, string Password);
    public record RegisterDto(string Username, string Password);
    
    public record UserProfileDto(
        Guid Id,
        string Username,
        int PointsBalance,
        int CorrectPredictions,
        int TotalResolvedVotes,
        double? AccuracyRate,
        int FollowerCount,
        int FollowingCount,
        List<string> PurchasedCosmetics
    );

    public record FollowingVoteDto(
        Guid PollId,
        string Question,
        string Category,
        string Username,
        List<string> Selections,
        bool WasCorrect,
        DateTime PollClosedAt
    );
}
