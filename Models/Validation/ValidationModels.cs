using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Validation
{
    /// <summary>
    /// Result wrapper for validation operations with success/error states
    /// </summary>
    public class ValidationResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public List<string> ValidationErrors { get; init; } = new();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        public static ValidationResult CreateSuccess() => new() { Success = true };
        public static ValidationResult CreateError(string errorMessage) => new() 
        { 
            Success = false, 
            ErrorMessage = errorMessage 
        };
        public static ValidationResult CreateValidationError(List<string> errors) => new() 
        { 
            Success = false, 
            ValidationErrors = errors 
        };
    }

    /// <summary>
    /// Generic result wrapper with data payload
    /// </summary>
    public class ValidationResult<T> : ValidationResult
    {
        public T? Data { get; init; }

        public static ValidationResult<T> CreateSuccess(T data) => new() 
        { 
            Success = true, 
            Data = data 
        };
        
        public static new ValidationResult<T> CreateError(string errorMessage) => new() 
        { 
            Success = false, 
            ErrorMessage = errorMessage 
        };
    }

    /// <summary>
    /// Filter criteria for validation queries
    /// </summary>
    public class ValidationFilter
    {
        public SpotValidationStatus? Status { get; set; }
        public ModeratorSpecialization? ModeratorSpecialization { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public ActivityCategory? ActivityCategory { get; set; }
        public DifficultyLevel? MinDifficulty { get; set; }
        public DifficultyLevel? MaxDifficulty { get; set; }
        public bool? HasSafetyFlags { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Structured safety flag with severity and category
    /// </summary>
    public record SafetyFlag
    {
        public SafetyFlagType Type { get; init; }
        public SafetyFlagSeverity Severity { get; init; }
        public string Description { get; init; } = string.Empty;
        public int ReportedByUserId { get; init; }
        public DateTime ReportedAt { get; init; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; init; } = new();

        public static SafetyFlag Create(SafetyFlagType type, SafetyFlagSeverity severity, string description, int reporterId)
        {
            return new SafetyFlag
            {
                Type = type,
                Severity = severity,
                Description = description,
                ReportedByUserId = reporterId
            };
        }
    }

    /// <summary>
    /// Safety review result with detailed outcome
    /// </summary>
    public record SafetyReviewResult
    {
        public bool IsSafe { get; init; }
        public string ReviewNotes { get; init; } = string.Empty;
        public List<SafetyRecommendation> Recommendations { get; init; } = new();
        public SafetyRiskLevel RiskLevel { get; init; }
        public DateTime ReviewedAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Safety recommendation for spot improvement
    /// </summary>
    public record SafetyRecommendation
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public SafetyRecommendationType Type { get; init; }
        public bool IsMandatory { get; init; }
    }

    /// <summary>
    /// Available validation action for a spot and user combination
    /// </summary>
    public record ValidationAction
    {
        public string ActionId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public ValidationActionType Type { get; init; }
        public bool RequiresNotes { get; init; }
        public List<string> RequiredPermissions { get; init; } = new();
    }

    /// <summary>
    /// Enhanced validation statistics with trends
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
        public Dictionary<ActivityCategory, int> SpotsByCategory { get; set; } = new();
        public Dictionary<SafetyFlagType, int> SafetyFlagsByType { get; set; } = new();
        public ValidationTrends Trends { get; set; } = new();
    }

    /// <summary>
    /// Moderator performance with enhanced metrics
    /// </summary>
    public class ModeratorPerformance
    {
        public int ModeratorId { get; set; }
        public string ModeratorName { get; set; } = string.Empty;
        public ModeratorSpecialization Specialization { get; set; }
        public int SpotsReviewed { get; set; }
        public int SpotsApproved { get; set; }
        public int SpotsRejected { get; set; }
        public int SafetyReviewsCompleted { get; set; }
        public TimeSpan AverageReviewTime { get; set; }
        public double ApprovalRate { get; set; }
        public double QualityScore { get; set; }
        public List<string> Achievements { get; set; } = new();
    }

    /// <summary>
    /// Validation trends for analytics
    /// </summary>
    public class ValidationTrends
    {
        public Dictionary<DateTime, int> DailySubmissions { get; set; } = new();
        public Dictionary<DateTime, int> DailyApprovals { get; set; } = new();
        public Dictionary<DateTime, double> DailyApprovalRates { get; set; } = new();
        public List<TrendAlert> Alerts { get; set; } = new();
    }

    /// <summary>
    /// Trend alert for monitoring
    /// </summary>
    public record TrendAlert
    {
        public string Message { get; init; } = string.Empty;
        public TrendAlertSeverity Severity { get; init; }
        public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
    }

    // Supporting enums for validation system
    public enum SafetyFlagType
    {
        Equipment,
        Environment,
        Access,
        Legal,
        Medical,
        Wildlife,
        Weather,
        Technical,
        Other
    }

    public enum SafetyFlagSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum SafetyRiskLevel
    {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum SafetyRecommendationType
    {
        Equipment,
        Training,
        Supervision,
        Timing,
        Route,
        Emergency
    }

    public enum ValidationActionType
    {
        Approve,
        Reject,
        AssignReview,
        RequestRevision,
        FlagSafety,
        CompleteSafetyReview,
        Archive,
        Restore
    }

    public enum TrendAlertSeverity
    {
        Info,
        Warning,
        Critical
    }

    /// <summary>
    /// Spot validation history record - moved from ISpotValidationService
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
}