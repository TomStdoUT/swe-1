using Microsoft.EntityFrameworkCore;
using PollSystem.Data;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;

namespace PollSystem.Repositories
{
    public sealed class PollRepository : IPollRepository
    {
        private readonly AppDbContext _db;

        public PollRepository(AppDbContext db)
        {
            _db = db;
        }

        public IEnumerable<Poll> GetAll() => QueryPolls().ToList();

        public Poll? GetById(Guid id) => QueryPolls().FirstOrDefault(poll => poll.Id == id);

        public void Add(Poll poll) => _db.Polls.Add(poll);

        public void Remove(Poll poll) => _db.Polls.Remove(poll);

        public Task SaveStateAsync() => _db.SaveChangesAsync();

        private IQueryable<Poll> QueryPolls() => _db.Polls
            .Include(poll => poll.Options)
            .Include(poll => poll.Tags)
            .Include(poll => poll.Votes)
                .ThenInclude(vote => vote.Selections)
            .Include(poll => poll.Comments)
            .AsSplitQuery();
    }
}
