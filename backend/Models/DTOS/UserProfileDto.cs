namespace PollSystem.Models.DTOs
{
    public sealed record UserProfileDto(
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
}
