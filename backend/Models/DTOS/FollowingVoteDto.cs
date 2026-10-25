namespace PollSystem.Models.DTOs
{
    public sealed record FollowingVoteDto(
        Guid PollId,
        string Question,
        string Category,
        string Username,
        List<string> Selections,
        bool WasCorrect,
        DateTime PollClosedAt
    );
}
