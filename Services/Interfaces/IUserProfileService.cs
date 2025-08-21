using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;

namespace SubExplore.Services.Interfaces
{
    public interface IUserProfileService
    {
        /// <summary>
        /// Get the current user's profile information
        /// </summary>
        Task<User?> GetCurrentUserAsync();

        /// <summary>
        /// Get user profile by ID
        /// </summary>
        Task<User?> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// Update user profile information
        /// </summary>
        Task<bool> UpdateUserProfileAsync(User user);

        /// <summary>
        /// Update user avatar
        /// </summary>
        Task<bool> UpdateUserAvatarAsync(string avatarUrl);

        /// <summary>
        /// Update user preferences
        /// </summary>
        Task<bool> UpdateUserPreferencesAsync(UserPreferences preferences);

        /// <summary>
        /// Get user's diving statistics
        /// </summary>
        Task<UserStatsDto> GetUserStatsAsync(Guid userId);

        /// <summary>
        /// Validate user profile data
        /// </summary>
        Task<(bool IsValid, List<string> ValidationErrors)> ValidateUserProfileAsync(User user);

        /// <summary>
        /// Check if current user is authenticated
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Get current user ID
        /// </summary>
        Guid? CurrentUserId { get; }
    }
}