using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service interface for caching favorite spot operations to improve performance
    /// </summary>
    public interface IFavoriteSpotCacheService
    {
        /// <summary>
        /// Get cached user favorites
        /// </summary>
        Task<IEnumerable<UserFavoriteSpot>?> GetCachedUserFavoritesAsync(int userId, bool byPriority = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cache user favorites
        /// </summary>
        Task SetUserFavoritesCacheAsync(int userId, IEnumerable<UserFavoriteSpot> favorites, bool byPriority = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get cached favorite status for a spot
        /// </summary>
        Task<bool?> GetCachedFavoriteStatusAsync(int userId, int spotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cache favorite status for a spot
        /// </summary>
        Task SetFavoriteStatusCacheAsync(int userId, int spotId, bool isFavorite, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get cached favorite stats for a user
        /// </summary>
        Task<FavoriteSpotStats?> GetCachedFavoriteStatsAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cache favorite stats for a user
        /// </summary>
        Task SetFavoriteStatsCacheAsync(int userId, FavoriteSpotStats stats, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidate user's favorite cache
        /// </summary>
        Task InvalidateUserFavoritesCacheAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidate spot's favorite cache
        /// </summary>
        Task InvalidateSpotFavoritesCacheAsync(int spotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear all favorite caches
        /// </summary>
        Task ClearAllFavoritesCacheAsync(CancellationToken cancellationToken = default);
    }
}