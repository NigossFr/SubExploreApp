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
        Task<IEnumerable<UserFavoriteSpot>?> GetCachedUserFavoritesAsync(Guid userId, bool byPriority = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cache user favorites
        /// </summary>
        Task SetUserFavoritesCacheAsync(Guid userId, IEnumerable<UserFavoriteSpot> favorites, bool byPriority = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get cached favorite status for a spot
        /// </summary>
        Task<bool?> GetCachedFavoriteStatusAsync(Guid userId, Guid spotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cache favorite status for a spot
        /// </summary>
        Task SetFavoriteStatusCacheAsync(Guid userId, Guid spotId, bool isFavorite, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get cached favorite stats for a user
        /// </summary>
        Task<FavoriteSpotStats?> GetCachedFavoriteStatsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cache favorite stats for a user
        /// </summary>
        Task SetFavoriteStatsCacheAsync(Guid userId, FavoriteSpotStats stats, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidate user's favorite cache
        /// </summary>
        Task InvalidateUserFavoritesCacheAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidate spot's favorite cache
        /// </summary>
        Task InvalidateSpotFavoritesCacheAsync(Guid spotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear all favorite caches
        /// </summary>
        Task ClearAllFavoritesCacheAsync(CancellationToken cancellationToken = default);
    }
}