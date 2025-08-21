using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Models.Validation;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for managing spot validation workflow
    /// Implements requirements from section 3.1.5 - Moderation system
    /// Uses Command and Strategy patterns for extensible validation logic
    /// </summary>
    public interface ISpotValidationService
    {
        /// <summary>
        /// Get all spots pending validation with optional filtering
        /// </summary>
        Task<ValidationResult<List<Spot>>> GetPendingValidationSpotsAsync(ValidationFilter? filter = null);

        /// <summary>
        /// Get spots under review by specific moderator
        /// </summary>
        Task<ValidationResult<List<Spot>>> GetSpotsUnderReviewAsync(Guid moderatorId, ValidationFilter? filter = null);

        /// <summary>
        /// Get validation history for a spot with pagination
        /// </summary>
        Task<ValidationResult<List<SpotValidationHistory>>> GetSpotValidationHistoryAsync(Guid spotId, int page = 1, int pageSize = 20);

        /// <summary>
        /// Execute validation command with proper error handling and events
        /// </summary>
        Task<ValidationResult> ExecuteValidationCommandAsync(IValidationCommand command);

        /// <summary>
        /// Assign spot for review using assignment strategy
        /// </summary>
        Task<ValidationResult> AssignSpotForReviewAsync(Guid spotId, Guid moderatorId);

        /// <summary>
        /// Flag a spot for safety review with structured safety flags
        /// </summary>
        Task<ValidationResult> FlagSpotForSafetyReviewAsync(Guid spotId, Guid reporterId, SafetyFlag safetyFlag);

        /// <summary>
        /// Get spots flagged for safety review with filtering
        /// </summary>
        Task<ValidationResult<List<Spot>>> GetSpotsFlaggedForSafetyAsync(ValidationFilter? filter = null);

        /// <summary>
        /// Complete safety review with detailed result tracking
        /// </summary>
        Task<ValidationResult> CompleteSafetyReviewAsync(Guid spotId, Guid reviewerId, SafetyReviewResult reviewResult);

        /// <summary>
        /// Get comprehensive validation statistics for dashboard
        /// </summary>
        Task<ValidationResult<SpotValidationStats>> GetValidationStatsAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Check if user can moderate spots using strategy pattern
        /// </summary>
        Task<bool> CanModerateSpotTypeAsync(ModeratorSpecialization moderatorSpecialization, SpotType spotType);

        /// <summary>
        /// Get available validation actions for a spot and user
        /// </summary>
        Task<ValidationResult<List<ValidationAction>>> GetAvailableActionsAsync(Guid spotId, Guid userId);
    }

    // Note: Classes moved to Models.Validation namespace for better organization
}