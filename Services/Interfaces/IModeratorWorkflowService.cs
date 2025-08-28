using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Moderator workflow service for managing moderation tasks and processes
    /// Integrates with spot validation, content moderation, and community management
    /// </summary>
    public interface IModeratorWorkflowService
    {
        #region Workflow Management

        /// <summary>
        /// Get moderator dashboard overview
        /// </summary>
        Task<ModeratorDashboard> GetModeratorDashboardAsync(Guid moderatorId);

        /// <summary>
        /// Get pending moderation tasks for moderator
        /// </summary>
        Task<IEnumerable<ModerationTask>> GetPendingTasksAsync(Guid moderatorId, ModerationTaskType? taskType = null);

        /// <summary>
        /// Assign moderation task to moderator
        /// </summary>
        Task<bool> AssignTaskAsync(Guid taskId, Guid moderatorId);

        /// <summary>
        /// Complete moderation task
        /// </summary>
        Task<bool> CompleteTaskAsync(Guid taskId, ModerationDecision decision, string notes);

        /// <summary>
        /// Escalate moderation task to senior moderator
        /// </summary>
        Task<bool> EscalateTaskAsync(Guid taskId, string reason);

        #endregion

        #region Spot Validation Workflow

        /// <summary>
        /// Get spots pending validation for moderator's specialization
        /// </summary>
        Task<IEnumerable<SpotValidationTask>> GetPendingSpotValidationsAsync(Guid moderatorId);

        /// <summary>
        /// Validate spot with detailed review
        /// </summary>
        Task<bool> ValidateSpotAsync(Guid spotId, SpotValidationResult validation);

        /// <summary>
        /// Reject spot with feedback
        /// </summary>
        Task<bool> RejectSpotAsync(Guid spotId, List<string> rejectionReasons, string feedback);

        /// <summary>
        /// Request spot improvements from creator
        /// </summary>
        Task<bool> RequestSpotImprovementsAsync(Guid spotId, List<SpotImprovementRequest> improvements);

        /// <summary>
        /// Get spot validation history
        /// </summary>
        Task<IEnumerable<SpotValidationRecord>> GetSpotValidationHistoryAsync(Guid spotId);

        #endregion

        #region Content Moderation

        /// <summary>
        /// Get reported content for review
        /// </summary>
        Task<IEnumerable<ContentModerationTask>> GetReportedContentAsync(Guid moderatorId);

        /// <summary>
        /// Moderate reported content
        /// </summary>
        Task<bool> ModerateContentAsync(Guid contentId, ContentModerationAction action, string reason);

        /// <summary>
        /// Get content moderation guidelines for specialization
        /// </summary>
        Task<ModerationGuidelines> GetModerationGuidelinesAsync(ModeratorSpecialization specialization);

        /// <summary>
        /// Flag content for expert review
        /// </summary>
        Task<bool> FlagForExpertReviewAsync(Guid contentId, string flagReason);

        #endregion

        #region Community Management

        /// <summary>
        /// Get user reports for moderator's specialization
        /// </summary>
        Task<IEnumerable<UserReport>> GetUserReportsAsync(Guid moderatorId);

        /// <summary>
        /// Process user report
        /// </summary>
        Task<bool> ProcessUserReportAsync(Guid reportId, UserModerationAction action, string reason);

        /// <summary>
        /// Issue user warning
        /// </summary>
        Task<bool> IssueUserWarningAsync(Guid userId, string reason, WarningLevel level);

        /// <summary>
        /// Suspend user temporarily
        /// </summary>
        Task<bool> SuspendUserAsync(Guid userId, TimeSpan duration, string reason);

        /// <summary>
        /// Get user moderation history
        /// </summary>
        Task<IEnumerable<UserModerationRecord>> GetUserModerationHistoryAsync(Guid userId);

        #endregion

        #region Task Queuing & Assignment

        /// <summary>
        /// Create new moderation task
        /// </summary>
        Task<Guid> CreateModerationTaskAsync(ModerationTaskRequest request);

        /// <summary>
        /// Auto-assign tasks to available moderators
        /// </summary>
        Task<int> AutoAssignTasksAsync(ModeratorSpecialization specialization);

        /// <summary>
        /// Get task assignment statistics
        /// </summary>
        Task<TaskAssignmentStats> GetTaskAssignmentStatsAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Update task priority
        /// </summary>
        Task<bool> UpdateTaskPriorityAsync(Guid taskId, TaskPriority priority);

        #endregion

        #region Performance & Quality

        /// <summary>
        /// Get moderator performance metrics
        /// </summary>
        Task<ModeratorPerformanceMetrics> GetPerformanceMetricsAsync(Guid moderatorId, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get quality score for moderator
        /// </summary>
        Task<QualityScore> GetQualityScoreAsync(Guid moderatorId);

        /// <summary>
        /// Submit peer review of moderation decision
        /// </summary>
        Task<bool> SubmitPeerReviewAsync(Guid taskId, PeerReview review);

        /// <summary>
        /// Get decisions up for peer review
        /// </summary>
        Task<IEnumerable<ModerationDecisionReview>> GetDecisionsForReviewAsync(Guid reviewerId);

        #endregion

        #region Training & Guidelines

        /// <summary>
        /// Get training modules for moderator
        /// </summary>
        Task<IEnumerable<TrainingModule>> GetTrainingModulesAsync(ModeratorSpecialization specialization);

        /// <summary>
        /// Complete training module
        /// </summary>
        Task<bool> CompleteTrainingModuleAsync(Guid moderatorId, Guid moduleId, TrainingResult result);

        /// <summary>
        /// Get certification requirements
        /// </summary>
        Task<IEnumerable<CertificationRequirement>> GetCertificationRequirementsAsync(ModeratorSpecialization specialization);

        /// <summary>
        /// Update moderation guidelines
        /// </summary>
        Task<bool> UpdateModerationGuidelinesAsync(ModeratorSpecialization specialization, ModerationGuidelines guidelines);

        #endregion

        #region Collaboration & Communication

        /// <summary>
        /// Send message to moderator team
        /// </summary>
        Task<bool> SendTeamMessageAsync(ModeratorSpecialization specialization, string subject, string message);

        /// <summary>
        /// Create moderation discussion
        /// </summary>
        Task<Guid> CreateModerationDiscussionAsync(string topic, string description, List<Guid> participants);

        /// <summary>
        /// Get active moderation discussions
        /// </summary>
        Task<IEnumerable<ModerationDiscussion>> GetActiveDiscussionsAsync(Guid moderatorId);

        /// <summary>
        /// Request consultation with expert moderator
        /// </summary>
        Task<bool> RequestConsultationAsync(Guid taskId, Guid expertModeratorId, string question);

        #endregion
    }

    /// <summary>
    /// Moderator dashboard overview
    /// </summary>
    public class ModeratorDashboard
    {
        public Guid ModeratorId { get; set; }
        public ModeratorSpecialization Specialization { get; set; }
        public int PendingTasks { get; set; }
        public int TasksCompletedToday { get; set; }
        public int TasksCompletedThisWeek { get; set; }
        public double AverageResponseTime { get; set; }
        public QualityScore CurrentQualityScore { get; set; }
        public List<ModerationTask> HighPriorityTasks { get; set; } = new();
        public List<ModerationAlert> Alerts { get; set; } = new();
        public Dictionary<ModerationTaskType, int> TasksByType { get; set; } = new();
        public DateTime LastLogin { get; set; }
        public TimeSpan OnlineTime { get; set; }
    }

    /// <summary>
    /// Moderation task
    /// </summary>
    public class ModerationTask
    {
        public Guid Id { get; set; }
        public ModerationTaskType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TaskPriority Priority { get; set; }
        public TaskStatus Status { get; set; }
        public Guid? AssignedTo { get; set; }
        public User? AssignedModerator { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid RelatedEntityId { get; set; }
        public string RelatedEntityType { get; set; } = string.Empty;
        public ModeratorSpecialization RequiredSpecialization { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Moderation task types
    /// </summary>
    public enum ModerationTaskType
    {
        SpotValidation,
        SpotReview,
        ContentModeration,
        UserReport,
        AppealReview,
        QualityAudit,
        PolicyViolation,
        SafetyReview,
        CommunityDispute,
        ExpertConsultation
    }

    /// <summary>
    /// Task priority levels
    /// </summary>
    public enum TaskPriority
    {
        Low,
        Normal,
        High,
        Urgent,
        Critical
    }

    /// <summary>
    /// Task status
    /// </summary>
    public enum TaskStatus
    {
        Pending,
        Assigned,
        InProgress,
        Completed,
        Escalated,
        Rejected,
        Expired
    }

    /// <summary>
    /// Spot validation task
    /// </summary>
    public class SpotValidationTask
    {
        public Guid SpotId { get; set; }
        public Spot Spot { get; set; } = null!;
        public DateTime SubmittedAt { get; set; }
        public User Creator { get; set; } = null!;
        public ValidationUrgency Urgency { get; set; }
        public List<string> SafetyConcerns { get; set; } = new();
        public List<string> QualityIssues { get; set; } = new();
        public bool RequiresExpertReview { get; set; }
        public string? PreviousValidatorNotes { get; set; }
    }

    /// <summary>
    /// Validation urgency levels
    /// </summary>
    public enum ValidationUrgency
    {
        Standard,
        High,
        Safety,
        Emergency
    }

    /// <summary>
    /// Spot validation result
    /// </summary>
    public class SpotValidationResult
    {
        public bool IsApproved { get; set; }
        public List<ValidationCriteria> Criteria { get; set; } = new();
        public string ValidatorNotes { get; set; } = string.Empty;
        public List<string> SafetyRecommendations { get; set; } = new();
        public List<string> ImprovementSuggestions { get; set; } = new();
        public ValidationConfidence Confidence { get; set; }
        public bool RequiresPeerReview { get; set; }
    }

    /// <summary>
    /// Validation criteria
    /// </summary>
    public class ValidationCriteria
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsMet { get; set; }
        public string? Notes { get; set; }
        public int Weight { get; set; } // 1-10
    }

    /// <summary>
    /// Validation confidence levels
    /// </summary>
    public enum ValidationConfidence
    {
        Low,
        Medium,
        High,
        Expert
    }

    /// <summary>
    /// Spot improvement request
    /// </summary>
    public class SpotImprovementRequest
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string? SuggestedSolution { get; set; }
        public DateTime? Deadline { get; set; }
    }

    /// <summary>
    /// Content moderation task
    /// </summary>
    public class ContentModerationTask
    {
        public Guid ContentId { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string ContentSummary { get; set; } = string.Empty;
        public ContentReport Report { get; set; } = null!;
        public DateTime ReportedAt { get; set; }
        public User Reporter { get; set; } = null!;
        public User? ContentAuthor { get; set; }
        public int ReportCount { get; set; }
        public ContentSeverity Severity { get; set; }
    }

    /// <summary>
    /// Content report
    /// </summary>
    public class ContentReport
    {
        public Guid Id { get; set; }
        public ContentReportReason Reason { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Evidence { get; set; }
        public Guid ReporterId { get; set; }
        public DateTime ReportedAt { get; set; }
    }

    /// <summary>
    /// Content report reasons
    /// </summary>
    public enum ContentReportReason
    {
        Inappropriate,
        Spam,
        Harassment,
        Misinformation,
        Copyright,
        Safety,
        Privacy,
        Other
    }

    /// <summary>
    /// Content severity levels
    /// </summary>
    public enum ContentSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Content moderation actions
    /// </summary>
    public enum ContentModerationAction
    {
        Approve,
        Remove,
        Hide,
        EditRequired,
        Warning,
        Escalate
    }

    /// <summary>
    /// User report
    /// </summary>
    public class UserReport
    {
        public Guid Id { get; set; }
        public Guid ReportedUserId { get; set; }
        public User ReportedUser { get; set; } = null!;
        public Guid ReporterId { get; set; }
        public User Reporter { get; set; } = null!;
        public UserReportReason Reason { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime ReportedAt { get; set; }
        public UserReportSeverity Severity { get; set; }
        public List<string> Evidence { get; set; } = new();
    }

    /// <summary>
    /// User report reasons
    /// </summary>
    public enum UserReportReason
    {
        Harassment,
        Spam,
        InappropriateBehavior,
        FakeProfile,
        Safety,
        Terms,
        Other
    }

    /// <summary>
    /// User report severity
    /// </summary>
    public enum UserReportSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// User moderation actions
    /// </summary>
    public enum UserModerationAction
    {
        NoAction,
        Warning,
        TempSuspend,
        Ban,
        RequireVerification,
        RestrictFeatures
    }

    /// <summary>
    /// Warning levels
    /// </summary>
    public enum WarningLevel
    {
        Informal,
        Formal,
        Final
    }

    /// <summary>
    /// Quality score
    /// </summary>
    public class QualityScore
    {
        public int OverallScore { get; set; } // 0-100
        public int AccuracyScore { get; set; }
        public int ConsistencyScore { get; set; }
        public int TimelinessScore { get; set; }
        public int CommunityFeedbackScore { get; set; }
        public int PeerReviewScore { get; set; }
        public DateTime LastCalculated { get; set; }
        public List<QualityFeedback> Feedback { get; set; } = new();
    }

    /// <summary>
    /// Quality feedback
    /// </summary>
    public class QualityFeedback
    {
        public string Category { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime ProvidedAt { get; set; }
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
        public TrainingModuleType Type { get; set; }
        public int EstimatedMinutes { get; set; }
        public bool IsRequired { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TrainingResult? Result { get; set; }
    }

    /// <summary>
    /// Training module types
    /// </summary>
    public enum TrainingModuleType
    {
        Guidelines,
        SafetyProcedures,
        QualityStandards,
        CommunityManagement,
        LegalCompliance,
        ContinuingEducation
    }

    /// <summary>
    /// Training result
    /// </summary>
    public class TrainingResult
    {
        public int Score { get; set; } // 0-100
        public bool Passed { get; set; }
        public TimeSpan CompletionTime { get; set; }
        public DateTime CompletedAt { get; set; }
        public List<string> IncorrectAnswers { get; set; } = new();
    }

    /// <summary>
    /// Moderation alert
    /// </summary>
    public class ModerationAlert
    {
        public Guid Id { get; set; }
        public AlertType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? ActionUrl { get; set; }
    }

    /// <summary>
    /// Alert types
    /// </summary>
    public enum AlertType
    {
        NewTask,
        HighPriority,
        Deadline,
        QualityIssue,
        SystemUpdate,
        TrainingDue
    }

    /// <summary>
    /// Alert severity
    /// </summary>
    public enum AlertSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}