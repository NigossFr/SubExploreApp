using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing user favorite spots with business logic
    /// </summary>
    public interface IFavoriteSpotService
    {
        /// <summary>
        /// Add a spot to user's favorites
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="spotId">Spot ID</param>
        /// <param name="priority">Priority level (1-10, default 5)</param>
        /// <param name="notes">Optional notes about the favorite</param>
        /// <param name="notificationEnabled">Enable notifications for this spot</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if added successfully, false if already favorited</returns>
        Task<bool> AddToFavoritesAsync(int userId, int spotId, int priority = 5, string? notes = null, bool notificationEnabled = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove a spot from user's favorites
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="spotId">Spot ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if removed successfully, false if not favorited</returns>
        Task<bool> RemoveFromFavoritesAsync(int userId, int spotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Toggle favorite status for a spot
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="spotId">Spot ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if now favorited, false if unfavorited</returns>
        Task<bool> ToggleFavoriteAsync(int userId, int spotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a spot is favorited by the user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="spotId">Spot ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if favorited, false otherwise</returns>
        Task<bool> IsSpotFavoritedAsync(int userId, int spotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all user's favorite spots with pagination
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated collection of favorite spots</returns>
        Task<IEnumerable<UserFavoriteSpot>> GetUserFavoritesAsync(int userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get user's favorites ordered by priority
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Ordered collection of favorite spots</returns>
        Task<IEnumerable<UserFavoriteSpot>> GetUserFavoritesByPriorityAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update favorite spot priority
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="spotId">Spot ID</param>
        /// <param name="priority">New priority (1-10)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateFavoritePriorityAsync(int userId, int spotId, int priority, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update favorite spot notes
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="spotId">Spot ID</param>
        /// <param name="notes">New notes</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateFavoriteNotesAsync(int userId, int spotId, string? notes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Toggle notification settings for a favorite spot
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="spotId">Spot ID</param>
        /// <param name="enabled">Notification enabled state</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateFavoriteNotificationAsync(int userId, int spotId, bool enabled, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get spots favorited by a user with notification enabled
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of notification-enabled favorites</returns>
        Task<IEnumerable<UserFavoriteSpot>> GetNotificationFavoritesAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get statistics about user's favorites
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Favorite statistics</returns>
        Task<FavoriteSpotStats> GetUserFavoriteStatsAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get number of users who favorited a specific spot
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of favorites for the spot</returns>
        Task<int> GetSpotFavoritesCountAsync(int spotId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Statistics about a user's favorite spots
    /// </summary>
    public class FavoriteSpotStats
    {
        public int TotalFavorites { get; set; }
        public int NotificationEnabled { get; set; }
        public int HighPriorityFavorites { get; set; }
        public Dictionary<string, int> FavoritesByType { get; set; } = new();
        public DateTime? MostRecentFavorite { get; set; }
        public DateTime? OldestFavorite { get; set; }
    }
}