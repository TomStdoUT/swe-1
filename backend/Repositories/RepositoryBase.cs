using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PollSystem.Data;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;

namespace PollSystem.Repositories
{
    public abstract class RepositoryBase<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity
    {
        protected readonly AppDbContext DbContext;
        protected readonly DbSet<TEntity> DbSet;

        protected RepositoryBase(AppDbContext dbContext)
        {
            DbContext = dbContext;
            DbSet = dbContext.Set<TEntity>();
        }

        public void Add(TEntity entity)
        {
            DbSet.Add(entity);
        }

        public void Remove(TEntity entity)
        {
            DbSet.Remove(entity);
        }

        public virtual async Task<TEntity?> FindByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await DbSet.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<TResult>> ProjectWhereAsync<TResult>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Where(predicate)
                .Select(selector)
                .ToListAsync(cancellationToken);
        }
    }
}