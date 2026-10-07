namespace PollSystem.Models.Entities
{
    public class Comment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Comment() { }

        public Comment(Guid authorId, string authorName, string content)
        {
            if (string.IsNullOrWhiteSpace(authorName)) throw new ArgumentException("Author name cannot be empty.");
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Comment content cannot be empty.");

            AuthorId = authorId;
            AuthorName = authorName;
            Content = content;
        }
    }
}
