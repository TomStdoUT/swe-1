namespace PollSystem.Models.Entities
{
    public class DailyRouletteSpin : IEntity
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid UserId { get; init; }
        public int AwardedPoints { get; set; }
        public DateTime SpunAtUtc { get; init; } = DateTime.UtcNow;

        public DailyRouletteSpin() { }

        public DailyRouletteSpin(Guid userId, int awardedPoints, DateTime spunAtUtc)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            if (awardedPoints <= 0)
                throw new ArgumentOutOfRangeException(nameof(awardedPoints), "Awarded points must be positive.");

            UserId = userId;
            AwardedPoints = awardedPoints;
            SpunAtUtc = spunAtUtc;
        }
    }
}