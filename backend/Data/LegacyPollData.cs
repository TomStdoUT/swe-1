namespace PollSystem.Data
{
    internal sealed class LegacyPollData
    {
        public Guid Id { get; set; }
        public Guid CreatorId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string>? Tags { get; set; }
        public List<LegacyPollOptionData>? Options { get; set; }
        public DateTime ClosingTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool AllowMultiple { get; set; }
        public bool IsDailyChallenge { get; set; }
        public bool NotificationsGenerated { get; set; }
        public bool IsResolved { get; set; }
        public List<LegacyCommentData>? Comments { get; set; }
        public List<LegacyVoteData>? Votes { get; set; }
        public List<LegacyVoteData>? _votes { get; set; }
    }
}
