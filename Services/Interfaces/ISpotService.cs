using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for advanced spot management, business logic, and analytics
    /// Provides enhanced functionality beyond basic repository operations
    /// </summary>
    public interface ISpotService
    {
        // Core Business Operations
        Task<Spot?> GetSpotWithFullDetailsAsync(int spotId);
        Task<IEnumerable<Spot>> GetSpotsWithinRadiusAsync(double latitude, double longitude, double radiusKm, SpotValidationStatus? status = null);
        Task<IEnumerable<Spot>> GetSpotsByDifficultyAsync(DifficultyLevel difficulty);
        Task<IEnumerable<Spot>> GetSpotsByCategoryAsync(ActivityCategory category);
        Task<IEnumerable<Spot>> GetRecommendedSpotsAsync(int userId, int limit = 10);
        
        // Safety and Validation
        Task<bool> ValidateSpotSafetyAsync(int spotId);
        Task<SpotSafetyReport> GenerateSafetyReportAsync(int spotId);
        Task<bool> IsSpotSafeForUserLevelAsync(int spotId, DifficultyLevel userLevel);
        Task UpdateSpotSafetyReviewAsync(int spotId, string reviewNotes, int reviewerId);
        
        // Visit Tracking and Analytics
        Task RecordSpotVisitAsync(int spotId, int userId);
        Task<SpotStatistics> GetSpotStatisticsAsync(int spotId);
        Task<IEnumerable<Spot>> GetMostPopularSpotsAsync(int limit = 10, TimeSpan? period = null);
        Task<IEnumerable<Spot>> GetRecentlyAddedSpotsAsync(int limit = 10);
        
        // Rating and Reviews
        Task<double> GetSpotAverageRatingAsync(int spotId);
        Task<int> GetSpotReviewCountAsync(int spotId);
        Task<bool> HasUserVisitedSpotAsync(int spotId, int userId);
        
        // Search and Discovery
        Task<IEnumerable<Spot>> SearchSpotsAsync(string searchTerm, double? userLat = null, double? userLon = null);
        Task<IEnumerable<Spot>> GetSimilarSpotsAsync(int spotId, int limit = 5);
        Task<IEnumerable<Spot>> GetSpotsNearUserAsync(int userId, double radiusKm = 50);
        
        // Favorites and Collections
        Task<bool> ToggleFavoriteSpotAsync(int spotId, int userId);
        Task<bool> IsSpotFavoriteAsync(int spotId, int userId);
        Task<IEnumerable<Spot>> GetUserFavoriteSpotsAsync(int userId);
        
        // Data Enrichment
        Task<Models.Domain.WeatherInfo?> GetSpotCurrentWeatherAsync(int spotId);
        Task<TideInfo?> GetSpotTideInfoAsync(int spotId);
        Task<IEnumerable<Spot>> GetSpotsWithGoodConditionsAsync();
        
        // Quality Assurance
        Task<SpotQualityScore> CalculateSpotQualityScoreAsync(int spotId);
        Task<bool> RequiresExpertValidationAsync(int spotId);
        Task FlagSpotForReviewAsync(int spotId, string reason, int reporterId);
    }
    
    /// <summary>
    /// Comprehensive safety report for a diving spot
    /// </summary>
    public class SpotSafetyReport
    {
        public int SpotId { get; set; }
        public string SpotName { get; set; } = string.Empty;
        public int SafetyScore { get; set; } // 0-100
        public DateTime LastReviewDate { get; set; }
        public string ReviewedBy { get; set; } = string.Empty;
        public List<string> SafetyWarnings { get; set; } = new();
        public List<string> RequiredEquipment { get; set; } = new();
        public CurrentStrength RecommendedMaxCurrent { get; set; }
        public DifficultyLevel MinimumRequiredLevel { get; set; }
        public bool RequiresGuide { get; set; }
        public string EmergencyContact { get; set; } = string.Empty;
        public string NearestHospital { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Statistical information about spot usage and popularity
    /// </summary>
    public class SpotStatistics
    {
        public int SpotId { get; set; }
        public int TotalVisits { get; set; }
        public int UniqueVisitors { get; set; }
        public int VisitsThisMonth { get; set; }
        public int VisitsThisWeek { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public DateTime LastVisitDate { get; set; }
        public DateTime? PeakSeasonStart { get; set; }
        public DateTime? PeakSeasonEnd { get; set; }
        public List<string> PopularTimeSlots { get; set; } = new();
        public Dictionary<DifficultyLevel, int> VisitorsByLevel { get; set; } = new();
    }
    
    /// <summary>
    /// Quality assessment score for spots
    /// </summary>
    public class SpotQualityScore
    {
        public int SpotId { get; set; }
        public int OverallScore { get; set; } // 0-100
        public int DataCompletenessScore { get; set; }
        public int SafetyInformationScore { get; set; }
        public int CommunityEngagementScore { get; set; }
        public int MediaQualityScore { get; set; }
        public bool IsRecommendedSpot { get; set; }
        public List<string> ImprovementSuggestions { get; set; } = new();
        public DateTime CalculatedAt { get; set; }
    }
    
    
    /// <summary>
    /// Tide information for coastal spots
    /// </summary>
    public class TideInfo
    {
        public DateTime HighTide { get; set; }
        public DateTime LowTide { get; set; }
        public double CurrentHeight { get; set; }
        public string TideDirection { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}