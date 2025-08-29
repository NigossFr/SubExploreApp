using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    public interface ISpotEditService
    {
        /// <summary>
        /// Check if current user can edit the specified spot
        /// </summary>
        /// <param name="spot">The spot to check</param>
        /// <returns>True if user can edit this spot</returns>
        Task<bool> CanUserEditSpotAsync(Spot spot);

        /// <summary>
        /// Update basic spot information
        /// </summary>
        /// <param name="spotId">Spot ID to update</param>
        /// <param name="name">New name</param>
        /// <param name="description">New description</param>
        /// <param name="requiredEquipment">Required equipment</param>
        /// <param name="safetyNotes">Safety notes</param>
        /// <param name="bestConditions">Best conditions</param>
        /// <returns>True if update successful</returns>
        Task<bool> UpdateSpotBasicInfoAsync(Guid spotId, string name, string description, 
            string requiredEquipment, string safetyNotes, string bestConditions);

        /// <summary>
        /// Update spot location
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <param name="latitude">New latitude</param>
        /// <param name="longitude">New longitude</param>
        /// <returns>True if update successful</returns>
        Task<bool> UpdateSpotLocationAsync(Guid spotId, decimal latitude, decimal longitude);

        /// <summary>
        /// Update spot technical details
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <param name="maxDepth">Maximum depth</param>
        /// <param name="difficultyLevel">Difficulty level</param>
        /// <param name="currentStrength">Current strength</param>
        /// <returns>True if update successful</returns>
        Task<bool> UpdateSpotTechnicalDetailsAsync(Guid spotId, int? maxDepth, 
            Models.Enums.DifficultyLevel difficultyLevel, Models.Enums.CurrentStrength? currentStrength);

        /// <summary>
        /// Update spot type
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <param name="newTypeId">New spot type ID</param>
        /// <returns>True if update successful</returns>
        Task<bool> UpdateSpotTypeAsync(Guid spotId, Guid newTypeId);

        /// <summary>
        /// Add or update spot safety flags
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <param name="safetyFlags">Safety flags dictionary</param>
        /// <returns>True if update successful</returns>
        Task<bool> UpdateSpotSafetyFlagsAsync(Guid spotId, Dictionary<string, object> safetyFlags);

        /// <summary>
        /// Get edit history for a spot (admin feature)
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <returns>List of edit records</returns>
        Task<List<SpotEditRecord>> GetSpotEditHistoryAsync(Guid spotId);

        /// <summary>
        /// Validate spot data before update
        /// </summary>
        /// <param name="spot">Spot data to validate</param>
        /// <returns>Validation result with errors if any</returns>
        Task<SpotEditValidationResult> ValidateSpotDataAsync(Spot spot);

        /// <summary>
        /// Submit spot for re-validation after major edits
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <param name="editReason">Reason for the edit</param>
        /// <returns>True if submission successful</returns>
        Task<bool> SubmitForRevalidationAsync(Guid spotId, string editReason);
    }

    public class SpotEditRecord
    {
        public Guid Id { get; set; }
        public Guid SpotId { get; set; }
        public Guid EditorId { get; set; }
        public DateTime EditedAt { get; set; }
        public string EditType { get; set; } = string.Empty;
        public string Changes { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public User Editor { get; set; } = null!;
    }

    public class SpotEditValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool RequiresRevalidation { get; set; }
    }
}