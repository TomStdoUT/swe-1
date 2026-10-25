using PollSystem.Models.Entities;

namespace PollSystem.Repositories.Interfaces
{
    public interface IPollRepository
    {
        IEnumerable<Poll> GetAll();
        Poll? GetById(Guid id);
        void Add(Poll poll);
        void AddVote(Vote vote);
        void AddComment(Comment comment);
        void Remove(Poll poll);
        Task SaveStateAsync();
    }
}
