using PollSystem.Models.Entities;

namespace PollSystem.Extensions
{
    public static class UserExtensions
    {
        public static bool IsFollowing(this User user, Guid targetUserId)
        {
            return user.Following.Any(follow => follow.FollowedId == targetUserId);
        }
    }
}
