using LGC.PIT.Plugin.Common.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LGC.PIT.Plugin.Common.Database.Repositories;

public interface IRepository<TKey, TEntity> where TEntity : BaseEntity<TKey> where TKey : struct
{
    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    System.Threading.Tasks.Task<TEntity> GetByIdAsync(TKey id);
    System.Threading.Tasks.Task<IReadOnlyList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate = null);
    System.Threading.Tasks.Task AddAsync(TEntity entity);
    System.Threading.Tasks.Task UpdateAsync(TEntity entity);
    System.Threading.Tasks.Task DeleteAsync(TEntity entity);
}
