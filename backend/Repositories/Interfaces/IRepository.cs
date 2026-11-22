using System.Linq.Expressions;
using PollSystem.Models.Entities;

namespace PollSystem.Repositories.Interfaces
{
    public interface IRepository<TEntity> where TEntity : class, IEntity
    {
        void Add(TEntity entity);
        void Remove(TEntity entity);
        Task<TEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TResult>> ProjectWhereAsync<TResult>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector,
            CancellationToken cancellationToken = default);
    }
}