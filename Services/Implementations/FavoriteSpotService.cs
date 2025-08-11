using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service implementation for managing user favorite spots with comprehensive business logic
    /// </summary>
    public class FavoriteSpotService : IFavoriteSpotService
    {
        private readonly IUserFavoriteSpotRepository _favoriteRepository;
        private readonly ISpotRepository _spotRepository;
        private readonly IUserRepository _userRepository;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly ILogger<FavoriteSpotService> _logger;

        /// <summary>
        /// Initializes a new instance of the FavoriteSpotService
        /// </summary>
        public FavoriteSpotService(
            IUserFavoriteSpotRepository favoriteRepository,
            ISpotRepository spotRepository,
            IUserRepository userRepository,
            IErrorHandlingService errorHandlingService,
            ILogger<FavoriteSpotService> logger)
        {
            _favoriteRepository = favoriteRepository ?? throw new ArgumentNullException(nameof(favoriteRepository));
            _spotRepository = spotRepository ?? throw new ArgumentNullException(nameof(spotRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Add a spot to user's favorites
        /// </summary>
        public async Task<bool> AddToFavoritesAsync(int userId, int spotId, int priority = 5, string? notes = null, bool notificationEnabled = true, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input parameters
                ValidateUserAndSpotIds(userId, spotId);
                ValidatePriority(priority);
                ValidateNotes(notes);

                // Check if already favorited (fast check)
                var existingFavorite = await _favoriteRepository.IsSpotFavoritedAsync(userId, spotId, cancellationToken).ConfigureAwait(false);
                if (existingFavorite)
                {
                    _logger.LogInformation("Spot {SpotId} is already favorited by user {UserId}", spotId, userId);
                    return false;
                }

                // Batch verify user and spot existence for performance
                var (userExists, spotExists) = await VerifyUserAndSpotExistenceAsync(userId, spotId, cancellationToken).ConfigureAwait(false);
                
                if (!userExists)
                {
                    await _errorHandlingService.HandleValidationErrorAsync(
                        $"User with ID {userId} not found", 
                        nameof(AddToFavoritesAsync));
                    return false;
                }

                if (!spotExists)
                {
                    await _errorHandlingService.HandleValidationErrorAsync(
                        $"Spot with ID {spotId} not found", 
                        nameof(AddToFavoritesAsync));
                    return false;
                }

                // Create new favorite with optimized properties
                var favorite = CreateFavoriteSpot(userId, spotId, priority, notes, notificationEnabled);

                await _favoriteRepository.AddAsync(favorite, cancellationToken).ConfigureAwait(false);
                await _favoriteRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Added spot {SpotId} to favorites for user {UserId} with priority {Priority}", 
                    spotId, userId, priority);

                return true;
            }
            catch (ArgumentException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add spot {SpotId} to favorites for user {UserId}", spotId, userId);
                await _errorHandlingService.HandleExceptionAsync(ex, nameof(AddToFavoritesAsync), showToUser: false);
                return false;
            }
        }

        /// <summary>
        /// Validate user and spot IDs
        /// </summary>
        private static void ValidateUserAndSpotIds(int userId, int spotId)
        {
            if (userId <= 0) throw new ArgumentException("User ID must be positive", nameof(userId));
            if (spotId <= 0) throw new ArgumentException("Spot ID must be positive", nameof(spotId));
        }

        /// <summary>
        /// Validate priority value
        /// </summary>
        private static void ValidatePriority(int priority)
        {
            if (priority < 1 || priority > 10)
                throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be between 1 and 10");
        }

        /// <summary>
        /// Validate notes length
        /// </summary>
        private static void ValidateNotes(string? notes)
        {
            if (!string.IsNullOrEmpty(notes) && notes.Length > 500)
                throw new ArgumentException("Notes cannot exceed 500 characters", nameof(notes));
        }

        /// <summary>
        /// Verify user and spot existence sequentially to prevent DbContext concurrency issues
        /// </summary>
        private async Task<(bool userExists, bool spotExists)> VerifyUserAndSpotExistenceAsync(int userId, int spotId, CancellationToken cancellationToken)
        {
            // Execute sequentially to prevent DbContext concurrency errors
            // "A second operation was started on this context instance before a previous operation completed"
            var userExists = await _userRepository.ExistsAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false);
            var spotExists = await _spotRepository.ExistsAsync(s => s.Id == spotId, cancellationToken).ConfigureAwait(false);

            return (userExists, spotExists);
        }

        /// <summary>
        /// Create a new UserFavoriteSpot instance
        /// </summary>
        private static UserFavoriteSpot CreateFavoriteSpot(int userId, int spotId, int priority, string? notes, bool notificationEnabled)
        {
            return new UserFavoriteSpot
            {
                UserId = userId,
                SpotId = spotId,
                Priority = priority, // Already validated
                Notes = notes?.Trim(),
                NotificationEnabled = notificationEnabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };
        }

        /// <summary>
        /// Remove a spot from user's favorites
        /// </summary>
        public async Task<bool> RemoveFromFavoritesAsync(int userId, int spotId, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateUserAndSpotIds(userId, spotId);

                var removed = await _favoriteRepository.RemoveFavoriteAsync(userId, spotId, cancellationToken).ConfigureAwait(false);
                
                if (removed)
                {
                    await _favoriteRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("Removed spot {SpotId} from favorites for user {UserId}", spotId, userId);
                }
                else
                {
                    _logger.LogWarning("Attempted to remove non-existent favorite: user {UserId}, spot {SpotId}", userId, spotId);
                }

                return removed;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove spot {SpotId} from favorites for user {UserId}", spotId, userId);
                await _errorHandlingService.HandleExceptionAsync(ex, nameof(RemoveFromFavoritesAsync), showToUser: false);
                return false;
            }
        }

        /// <summary>
        /// Toggle favorite status for a spot
        /// </summary>
        public async Task<bool> ToggleFavoriteAsync(int userId, int spotId, CancellationToken cancellationToken = default)
        {
            try
            {
                var isFavorited = await _favoriteRepository.IsSpotFavoritedAsync(userId, spotId, cancellationToken).ConfigureAwait(false);
                
                if (isFavorited)
                {
                    await RemoveFromFavoritesAsync(userId, spotId, cancellationToken).ConfigureAwait(false);
                    return false;
                }
                else
                {
                    await AddToFavoritesAsync(userId, spotId, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, nameof(ToggleFavoriteAsync), showToUser: false);
                return false;
            }
        }

        /// <summary>
        /// Check if a spot is favorited by the user
        /// </summary>
        public async Task<bool> IsSpotFavoritedAsync(int userId, int spotId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _favoriteRepository.IsSpotFavoritedAsync(userId, spotId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(IsSpotFavoritedAsync));
                return false;
            }
        }

        /// <summary>
        /// Get all user's favorite spots with pagination
        /// </summary>
        public async Task<IEnumerable<UserFavoriteSpot>> GetUserFavoritesAsync(int userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            try
            {
                if (userId <= 0)
                {
                    await _errorHandlingService.HandleValidationErrorAsync(
                        "Invalid user ID", 
                        nameof(GetUserFavoritesAsync));
                    return Enumerable.Empty<UserFavoriteSpot>();
                }

                var allFavorites = await _favoriteRepository.GetUserFavoritesAsync(userId, cancellationToken).ConfigureAwait(false);
                
                return allFavorites
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(GetUserFavoritesAsync));
                return Enumerable.Empty<UserFavoriteSpot>();
            }
        }

        /// <summary>
        /// Get user's favorites ordered by priority
        /// </summary>
        public async Task<IEnumerable<UserFavoriteSpot>> GetUserFavoritesByPriorityAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _favoriteRepository.GetUserFavoritesByPriorityAsync(userId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(GetUserFavoritesByPriorityAsync));
                return Enumerable.Empty<UserFavoriteSpot>();
            }
        }

        /// <summary>
        /// Update favorite spot priority
        /// </summary>
        public async Task<bool> UpdateFavoritePriorityAsync(int userId, int spotId, int priority, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _favoriteRepository.UpdateFavoritePriorityAsync(userId, spotId, priority, cancellationToken).ConfigureAwait(false);
                
                if (updated)
                {
                    await _favoriteRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("Updated priority for spot {SpotId} to {Priority} for user {UserId}", 
                        spotId, priority, userId);
                }

                return updated;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(UpdateFavoritePriorityAsync));
                return false;
            }
        }

        /// <summary>
        /// Update favorite spot notes
        /// </summary>
        public async Task<bool> UpdateFavoriteNotesAsync(int userId, int spotId, string? notes, CancellationToken cancellationToken = default)
        {
            try
            {
                var favorite = await _favoriteRepository.GetUserFavoriteAsync(userId, spotId, cancellationToken).ConfigureAwait(false);
                
                if (favorite == null)
                    return false;

                favorite.Notes = notes;
                await _favoriteRepository.UpdateAsync(favorite).ConfigureAwait(false);
                await _favoriteRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Updated notes for favorite spot {SpotId} for user {UserId}", spotId, userId);
                return true;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(UpdateFavoriteNotesAsync));
                return false;
            }
        }

        /// <summary>
        /// Toggle notification settings for a favorite spot
        /// </summary>
        public async Task<bool> UpdateFavoriteNotificationAsync(int userId, int spotId, bool enabled, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _favoriteRepository.UpdateFavoriteNotificationAsync(userId, spotId, enabled, cancellationToken).ConfigureAwait(false);
                
                if (updated)
                {
                    await _favoriteRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("Updated notification settings for spot {SpotId} to {Enabled} for user {UserId}", 
                        spotId, enabled, userId);
                }

                return updated;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(UpdateFavoriteNotificationAsync));
                return false;
            }
        }

        /// <summary>
        /// Get spots favorited by a user with notification enabled
        /// </summary>
        public async Task<IEnumerable<UserFavoriteSpot>> GetNotificationFavoritesAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _favoriteRepository.GetUserNotificationFavoritesAsync(userId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(GetNotificationFavoritesAsync));
                return Enumerable.Empty<UserFavoriteSpot>();
            }
        }

        /// <summary>
        /// Get statistics about user's favorites
        /// </summary>
        public async Task<FavoriteSpotStats> GetUserFavoriteStatsAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var favorites = await _favoriteRepository.GetUserFavoritesAsync(userId, cancellationToken).ConfigureAwait(false);
                var favoritesList = favorites.ToList();

                if (!favoritesList.Any())
                {
                    return new FavoriteSpotStats();
                }

                return new FavoriteSpotStats
                {
                    TotalFavorites = favoritesList.Count,
                    NotificationEnabled = favoritesList.Count(f => f.NotificationEnabled),
                    ActivityFavorites = favoritesList.Count(f => f.Spot?.Type?.Category == Models.Enums.ActivityCategory.Activity),
                    FavoritesByType = favoritesList
                        .Where(f => f.Spot?.Type != null)
                        .GroupBy(f => f.Spot!.Type!.Name)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    MostRecentFavorite = favoritesList.Max(f => f.CreatedAt),
                    OldestFavorite = favoritesList.Min(f => f.CreatedAt)
                };
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(GetUserFavoriteStatsAsync));
                return new FavoriteSpotStats();
            }
        }

        /// <summary>
        /// Get number of users who favorited a specific spot
        /// </summary>
        public async Task<int> GetSpotFavoritesCountAsync(int spotId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _favoriteRepository.GetSpotFavoritesCountAsync(spotId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(GetSpotFavoritesCountAsync));
                return 0;
            }
        }
    }
}