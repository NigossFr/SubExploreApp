using SubExplore.Models.Enums;

namespace SubExplore.Models.DTOs
{
    public class UserStatsDto
    {
        public int TotalSpots { get; set; }
        public int ValidatedSpots { get; set; }
        public int PendingSpots { get; set; }
        public int TotalPhotos { get; set; }
        public DateTime? LastSpotCreated { get; set; }
        public DateTime? LastActivity { get; set; }
        public Dictionary<string, int> SpotsByType { get; set; } = new();
        public int DaysActive { get; set; }
        public ExpertiseLevel? ExpertiseLevel { get; set; }
        public List<string> RecentCertifications { get; set; } = new();
        public double? AverageDepth { get; set; }
        public double? MaxDepth { get; set; }
        public string? FavoriteSpotType { get; set; }
        public int ContributionScore { get; set; }
    }
}