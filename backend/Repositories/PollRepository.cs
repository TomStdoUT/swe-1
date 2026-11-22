using Microsoft.EntityFrameworkCore;
using PollSystem.Data;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;

namespace PollSystem.Repositories
{
    public sealed class PollRepository : RepositoryBase<Poll>, IPollRepository
    {
        public PollRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<Poll>> GetAllAsync() => await QueryPolls().ToListAsync();

        public async Task<IEnumerable<Poll>> GetClosedPollsAsync()
        {
            return await QueryPolls()
                .Where(p => p.ClosingTime <= DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<IEnumerable<Poll>> GetUnresolvedClosedPollsAsync()
        {
            return await DbContext.Polls
                .Include(p => p.Votes).ThenInclude(v => v.Selections)
                .Where(p => p.ClosingTime <= DateTime.UtcNow && !p.IsResolved)
                .ToListAsync();
        }

        public async Task<Poll?> GetByIdAsync(Guid id) => await QueryPolls().FirstOrDefaultAsync(poll => poll.Id == id);

        public override async Task<Poll?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            await QueryPolls().FirstOrDefaultAsync(poll => poll.Id == id, cancellationToken);

        public void AddVote(Vote vote) => DbContext.Votes.Add(vote);

        public void AddComment(Comment comment) => DbContext.Comments.Add(comment);

        private IQueryable<Poll> QueryPolls() => DbContext.Polls
            .Include(poll => poll.Options)
            .Include(poll => poll.Tags)
            .Include(poll => poll.Votes)
                .ThenInclude(vote => vote.Selections)
            .Include(poll => poll.Comments)
            .AsSplitQuery();
    }
}