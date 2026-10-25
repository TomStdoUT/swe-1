namespace PollSystem.Data
{
    internal sealed class LegacyCommentData
    {
        public Guid Id { get; set; }
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
