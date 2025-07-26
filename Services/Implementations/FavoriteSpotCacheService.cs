using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Caching;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Cache service implementation for favorite spots with intelligent cache management
    /// </summary>
    public class FavoriteSpotCacheService : IFavoriteSpotCacheService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<FavoriteSpotCacheService> _logger;
        
        // Cache key patterns
        private const string UserFavoritesKey = "user_favorites_{0}_{1}"; // userId, byPriority
        private const string FavoriteStatusKey = "favorite_status_{0}_{1}"; // userId, spotId
        private const string FavoriteStatsKey = "favorite_stats_{0}"; // userId
        private const string SpotFavoritesCountKey = "spot_favorites_count_{0}"; // spotId
        
        // Cache expiration times
        private static readonly TimeSpan DefaultCacheExpiry = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan StatsCacheExpiry = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan StatusCacheExpiry = TimeSpan.FromMinutes(15);

        public FavoriteSpotCacheService(
            ICacheService cacheService,
            ILogger<FavoriteSpotCacheService> logger)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get cached user favorites
        /// </summary>
        public async Task<IEnumerable<UserFavoriteSpot>?> GetCachedUserFavoritesAsync(int userId, bool byPriority = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = string.Format(UserFavoritesKey, userId, byPriority);
                var cached = await _cacheService.GetAsync<List<UserFavoriteSpot>>(key).ConfigureAwait(false);
                
                if (cached != null)
                {
                    _logger.LogDebug("Cache hit for user {UserId} favorites (byPriority: {ByPriority})", userId, byPriority);
                    return cached;
                }

                _logger.LogDebug("Cache miss for user {UserId} favorites (byPriority: {ByPriority})", userId, byPriority);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving cached favorites for user {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Cache user favorites
        /// </summary>
        public async Task SetUserFavoritesCacheAsync(int userId, IEnumerable<UserFavoriteSpot> favorites, bool byPriority = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = string.Format(UserFavoritesKey, userId, byPriority);
                var favoritesList = favorites.ToList();
                
                await _cacheService.SetAsync(key, favoritesList, DefaultCacheExpiry).ConfigureAwait(false);
                
                _logger.LogDebug("Cached {Count} favorites for user {UserId} (byPriority: {ByPriority})", 
                    favoritesList.Count, userId, byPriority);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error caching favorites for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Get cached favorite status for a spot
        /// </summary>
        public async Task<bool?> GetCachedFavoriteStatusAsync(int userId, int spotId, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = string.Format(FavoriteStatusKey, userId, spotId);
                var cached = await _cacheService.GetAsync<bool?>(key).ConfigureAwait(false);
                
                if (cached.HasValue)
                {
                    _logger.LogDebug("Cache hit for favorite status: user {UserId}, spot {SpotId}", userId, spotId);
                    return cached.Value;
                }

                _logger.LogDebug("Cache miss for favorite status: user {UserId}, spot {SpotId}", userId, spotId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving cached favorite status for user {UserId}, spot {SpotId}", userId, spotId);
                return null;
            }
        }

        /// <summary>
        /// Cache favorite status for a spot
        /// </summary>
        public async Task SetFavoriteStatusCacheAsync(int userId, int spotId, bool isFavorite, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = string.Format(FavoriteStatusKey, userId, spotId);
                await _cacheService.SetAsync(key, isFavorite, StatusCacheExpiry).ConfigureAwait(false);
                
                _logger.LogDebug("Cached favorite status for user {UserId}, spot {SpotId}: {IsFavorite}", 
                    userId, spotId, isFavorite);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error caching favorite status for user {UserId}, spot {SpotId}", userId, spotId);
            }
        }

        /// <summary>
        /// Get cached favorite stats for a user
        /// </summary>
        public async Task<FavoriteSpotStats?> GetCachedFavoriteStatsAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = string.Format(FavoriteStatsKey, userId);
                var cached = await _cacheService.GetAsync<FavoriteSpotStats>(key).ConfigureAwait(false);
                
                if (cached != null)
                {
                    _logger.LogDebug("Cache hit for favorite stats: user {UserId}", userId);
                    return cached;
                }

                _logger.LogDebug("Cache miss for favorite stats: user {UserId}", userId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving cached favorite stats for user {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Cache favorite stats for a user
        /// </summary>
        public async Task SetFavoriteStatsCacheAsync(int userId, FavoriteSpotStats stats, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = string.Format(FavoriteStatsKey, userId);
                await _cacheService.SetAsync(key, stats, StatsCacheExpiry).ConfigureAwait(false);
                
                _logger.LogDebug("Cached favorite stats for user {UserId}: {TotalFavorites} total", 
                    userId, stats.TotalFavorites);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error caching favorite stats for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Invalidate user's favorite cache
        /// </summary>
        public async Task InvalidateUserFavoritesCacheAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var tasks = new List<Task>
                {
                    _cacheService.RemoveAsync(string.Format(UserFavoritesKey, userId, false)),
                    _cacheService.RemoveAsync(string.Format(UserFavoritesKey, userId, true)),
                    _cacheService.RemoveAsync(string.Format(FavoriteStatsKey, userId))
                };

                await Task.WhenAll(tasks).ConfigureAwait(false);
                
                _logger.LogDebug("Invalidated favorite cache for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating favorite cache for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Invalidate spot's favorite cache
        /// </summary>
        public async Task InvalidateSpotFavoritesCacheAsync(int spotId, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = string.Format(SpotFavoritesCountKey, spotId);
                await _cacheService.RemoveAsync(key).ConfigureAwait(false);
                
                _logger.LogDebug("Invalidated favorite cache for spot {SpotId}", spotId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating favorite cache for spot {SpotId}", spotId);
            }
        }

        /// <summary>
        /// Clear all favorite caches
        /// </summary>
        public async Task ClearAllFavoritesCacheAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // This would require implementing a pattern-based cache clear in the base cache service
                // For now, we'll log that a manual cache clear was requested
                _logger.LogInformation("Manual cache clear requested for all favorites");
                
                // In a production system, you might want to maintain a list of active cache keys
                // or implement pattern-based removal in the cache service
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error clearing all favorite caches");
            }
        }
    }
}