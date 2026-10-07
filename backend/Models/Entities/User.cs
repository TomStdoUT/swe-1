namespace PollSystem.Models.Entities
{
    public class User
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public int PointsBalance { get; set; } = 100;

        public User() { }

        public User(string username, int initialPoints = 100)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username cannot be empty.");
            Username = username;
            PointsBalance = initialPoints;
        }
    }
}