using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Repositories.Interfaces;

namespace SubExplore.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for managing user favorite spots with optimized queries
    /// </summary>
    public class UserFavoriteSpotRepository : GenericRepository<UserFavoriteSpot>, IUserFavoriteSpotRepository
    {
        private readonly ILogger<UserFavoriteSpotRepository> _logger;
        
        /// <summary>
        /// Initializes a new instance of the UserFavoriteSpotRepository
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="logger">Logger instance</param>
        public UserFavoriteSpotRepository(SubExploreDbContext context, ILogger<UserFavoriteSpotRepository> logger) : base(context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Common query for loading favorite spots with related data
        /// </summary>
        private IQueryable<UserFavoriteSpot> GetBaseFavoritesQuery()
        {
            return _dbSet
                .AsNoTracking()
                .Include(f => f.Spot)
                .ThenInclude(s => s!.Type);
        }

        /// <summary>
        /// Query for loading favorite spots with media
        /// </summary>
        private IQueryable<UserFavoriteSpot> GetFavoritesWithMediaQuery()
        {
            return _dbSet
                .AsNoTracking()
                .Include(f => f.Spot)
                .ThenInclude(s => s!.Type)
                .Include(f => f.Spot)
                .ThenInclude(s => s!.Media.Where(m => m.IsPrimary));
        }

        /// <summary>
        /// Validate user and spot IDs
        /// </summary>
        private static void ValidateIds(int userId, int spotId)
        {
            if (userId <= 0) throw new ArgumentException("User ID must be positive", nameof(userId));
            if (spotId <= 0) throw new ArgumentException("Spot ID must be positive", nameof(spotId));
        }

        /// <summary>
        /// Get all favorite spots for a specific user with spot details
        /// </summary>
        public async Task<IEnumerable<UserFavoriteSpot>> GetUserFavoritesAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0) throw new ArgumentException("User ID must be positive", nameof(userId));
            
            try
            {
                var favorites = await GetFavoritesWithMediaQuery()
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.CreatedAt)
                    .ThenBy(f => f.Priority)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug("Retrieved {Count} favorites for user {UserId}", favorites.Count, userId);
                return favorites;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorites for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get user favorites ordered by priority and creation date
        /// </summary>
        public async Task<IEnumerable<UserFavoriteSpot>> GetUserFavoritesByPriorityAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0) throw new ArgumentException("User ID must be positive", nameof(userId));
            
            try
            {
                var favorites = await GetBaseFavoritesQuery()
                    .Where(f => f.UserId == userId)
                    .OrderBy(f => f.Priority)
                    .ThenByDescending(f => f.CreatedAt)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug("Retrieved {Count} favorites by priority for user {UserId}", favorites.Count, userId);
                return favorites;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorites by priority for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Check if a specific spot is favorited by a user
        /// </summary>
        public async Task<bool> IsSpotFavoritedAsync(int userId, int spotId, CancellationToken cancellationToken = default)
        {
            ValidateIds(userId, spotId);
            
            try
            {
                var isFavorited = await _dbSet
                    .AsNoTracking()
                    .AnyAsync(f => f.UserId == userId && f.SpotId == spotId, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug("Spot {SpotId} favorited status for user {UserId}: {IsFavorited}", spotId, userId, isFavorited);
                return isFavorited;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking favorite status for user {UserId} and spot {SpotId}", userId, spotId);
                throw;
            }
        }

        /// <summary>
        /// Get the favorite relationship between a user and spot
        /// </summary>
        public async Task<UserFavoriteSpot?> GetUserFavoriteAsync(int userId, int spotId, CancellationToken cancellationToken = default)
        {
            ValidateIds(userId, spotId);
            
            try
            {
                var favorite = await GetBaseFavoritesQuery()
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.SpotId == spotId, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug("Retrieved favorite relationship for user {UserId} and spot {SpotId}: {Found}", 
                    userId, spotId, favorite != null);
                return favorite;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorite for user {UserId} and spot {SpotId}", userId, spotId);
                throw;
            }
        }

        /// <summary>
        /// Get users who have favorited a specific spot
        /// </summary>
        public async Task<IEnumerable<User>> GetSpotFavoritersAsync(int spotId, CancellationToken cancellationToken = default)
        {
            if (spotId <= 0) throw new ArgumentException("Spot ID must be positive", nameof(spotId));
            
            try
            {
                var favoriters = await _dbSet
                    .AsNoTracking()
                    .Include(f => f.User)
                    .Where(f => f.SpotId == spotId)
                    .Select(f => f.User!)
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug("Retrieved {Count} favoriters for spot {SpotId}", favoriters.Count, spotId);
                return favoriters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favoriters for spot {SpotId}", spotId);
                throw;
            }
        }

        /// <summary>
        /// Get count of users who favorited a specific spot
        /// </summary>
        public async Task<int> GetSpotFavoritesCountAsync(int spotId, CancellationToken cancellationToken = default)
        {
            if (spotId <= 0) throw new ArgumentException("Spot ID must be positive", nameof(spotId));
            
            try
            {
                var count = await _dbSet
                    .AsNoTracking()
                    .CountAsync(f => f.SpotId == spotId, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug("Spot {SpotId} has {Count} favorites", spotId, count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting favorites for spot {SpotId}", spotId);
                throw;
            }
        }

        /// <summary>
        /// Get user's favorites with notification enabled
        /// </summary>
        public async Task<IEnumerable<UserFavoriteSpot>> GetUserNotificationFavoritesAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0) throw new ArgumentException("User ID must be positive", nameof(userId));
            
            try
            {
                var notificationFavorites = await GetBaseFavoritesQuery()
                    .Where(f => f.UserId == userId && f.NotificationEnabled)
                    .OrderBy(f => f.Priority)
                    .ThenByDescending(f => f.CreatedAt)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug("Retrieved {Count} notification-enabled favorites for user {UserId}", 
                    notificationFavorites.Count, userId);
                return notificationFavorites;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification favorites for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Remove a favorite spot relationship
        /// </summary>
        public async Task<bool> RemoveFavoriteAsync(int userId, int spotId, CancellationToken cancellationToken = default)
        {
            ValidateIds(userId, spotId);
            
            try
            {
                var favorite = await _dbSet
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.SpotId == spotId, cancellationToken)
                    .ConfigureAwait(false);

                if (favorite == null)
                {
                    _logger.LogWarning("Attempted to remove non-existent favorite for user {UserId} and spot {SpotId}", userId, spotId);
                    return false;
                }

                _dbSet.Remove(favorite);
                _logger.LogInformation("Removed favorite spot {SpotId} for user {UserId}", spotId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing favorite for user {UserId} and spot {SpotId}", userId, spotId);
                throw;
            }
        }

        /// <summary>
        /// Update favorite spot priority
        /// </summary>
        public async Task<bool> UpdateFavoritePriorityAsync(int userId, int spotId, int priority, CancellationToken cancellationToken = default)
        {
            ValidateIds(userId, spotId);
            if (priority < 1 || priority > 10) throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be between 1 and 10");
            
            try
            {
                var favorite = await _dbSet
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.SpotId == spotId, cancellationToken)
                    .ConfigureAwait(false);

                if (favorite == null)
                {
                    _logger.LogWarning("Attempted to update priority for non-existent favorite: user {UserId}, spot {SpotId}", userId, spotId);
                    return false;
                }

                var oldPriority = favorite.Priority;
                favorite.Priority = priority;
                favorite.UpdatedAt = DateTime.UtcNow;
                _context.Entry(favorite).State = EntityState.Modified;
                
                _logger.LogInformation("Updated priority for favorite spot {SpotId} (user {UserId}) from {OldPriority} to {NewPriority}", 
                    spotId, userId, oldPriority, priority);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating priority for user {UserId} and spot {SpotId}", userId, spotId);
                throw;
            }
        }

        /// <summary>
        /// Toggle notification settings for a favorite spot
        /// </summary>
        public async Task<bool> UpdateFavoriteNotificationAsync(int userId, int spotId, bool enabled, CancellationToken cancellationToken = default)
        {
            ValidateIds(userId, spotId);
            
            try
            {
                var favorite = await _dbSet
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.SpotId == spotId, cancellationToken)
                    .ConfigureAwait(false);

                if (favorite == null)
                {
                    _logger.LogWarning("Attempted to update notification for non-existent favorite: user {UserId}, spot {SpotId}", userId, spotId);
                    return false;
                }

                var oldValue = favorite.NotificationEnabled;
                favorite.NotificationEnabled = enabled;
                favorite.UpdatedAt = DateTime.UtcNow;
                _context.Entry(favorite).State = EntityState.Modified;
                
                _logger.LogInformation("Updated notification for favorite spot {SpotId} (user {UserId}) from {OldValue} to {NewValue}", 
                    spotId, userId, oldValue, enabled);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification for user {UserId} and spot {SpotId}", userId, spotId);
                throw;
            }
        }
    }
}