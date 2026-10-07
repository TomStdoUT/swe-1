using PollSystem.Models.DTOs;
using PollSystem.Models.Entities;

namespace PollSystem.Services.Interfaces
{
    public interface IPollService
    {
        IEnumerable<object> GetFeedForUser(string? username);
        Task<Poll> CreatePollAsync(Guid creatorId, PollCreateDto dto);
        Task<(int remainingPoints, bool isEarlyBird)> CastVoteAsync(Guid pollId, User user, VoteDto dto);        Task DeletePollAsync(Guid pollId, Guid requestingUserId);
        Task<Comment> AddCommentAsync(Guid pollId, Guid authorId, string authorName, string content);
    }
}