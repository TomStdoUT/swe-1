namespace PollSystem.Models.Entities
{
    public class PollOption
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PollId { get; set; }
        public int Position { get; set; }
        public string text { get; set; } = string.Empty;

        public PollOption() { }

        public PollOption(string text)
        {
            this.text = text;
        }
    }
}
