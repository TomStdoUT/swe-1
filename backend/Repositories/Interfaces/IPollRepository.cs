using PollSystem.Models.Entities;

namespace PollSystem.Repositories.Interfaces
{
    public interface IPollRepository : IRepository<Poll>
    {
        Task<IEnumerable<Poll>> GetAllAsync();
        Task<IEnumerable<Poll>> GetUnresolvedClosedPollsAsync();
        Task<Poll?> GetByIdAsync(Guid id);
        Task<IEnumerable<Poll>> GetClosedPollsAsync();
        void AddVote(Vote vote);
        void AddComment(Comment comment);
    }
}