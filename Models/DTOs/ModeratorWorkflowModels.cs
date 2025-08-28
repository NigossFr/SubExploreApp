using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Models.DTOs
{
    /// <summary>
    /// Missing data transfer objects for moderator workflow service
    /// </summary>

    /// <summary>
    /// Spot validation record
    /// </summary>
    public class SpotValidationRecord
    {
        public Guid Id { get; set; }
        public Guid SpotId { get; set; }
        public Guid ValidatorId { get; set; }
        public User Validator { get; set; } = null!;
        public bool IsApproved { get; set; }
        public string ValidationNotes { get; set; } = string.Empty;
        public DateTime ValidatedAt { get; set; }
        public List<string> ValidationCriteria { get; set; } = new();
    }

    /// <summary>
    /// User moderation record
    /// </summary>
    public class UserModerationRecord
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid ModeratorId { get; set; }
        public User Moderator { get; set; } = null!;
        public string Action { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Moderation task request
    /// </summary>
    public class ModerationTaskRequest
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";
        public Guid RelatedEntityId { get; set; }
        public string RelatedEntityType { get; set; } = string.Empty;
        public ModeratorSpecialization RequiredSpecialization { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Task assignment statistics
    /// </summary>
    public class TaskAssignmentStats
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalTasksAssigned { get; set; }
        public int TasksCompleted { get; set; }
        public int TasksPending { get; set; }
        public double AverageAssignmentTime { get; set; }
        public Dictionary<ModeratorSpecialization, int> TasksBySpecialization { get; set; } = new();
        public Dictionary<string, int> TasksByPriority { get; set; } = new();
    }

    /// <summary>
    /// Moderator performance metrics
    /// </summary>
    public class ModeratorPerformanceMetrics
    {
        public Guid ModeratorId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TasksCompleted { get; set; }
        public int TasksAssigned { get; set; }
        public double CompletionRate { get; set; }
        public double AverageResponseTimeHours { get; set; }
        public int QualityScore { get; set; } // 0-100
        public int CommunityFeedbackScore { get; set; } // 0-100
        public int EscalationCount { get; set; }
        public Dictionary<string, int> TaskTypeBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Peer review of moderation decision
    /// </summary>
    public class PeerReview
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid ReviewerId { get; set; }
        public User Reviewer { get; set; } = null!;
        public bool AgreesWithDecision { get; set; }
        public string ReviewNotes { get; set; } = string.Empty;
        public int QualityRating { get; set; } // 1-5
        public DateTime ReviewDate { get; set; }
        public List<string> SuggestedImprovements { get; set; } = new();
    }

    /// <summary>
    /// Moderation decision review
    /// </summary>
    public class ModerationDecisionReview
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public Guid OriginalModeratorId { get; set; }
        public User OriginalModerator { get; set; } = null!;
        public string Decision { get; set; } = string.Empty;
        public string DecisionReason { get; set; } = string.Empty;
        public DateTime DecisionDate { get; set; }
        public bool RequiresPeerReview { get; set; }
        public List<PeerReview> PeerReviews { get; set; } = new();
    }

    /// <summary>
    /// Training module
    /// </summary>
    public class TrainingModule
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ModeratorSpecialization Specialization { get; set; }
        public string ModuleType { get; set; } = string.Empty;
        public int EstimatedMinutes { get; set; }
        public bool IsRequired { get; set; }
        public string ContentUrl { get; set; } = string.Empty;
        public List<string> LearningObjectives { get; set; } = new();
        public DateTime? CompletedAt { get; set; }
        public int? Score { get; set; }
    }

    /// <summary>
    /// Training completion result
    /// </summary>
    public class TrainingResult
    {
        public int Score { get; set; } // 0-100
        public bool Passed { get; set; }
        public TimeSpan CompletionTime { get; set; }
        public DateTime CompletedAt { get; set; }
        public List<string> IncorrectAnswers { get; set; } = new();
        public Dictionary<string, object> Results { get; set; } = new();
    }

    /// <summary>
    /// Certification requirement
    /// </summary>
    public class CertificationRequirement
    {
        public Guid Id { get; set; }
        public ModeratorSpecialization Specialization { get; set; }
        public string RequirementName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public int MinimumScore { get; set; }
        public string CertificationUrl { get; set; } = string.Empty;
        public TimeSpan ValidityPeriod { get; set; }
    }

    /// <summary>
    /// Moderation guidelines
    /// </summary>
    public class ModerationGuidelines
    {
        public Guid Id { get; set; }
        public ModeratorSpecialization Specialization { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public Guid UpdatedBy { get; set; }
        public User UpdatedByUser { get; set; } = null!;
        public List<GuidelineSection> Sections { get; set; } = new();
        public Dictionary<string, object> Rules { get; set; } = new();
    }

    /// <summary>
    /// Guideline section
    /// </summary>
    public class GuidelineSection
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<string> KeyPoints { get; set; } = new();
        public List<string> Examples { get; set; } = new();
    }

    /// <summary>
    /// Moderation discussion
    /// </summary>
    public class ModerationDiscussion
    {
        public Guid Id { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public User CreatedByUser { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<Guid> Participants { get; set; } = new();
        public List<User> ParticipantUsers { get; set; } = new();
        public bool IsActive { get; set; }
        public int MessageCount { get; set; }
        public DateTime? LastActivity { get; set; }
    }

    /// <summary>
    /// Moderation decision
    /// </summary>
    public class ModerationDecision
    {
        public bool IsApproved { get; set; }
        public string Decision { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
        public bool RequiresPeerReview { get; set; }
        public DateTime DecisionDate { get; set; }
    }
}