using System;
using SubExplore.Models.Domain;

namespace SubExplore.Models.Navigation
{
    /// <summary>
    /// Navigation parameter for favorite-specific context
    /// </summary>
    public class FavoriteNavigationParameter
    {
        public Guid FavoriteId { get; set; }
        public Guid SpotId { get; set; }
        public Guid UserId { get; set; }
        public string? SpotName { get; set; }
        public string? Source { get; set; } = "Unknown";
        public bool IsFavorite { get; set; } = true;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int Priority { get; set; } = 5;
        public bool NotificationEnabled { get; set; } = true;
        public string? Notes { get; set; }
        
        // Context for enhanced navigation
        public DateTime FavoriteCreatedAt { get; set; }
        public string? PreviousPage { get; set; }
        public Dictionary<string, object>? AdditionalContext { get; set; }

        public FavoriteNavigationParameter()
        {
            AdditionalContext = new Dictionary<string, object>();
        }

        public FavoriteNavigationParameter(UserFavoriteSpot favorite, string source = "Unknown", string? previousPage = null)
        {
            FavoriteId = favorite.Id;
            SpotId = favorite.SpotId;
            UserId = favorite.UserId;
            SpotName = favorite.Spot?.Name;
            Source = source;
            Priority = favorite.Priority;
            NotificationEnabled = favorite.NotificationEnabled;
            Notes = favorite.Notes;
            FavoriteCreatedAt = favorite.CreatedAt;
            PreviousPage = previousPage;
            
            if (favorite.Spot != null)
            {
                Latitude = (double?)favorite.Spot.Latitude;
                Longitude = (double?)favorite.Spot.Longitude;
            }
            
            AdditionalContext = new Dictionary<string, object>();
        }

        /// <summary>
        /// Add additional context information
        /// </summary>
        public void AddContext(string key, object value)
        {
            AdditionalContext ??= new Dictionary<string, object>();
            AdditionalContext[key] = value;
        }

        /// <summary>
        /// Get context value with type conversion
        /// </summary>
        public T GetContext<T>(string key, T defaultValue = default)
        {
            if (AdditionalContext?.TryGetValue(key, out var value) == true)
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
}