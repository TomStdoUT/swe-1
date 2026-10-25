namespace PollSystem.Models.Entities
{
    public class UserCosmetic
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
