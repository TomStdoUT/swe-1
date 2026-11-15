using PollSystem.Models.Entities;

namespace PollSystem.Repositories.Interfaces
{
    public interface IPollRepository
    {
        Task<IEnumerable<Poll>> GetAllAsync();
        Task<IEnumerable<Poll>> GetUnresolvedClosedPollsAsync();
        Task<Poll?> GetByIdAsync(Guid id);
        Task<IEnumerable<Poll>> GetClosedPollsAsync();
        void Add(Poll poll);
        void AddVote(Vote vote);
        void AddComment(Comment comment);
        void Remove(Poll poll);
    }
}
