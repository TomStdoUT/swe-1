namespace PollSystem.Models.Entities
{
    public class UserCosmetic : IEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
