using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for validating and ensuring SpotType data integrity
    /// Helps diagnose and fix issues with missing or inactive SpotTypes
    /// </summary>
    public interface ISpotTypeValidationService
    {
        /// <summary>
        /// Validates that all required SpotTypes exist and are active
        /// </summary>
        Task<SpotTypeValidationResult> ValidateSpotTypesAsync();
        
        /// <summary>
        /// Ensures that all ActivityCategory values have corresponding active SpotTypes
        /// </summary>
        Task<bool> EnsureCategorySpotTypesAsync();
        
        /// <summary>
        /// Gets diagnostic information about current SpotType state
        /// </summary>
        Task<SpotTypeDiagnostics> GetDiagnosticsAsync();
        
        /// <summary>
        /// Repairs common SpotType issues (inactive types, missing categories)
        /// </summary>
        Task<SpotTypeRepairResult> RepairSpotTypesAsync();
        
        /// <summary>
        /// Validates that a specific ActivityCategory has active SpotTypes
        /// </summary>
        Task<bool> HasActiveSpotTypesForCategoryAsync(ActivityCategory category);
        
        /// <summary>
        /// Gets the count of active SpotTypes per category
        /// </summary>
        Task<Dictionary<ActivityCategory, int>> GetActiveTypeCountsByCategoryAsync();
    }
    
    /// <summary>
    /// Result of SpotType validation
    /// </summary>
    public class SpotTypeValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public Dictionary<ActivityCategory, int> CategoryCounts { get; set; } = new();
        public int TotalActiveTypes { get; set; }
        public int TotalInactiveTypes { get; set; }
    }
    
    /// <summary>
    /// Diagnostic information about SpotTypes
    /// </summary>
    public class SpotTypeDiagnostics
    {
        public int TotalSpotTypes { get; set; }
        public int ActiveSpotTypes { get; set; }
        public int InactiveSpotTypes { get; set; }
        public Dictionary<ActivityCategory, List<SpotType>> TypesByCategory { get; set; } = new();
        public List<ActivityCategory> MissingCategories { get; set; } = new();
        public List<string> IssuesFound { get; set; } = new();
        public DateTime DiagnosticTime { get; set; }
    }
    
    /// <summary>
    /// Result of SpotType repair operation
    /// </summary>
    public class SpotTypeRepairResult
    {
        public bool Success { get; set; }
        public int TypesActivated { get; set; }
        public int TypesCreated { get; set; }
        public List<string> ActionsPerformed { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public SpotTypeDiagnostics PostRepairState { get; set; } = new();
    }
}