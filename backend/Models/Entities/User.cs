namespace PollSystem.Models.Entities
{
    public class User
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int PointsBalance { get; set; } = 100;
        public List<Guid> FollowingUserIds { get; set; } = new();
        public List<string> PurchasedCosmetics { get; set; } = new();

        public User() { }

        public User(string username, string passwordHash, int initialPoints = 100)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty.");

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash cannot be empty.");

            Username = username;
            PasswordHash = passwordHash;
            PointsBalance = initialPoints;
        }
    }
}
