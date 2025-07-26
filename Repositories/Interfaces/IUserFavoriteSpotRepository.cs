using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SubExplore.Models.Domain;

namespace SubExplore.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for managing user favorite spots with advanced query capabilities
    /// </summary>
    public interface IUserFavoriteSpotRepository : IGenericRepository<UserFavoriteSpot>
    {
        /// <summary>
        /// Get all favorite spots for a specific user with spot details
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of favorite spots with related data</returns>
        Task<IEnumerable<UserFavoriteSpot>> GetUserFavoritesAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get user favorites ordered by priority and creation date
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Ordered collection of favorite spots</returns>
        Task<IEnumerable<UserFavoriteSpot>> GetUserFavoritesByPriorityAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a specific spot is favorited by a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="spotId">Spot ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if spot is favorited, false otherwise</returns>
        Task<bool> IsSpotFavoritedAsync(int userId, int spotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the favorite relationship between a user and spot
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="spotId">Spot ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UserFavoriteSpot entity or null if not found</returns>
        Task<UserFavoriteSpot?> GetUserFavoriteAsync(int userId, int spotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get users who have favorited a specific spot
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of users who favorited the spot</returns>
        Task<IEnumerable<User>> GetSpotFavoritersAsync(int spotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get count of users who favorited a specific spot
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of users who favorited the spot</returns>
        Task<int> GetSpotFavoritesCountAsync(int spotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get user's favorites with notification enabled
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of favorites with notifications enabled</returns>
        Task<IEnumerable<UserFavoriteSpot>> GetUserNotificationFavoritesAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove a favorite spot relationship
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="spotId">Spot ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if removed successfully, false if not found</returns>
        Task<bool> RemoveFavoriteAsync(int userId, int spotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update favorite spot priority
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="spotId">Spot ID</param>
        /// <param name="priority">New priority level (1-10)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if updated successfully, false if not found</returns>
        Task<bool> UpdateFavoritePriorityAsync(int userId, int spotId, int priority, CancellationToken cancellationToken = default);

        /// <summary>
        /// Toggle notification settings for a favorite spot
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="spotId">Spot ID</param>
        /// <param name="enabled">Notification enabled state</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if updated successfully, false if not found</returns>
        Task<bool> UpdateFavoriteNotificationAsync(int userId, int spotId, bool enabled, CancellationToken cancellationToken = default);
    }
}