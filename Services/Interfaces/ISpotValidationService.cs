using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for managing spot validation workflow
    /// Implements requirements from section 3.1.5 - Moderation system
    /// </summary>
    public interface ISpotValidationService
    {
        /// <summary>
        /// Get all spots pending validation
        /// </summary>
        Task<List<Spot>> GetPendingValidationSpotsAsync();

        /// <summary>
        /// Get spots under review by current user
        /// </summary>
        Task<List<Spot>> GetSpotsUnderReviewAsync(int moderatorId);

        /// <summary>
        /// Get validation history for a spot
        /// </summary>
        Task<List<SpotValidationHistory>> GetSpotValidationHistoryAsync(int spotId);

        /// <summary>
        /// Validate a spot (approve or reject)
        /// </summary>
        Task<bool> ValidateSpotAsync(int spotId, int validatorId, SpotValidationStatus status, string validationNotes);

        /// <summary>
        /// Assign spot for review to a moderator
        /// </summary>
        Task<bool> AssignSpotForReviewAsync(int spotId, int moderatorId);

        /// <summary>
        /// Flag a spot for safety review
        /// </summary>
        Task<bool> FlagSpotForSafetyReviewAsync(int spotId, int reporterId, string safetyNotes);

        /// <summary>
        /// Get spots flagged for safety review
        /// </summary>
        Task<List<Spot>> GetSpotsFlaggedForSafetyAsync();

        /// <summary>
        /// Complete safety review
        /// </summary>
        Task<bool> CompleteSafetyReviewAsync(int spotId, int reviewerId, bool isSafe, string reviewNotes);

        /// <summary>
        /// Get validation statistics for dashboard
        /// </summary>
        Task<SpotValidationStats> GetValidationStatsAsync();

        /// <summary>
        /// Check if user can moderate spots based on specialization
        /// </summary>
        bool CanModerateSpotType(ModeratorSpecialization moderatorSpecialization, SpotType spotType);
    }

    /// <summary>
    /// Spot validation history record
    /// </summary>
    public class SpotValidationHistory
    {
        public int Id { get; set; }
        public int SpotId { get; set; }
        public int ValidatorId { get; set; }
        public string ValidatorName { get; set; } = string.Empty;
        public SpotValidationStatus Status { get; set; }
        public string ValidationNotes { get; set; } = string.Empty;
        public DateTime ValidatedAt { get; set; }
        public ModeratorSpecialization ValidatorSpecialization { get; set; }
    }

    /// <summary>
    /// Validation statistics for admin dashboard
    /// </summary>
    public class SpotValidationStats
    {
        public int PendingCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int SafetyFlaggedCount { get; set; }
        public int TotalSpots { get; set; }
        public double ApprovalRate { get; set; }
        public TimeSpan AverageReviewTime { get; set; }
        public List<ModeratorPerformance> ModeratorStats { get; set; } = new();
    }

    /// <summary>
    /// Moderator performance statistics
    /// </summary>
    public class ModeratorPerformance
    {
        public int ModeratorId { get; set; }
        public string ModeratorName { get; set; } = string.Empty;
        public ModeratorSpecialization Specialization { get; set; }
        public int SpotsReviewed { get; set; }
        public int SpotsApproved { get; set; }
        public int SpotsRejected { get; set; }
        public TimeSpan AverageReviewTime { get; set; }
        public double ApprovalRate { get; set; }
    }
}