using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SubExplore.Services.Implementations
{
    public class UserProfileService : IUserProfileService
    {
        private readonly SubExploreDbContext _context;
        private readonly ISettingsService _settingsService;

        public UserProfileService(SubExploreDbContext context, ISettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public bool IsAuthenticated => CurrentUserId.HasValue;

        public int? CurrentUserId => _settingsService.Get<int?>("CurrentUserId");

        public async Task<User?> GetCurrentUserAsync()
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
                return null;

            return await GetUserByIdAsync(userId.Value);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Preferences)
                    .Include(u => u.CreatedSpots)
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserProfileService] Error getting user by ID: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateUserProfileAsync(User user)
        {
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                if (existingUser == null)
                    return false;

                // Validate before updating
                var (isValid, validationErrors) = await ValidateUserProfileAsync(user);
                if (!isValid)
                {
                    System.Diagnostics.Debug.WriteLine($"[UserProfileService] Validation failed: {string.Join(", ", validationErrors)}");
                    return false;
                }

                // Update allowed fields
                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.Username = user.Username;
                existingUser.Email = user.Email;
                existingUser.AvatarUrl = user.AvatarUrl;
                existingUser.ExpertiseLevel = user.ExpertiseLevel;
                existingUser.Certifications = user.Certifications;
                existingUser.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserProfileService] Error updating user profile: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUserAvatarAsync(string avatarUrl)
        {
            try
            {
                var userId = CurrentUserId;
                if (!userId.HasValue)
                    return false;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user == null)
                    return false;

                user.AvatarUrl = avatarUrl;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserProfileService] Error updating avatar: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUserPreferencesAsync(UserPreferences preferences)
        {
            try
            {
                var userId = CurrentUserId;
                if (!userId.HasValue)
                    return false;

                var existingPreferences = await _context.UserPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value);

                if (existingPreferences == null)
                {
                    preferences.UserId = userId.Value;
                    preferences.CreatedAt = DateTime.UtcNow;
                    _context.UserPreferences.Add(preferences);
                }
                else
                {
                    existingPreferences.Theme = preferences.Theme;
                    existingPreferences.DisplayNamePreference = preferences.DisplayNamePreference;
                    existingPreferences.NotificationSettings = preferences.NotificationSettings;
                    existingPreferences.Language = preferences.Language;
                    existingPreferences.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserProfileService] Error updating preferences: {ex.Message}");
                return false;
            }
        }

        public async Task<UserStatsDto> GetUserStatsAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.CreatedSpots)
                    .ThenInclude(s => s.Media)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return new UserStatsDto();

                var spots = user.CreatedSpots.ToList();
                var stats = new UserStatsDto
                {
                    TotalSpots = spots.Count,
                    ValidatedSpots = spots.Count(s => s.ValidationStatus == SpotValidationStatus.Approved),
                    PendingSpots = spots.Count(s => s.ValidationStatus == SpotValidationStatus.Pending),
                    TotalPhotos = spots.Sum(s => s.Media?.Count ?? 0),
                    LastSpotCreated = spots.OrderByDescending(s => s.CreatedAt).FirstOrDefault()?.CreatedAt,
                    LastActivity = user.LastLogin ?? user.UpdatedAt ?? user.CreatedAt,
                    DaysActive = (DateTime.UtcNow - user.CreatedAt).Days,
                    ExpertiseLevel = user.ExpertiseLevel,
                    AverageDepth = spots.Where(s => s.MaxDepth.HasValue).Average(s => (double?)s.MaxDepth) ?? 0,
                    MaxDepth = spots.Where(s => s.MaxDepth.HasValue).Max(s => (double?)s.MaxDepth) ?? 0,
                    ContributionScore = CalculateContributionScore(user, spots)
                };

                // Parse certifications
                if (!string.IsNullOrEmpty(user.Certifications))
                {
                    try
                    {
                        var certifications = JsonSerializer.Deserialize<List<string>>(user.Certifications);
                        stats.RecentCertifications = certifications?.Take(3).ToList() ?? new List<string>();
                    }
                    catch
                    {
                        stats.RecentCertifications = new List<string>();
                    }
                }

                // Calculate spots by type
                var spotTypes = await _context.SpotTypes.ToListAsync();
                foreach (var spotType in spotTypes)
                {
                    var count = spots.Count(s => s.TypeId == spotType.Id);
                    if (count > 0)
                    {
                        stats.SpotsByType[spotType.Name] = count;
                    }
                }

                // Find favorite spot type
                if (stats.SpotsByType.Any())
                {
                    var favoriteType = stats.SpotsByType.OrderByDescending(kvp => kvp.Value).First();
                    stats.FavoriteSpotType = favoriteType.Key;
                }

                return stats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserProfileService] Error getting user stats: {ex.Message}");
                return new UserStatsDto();
            }
        }

        public async Task<(bool IsValid, List<string> ValidationErrors)> ValidateUserProfileAsync(User user)
        {
            var validationErrors = new List<string>();
            var validationContext = new ValidationContext(user);
            var validationResults = new List<ValidationResult>();

            // Run standard validation attributes
            var isValid = Validator.TryValidateObject(user, validationContext, validationResults, true);

            if (!isValid)
            {
                validationErrors.AddRange(validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error"));
            }

            // Custom validation rules
            if (!string.IsNullOrEmpty(user.Email))
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == user.Email && u.Id != user.Id);
                if (existingUser != null)
                {
                    validationErrors.Add("Email address is already in use");
                }
            }

            if (!string.IsNullOrEmpty(user.Username))
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == user.Username && u.Id != user.Id);
                if (existingUser != null)
                {
                    validationErrors.Add("Username is already taken");
                }
            }

            return (validationErrors.Count == 0, validationErrors);
        }

        private int CalculateContributionScore(User user, List<Spot> spots)
        {
            int score = 0;
            
            // Base score for each validated spot
            score += spots.Count(s => s.ValidationStatus == SpotValidationStatus.Approved) * 10;
            
            // Bonus for spots with photos
            score += spots.Count(s => s.Media?.Any() == true) * 5;
            
            // Bonus for spots with safety information
            score += spots.Count(s => !string.IsNullOrEmpty(s.SafetyNotes)) * 3;
            
            // Bonus for detailed descriptions
            score += spots.Count(s => !string.IsNullOrEmpty(s.Description) && s.Description.Length > 100) * 2;
            
            // Account age bonus
            var accountAgeDays = (DateTime.UtcNow - user.CreatedAt).Days;
            score += Math.Min(accountAgeDays / 30, 12); // Max 12 points for account age
            
            return score;
        }
    }
}