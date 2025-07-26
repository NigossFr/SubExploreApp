using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Repositories.Interfaces;
using System.Linq.Expressions;
using SubExplore.Constants;

namespace SubExplore.Repositories.Implementations
{
    /// <summary>
    /// Generic repository implementation providing common CRUD operations with optimized async patterns
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly SubExploreDbContext _context;
        protected readonly DbSet<T> _dbSet;

        /// <summary>
        /// Initializes a new instance of the GenericRepository
        /// </summary>
        /// <param name="context">The database context</param>
        public GenericRepository(SubExploreDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<T>();
        }

        /// <summary>
        /// Get an entity by its identifier
        /// </summary>
        /// <param name="id">The entity identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The entity or null if not found</returns>
        public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get all entities (use with caution for large datasets)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>All entities</returns>
        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Find entities matching the specified predicate
        /// </summary>
        /// <param name="predicate">The search predicate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Entities matching the predicate</returns>
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Add a new entity
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            await _dbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing entity
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>Completed task</returns>
        public virtual Task UpdateAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Delete an entity
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        /// <returns>Completed task</returns>
        public virtual Task DeleteAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Check if any entity matches the specified predicate
        /// </summary>
        /// <param name="predicate">The search predicate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if any entity matches the predicate</returns>
        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().AnyAsync(predicate, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Save all changes to the database with optimized timeout settings
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of affected records</returns>
        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Set command timeout for long-running operations
            var originalTimeout = _context.Database.GetCommandTimeout();
            try
            {
                _context.Database.SetCommandTimeout(TimeSpan.FromSeconds(AppConstants.Database.COMMAND_TIMEOUT_SECONDS));
                return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _context.Database.SetCommandTimeout(originalTimeout);
            }
        }
        /// <summary>
        /// Get entities with paging support for efficient data retrieval
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
        /// <returns>Paged entities without change tracking for read-only scenarios</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when pageNumber or pageSize is less than 1</exception>
        public virtual async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be 1 or greater");
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be 1 or greater");
            
            return await _dbSet
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get entities with paging and filtering support for complex queries
        /// </summary>
        /// <param name="predicate">Filter predicate expression</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
        /// <returns>Filtered and paged entities without change tracking</returns>
        /// <exception cref="ArgumentNullException">Thrown when predicate is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when pageNumber or pageSize is less than 1</exception>
        public virtual async Task<IEnumerable<T>> GetPagedAsync(
            Expression<Func<T, bool>> predicate, 
            int pageNumber, 
            int pageSize, 
            CancellationToken cancellationToken = default)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be 1 or greater");
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be 1 or greater");
            
            return await _dbSet
                .AsNoTracking()
                .Where(predicate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get count of entities matching predicate for pagination and metrics
        /// </summary>
        /// <param name="predicate">Optional filter predicate; if null, counts all entities</param>
        /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
        /// <returns>Count of matching entities using optimized counting query</returns>
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
                return await _dbSet.AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
            
            return await _dbSet.AsNoTracking().CountAsync(predicate, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Add multiple entities in batch for improved performance
        /// </summary>
        /// <param name="entities">Collection of entities to add</param>
        /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
        /// <exception cref="ArgumentNullException">Thrown when entities collection is null</exception>
        public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            
            await _dbSet.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Remove multiple entities in batch for improved performance
        /// </summary>
        /// <param name="entities">Collection of entities to remove</param>
        /// <exception cref="ArgumentNullException">Thrown when entities collection is null</exception>
        public virtual void RemoveRange(IEnumerable<T> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            
            _dbSet.RemoveRange(entities);
        }
    }
}
