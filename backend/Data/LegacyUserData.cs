namespace PollSystem.Data
{
    internal sealed class LegacyUserData
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int PointsBalance { get; set; }
        public List<Guid>? FollowingUserIds { get; set; }
        public List<string>? PurchasedCosmetics { get; set; }
    }
}
