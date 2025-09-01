using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;
using SubExplore.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Implémentation simple du service de profil utilisateur utilisant SimpleAuthenticationService
    /// Compatible avec l'API Supabase sans Entity Framework
    /// </summary>
    public class SimpleUserProfileService : IUserProfileService
    {
        private readonly ISimpleAuthenticationService _authService;
        private readonly ILogger<SimpleUserProfileService> _logger;
        private readonly ISupabaseUserService _userService;

        public SimpleUserProfileService(
            ISimpleAuthenticationService authService,
            ISupabaseUserService userService,
            ILogger<SimpleUserProfileService> logger)
        {
            _authService = authService;
            _userService = userService;
            _logger = logger;
        }

        public bool IsAuthenticated => _authService.IsAuthenticated;

        public Guid? CurrentUserId
        {
            get
            {
                var user = _authService.CurrentUser;
                return user?.Id;
            }
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                if (!IsAuthenticated)
                {
                    _logger.LogWarning("User not authenticated when requesting current user");
                    return null;
                }

                var domainUser = _authService.CurrentUser;
                if (domainUser == null)
                {
                    return null;
                }

                // Ensure user has preferences - create default if missing
                if (domainUser.Preferences == null)
                {
                    _logger.LogInformation("User has no preferences, creating default preferences");
                    domainUser.Preferences = new UserPreferences
                    {
                        Id = Guid.NewGuid(),
                        UserId = domainUser.Id,
                        Theme = "light",
                        DisplayNamePreference = "username",
                        Language = "fr",
                        NotificationSettings = new Dictionary<string, object>
                        {
                            ["push_notifications"] = true,
                            ["email_notifications"] = true,
                            ["spots_nearby"] = true,
                            ["community_updates"] = true,
                            ["safety_alerts"] = true
                        },
                        CreatedAt = DateTime.UtcNow
                    };
                }

                return domainUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return null;
            }
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation($"Getting user profile for ID: {userId}");
                
                // Pour l'instant, seul l'utilisateur courant peut être récupéré
                var currentUser = await GetCurrentUserAsync();
                return currentUser?.Id == userId ? currentUser : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user by ID: {userId}");
                return null;
            }
        }

        public async Task<bool> UpdateUserProfileAsync(User user)
        {
            try
            {
                _logger.LogInformation($"Updating user profile for: {user.Email}");
                
                // Pour l'instant, retourne true (fonctionnalité à implémenter avec Supabase)
                await Task.Delay(1); // Éviter les warnings async
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return false;
            }
        }

        public async Task<bool> UpdateUserAvatarAsync(string avatarUrl)
        {
            try
            {
                _logger.LogInformation("Updating user avatar");
                
                // Pour l'instant, retourne true (fonctionnalité à implémenter)
                await Task.Delay(1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user avatar");
                return false;
            }
        }

        public async Task<bool> UpdateUserPreferencesAsync(UserPreferences preferences)
        {
            try
            {
                _logger.LogInformation("Updating user preferences - Theme: {Theme}, Language: {Language}", 
                    preferences?.Theme, preferences?.Language);
                
                // Update the current user's preferences in memory
                if (_authService.CurrentUser != null && preferences != null)
                {
                    _authService.CurrentUser.Preferences = preferences;
                    _logger.LogInformation("User preferences updated in memory");
                }
                
                // TODO: Persist to Supabase database when implemented
                await Task.Delay(1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences");
                return false;
            }
        }

        public async Task<UserStatsDto> GetUserStatsAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation($"Getting user stats for ID: {userId}");
                
                // Retourne des stats par défaut pour l'instant
                return new UserStatsDto
                {
                    TotalSpots = 0,
                    ValidatedSpots = 0,
                    PendingSpots = 0,
                    TotalPhotos = 0,
                    LastSpotCreated = null,
                    LastActivity = DateTime.UtcNow,
                    DaysActive = 0,
                    ContributionScore = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user stats for ID: {userId}");
                return new UserStatsDto
                {
                    TotalSpots = 0,
                    ValidatedSpots = 0,
                    PendingSpots = 0,
                    TotalPhotos = 0,
                    LastSpotCreated = null,
                    LastActivity = DateTime.UtcNow,
                    DaysActive = 0,
                    ContributionScore = 0
                };
            }
        }

        public async Task<(bool IsValid, List<string> ValidationErrors)> ValidateUserProfileAsync(User user)
        {
            var errors = new List<string>();

            try
            {
                // Validation basique
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    errors.Add("Email is required");
                }

                if (user.Id == Guid.Empty)
                {
                    errors.Add("User ID is required");
                }

                await Task.Delay(1);
                return (errors.Count == 0, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user profile");
                errors.Add("Validation error occurred");
                return (false, errors);
            }
        }
    }
}