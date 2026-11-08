namespace PollSystem.Models.DTOs
{
    public sealed record PollFeedDto(
        Guid Id,
        Guid CreatorId,
        string Question,
        string Category,
        List<string> Tags,
        List<string> Options,
        DateTime ClosingTime,
        DateTime CreatedAt,
        bool AllowMultiple,
        bool IsDailyChallenge,
        bool IsResolved,
        List<CommentDto> Comments,
        List<int> WinningOptions,
        List<UserVoteDto> Votes,
        int TotalVotesCount
    );

    public sealed record CommentDto(
        string AuthorName,
        string Content,
        DateTime CreatedAt
    );

    public sealed record UserVoteDto(
        List<int> OptionIndices,
        int BettedPoints,
        bool IsEarlyBird
    );
}