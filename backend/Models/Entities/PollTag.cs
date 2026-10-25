namespace PollSystem.Models.Entities
{
    public class PollTag
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PollId { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}
