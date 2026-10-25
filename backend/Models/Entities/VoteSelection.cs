namespace PollSystem.Models.Entities
{
    public class VoteSelection
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid VoteId { get; set; }
        public int OptionIndex { get; set; }
        public int Position { get; set; }
    }
}
