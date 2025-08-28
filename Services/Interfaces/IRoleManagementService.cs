using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Advanced role management service for handling complex role transitions and workflows
    /// </summary>
    public interface IRoleManagementService
    {
        #region Role Transitions

        /// <summary>
        /// Request elevation from Standard to Expert Moderator
        /// </summary>
        Task<RoleTransitionResult> RequestModeratorElevationAsync(Guid userId, ModeratorElevationRequest request);

        /// <summary>
        /// Process moderator elevation request
        /// </summary>
        Task<bool> ProcessModeratorElevationAsync(Guid requestId, bool approved, string processorNotes);

        /// <summary>
        /// Request professional account verification
        /// </summary>
        Task<RoleTransitionResult> RequestProfessionalVerificationAsync(Guid userId, ProfessionalVerificationRequest request);

        /// <summary>
        /// Process professional verification request
        /// </summary>
        Task<bool> ProcessProfessionalVerificationAsync(Guid requestId, bool approved, string processorNotes);

        /// <summary>
        /// Demote user from higher role to lower role
        /// </summary>
        Task<bool> DemoteUserAsync(Guid userId, AccountType newAccountType, string reason);

        /// <summary>
        /// Transfer moderator specialization
        /// </summary>
        Task<bool> TransferModeratorSpecializationAsync(Guid userId, ModeratorSpecialization newSpecialization, string reason);

        #endregion

        #region Moderator Workflow

        /// <summary>
        /// Get pending moderator applications
        /// </summary>
        Task<IEnumerable<ModeratorApplication>> GetPendingModeratorApplicationsAsync();

        /// <summary>
        /// Get moderator application by ID
        /// </summary>
        Task<ModeratorApplication?> GetModeratorApplicationAsync(Guid applicationId);

        /// <summary>
        /// Add endorsement to moderator application
        /// </summary>
        Task<bool> AddModeratorEndorsementAsync(Guid applicationId, ModeratorEndorsement endorsement);

        /// <summary>
        /// Get moderators by specialization
        /// </summary>
        Task<IEnumerable<User>> GetModeratorsBySpecializationAsync(ModeratorSpecialization specialization);

        /// <summary>
        /// Get moderator performance review
        /// </summary>
        Task<ModeratorPerformanceReview> GetModeratorPerformanceAsync(Guid moderatorId, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Schedule moderator review
        /// </summary>
        Task<bool> ScheduleModeratorReviewAsync(Guid moderatorId, DateTime reviewDate, string reason);

        #endregion

        #region Professional Account Management

        /// <summary>
        /// Get pending professional verification requests
        /// </summary>
        Task<IEnumerable<ProfessionalVerificationRequest>> GetPendingProfessionalVerificationsAsync();

        /// <summary>
        /// Get professional verification request by ID
        /// </summary>
        Task<ProfessionalVerificationRequest?> GetProfessionalVerificationRequestAsync(Guid requestId);

        /// <summary>
        /// Update organization association
        /// </summary>
        Task<bool> UpdateOrganizationAssociationAsync(Guid userId, Guid? organizationId);

        /// <summary>
        /// Get users by organization
        /// </summary>
        Task<IEnumerable<User>> GetUsersByOrganizationAsync(Guid organizationId);

        #endregion

        #region Permission Management

        /// <summary>
        /// Grant temporary permissions to user
        /// </summary>
        Task<bool> GrantTemporaryPermissionsAsync(Guid userId, UserPermissions permissions, DateTime expiresAt, string reason);

        /// <summary>
        /// Revoke temporary permissions
        /// </summary>
        Task<bool> RevokeTemporaryPermissionsAsync(Guid userId, UserPermissions permissions);

        /// <summary>
        /// Get user's effective permissions (including temporary)
        /// </summary>
        Task<UserPermissions> GetEffectivePermissionsAsync(Guid userId);

        /// <summary>
        /// Get permission history for user
        /// </summary>
        Task<IEnumerable<PermissionHistoryEntry>> GetPermissionHistoryAsync(Guid userId);

        #endregion

        #region Role Analytics

        /// <summary>
        /// Get role distribution statistics
        /// </summary>
        Task<RoleDistributionStats> GetRoleDistributionAsync();

        /// <summary>
        /// Get role transition statistics
        /// </summary>
        Task<IEnumerable<RoleTransitionStats>> GetRoleTransitionStatsAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get moderator specialization coverage
        /// </summary>
        Task<IEnumerable<SpecializationCoverage>> GetSpecializationCoverageAsync();

        #endregion

        #region Validation & Eligibility

        /// <summary>
        /// Check if user is eligible for moderator elevation
        /// </summary>
        Task<ModeratorEligibilityResult> CheckModeratorEligibilityAsync(Guid userId, ModeratorSpecialization specialization);

        /// <summary>
        /// Check if user is eligible for professional verification
        /// </summary>
        Task<ProfessionalEligibilityResult> CheckProfessionalEligibilityAsync(Guid userId);

        /// <summary>
        /// Validate role transition request
        /// </summary>
        Task<RoleTransitionValidationResult> ValidateRoleTransitionAsync(Guid userId, AccountType targetRole);

        #endregion
    }

    /// <summary>
    /// Role transition result
    /// </summary>
    public class RoleTransitionResult
    {
        public bool Success { get; set; }
        public Guid? RequestId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public RoleTransitionStatus Status { get; set; }
        public DateTime? EstimatedProcessingTime { get; set; }
        public List<string> RequiredSteps { get; set; } = new();
    }

    /// <summary>
    /// Role transition status
    /// </summary>
    public enum RoleTransitionStatus
    {
        Pending,
        UnderReview,
        RequiresMoreInfo,
        Approved,
        Rejected,
        Expired
    }

    /// <summary>
    /// Moderator elevation request
    /// </summary>
    public class ModeratorElevationRequest
    {
        public Guid UserId { get; set; }
        public ModeratorSpecialization Specialization { get; set; }
        public string Justification { get; set; } = string.Empty;
        public List<string> QualificationUrls { get; set; } = new();
        public List<string> ReferenceContacts { get; set; } = new();
        public Dictionary<string, object> Certifications { get; set; } = new();
        public string ExperienceDescription { get; set; } = string.Empty;
        public bool AgreesToModerationGuidelines { get; set; }
        public bool AgreesToTimeCommitment { get; set; }
    }

    /// <summary>
    /// Moderator application with full details
    /// </summary>
    public class ModeratorApplication
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public ModeratorSpecialization Specialization { get; set; }
        public string Justification { get; set; } = string.Empty;
        public ModeratorApplicationState Status { get; set; }
        public DateTime ApplicationDate { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string? ReviewNotes { get; set; }
        public Guid? ReviewerId { get; set; }
        public User? Reviewer { get; set; }
        public List<ModeratorEndorsement> Endorsements { get; set; } = new();
        public List<string> QualificationUrls { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Moderator performance review
    /// </summary>
    public class ModeratorPerformanceReview
    {
        public Guid ModeratorId { get; set; }
        public DateTime ReviewPeriodStart { get; set; }
        public DateTime ReviewPeriodEnd { get; set; }
        public int SpotsValidated { get; set; }
        public int SpotsRejected { get; set; }
        public int CommunityReportsProcessed { get; set; }
        public double AverageResponseTimeHours { get; set; }
        public int AppealsChallenged { get; set; }
        public int AppealsUpheld { get; set; }
        public double AccuracyScore { get; set; }
        public double CommunityFeedbackScore { get; set; }
        public List<string> Achievements { get; set; } = new();
        public List<string> AreasForImprovement { get; set; } = new();
        public ModeratorStatus RecommendedStatus { get; set; }
    }

    /// <summary>
    /// Permission history entry
    /// </summary>
    public class PermissionHistoryEntry
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public UserPermissions PreviousPermissions { get; set; }
        public UserPermissions NewPermissions { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public Guid ChangedBy { get; set; }
        public User ChangedByUser { get; set; } = null!;
        public bool IsTemporary { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Role distribution statistics
    /// </summary>
    public class RoleDistributionStats
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalUsers { get; set; }
        public Dictionary<AccountType, int> UsersByAccountType { get; set; } = new();
        public Dictionary<ModeratorSpecialization, int> ModeratorsBySpecialization { get; set; } = new();
        public Dictionary<ModeratorStatus, int> ModeratorsByStatus { get; set; } = new();
        public int VerifiedProfessionals { get; set; }
        public int PendingVerifications { get; set; }
    }

    /// <summary>
    /// Role transition statistics
    /// </summary>
    public class RoleTransitionStats
    {
        public AccountType FromRole { get; set; }
        public AccountType ToRole { get; set; }
        public int TransitionCount { get; set; }
        public double AverageProcessingDays { get; set; }
        public double ApprovalRate { get; set; }
    }

    /// <summary>
    /// Specialization coverage statistics
    /// </summary>
    public class SpecializationCoverage
    {
        public ModeratorSpecialization Specialization { get; set; }
        public int ActiveModerators { get; set; }
        public int PendingApplications { get; set; }
        public double WorkloadDistribution { get; set; }
        public bool IsCoverageAdequate { get; set; }
        public int RecommendedModeratorCount { get; set; }
    }

    /// <summary>
    /// Moderator eligibility result
    /// </summary>
    public class ModeratorEligibilityResult
    {
        public bool IsEligible { get; set; }
        public List<string> RequirementsMet { get; set; } = new();
        public List<string> RequirementsNotMet { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public int EligibilityScore { get; set; } // 0-100
        public DateTime? EarliestEligibleDate { get; set; }
    }

    /// <summary>
    /// Professional eligibility result
    /// </summary>
    public class ProfessionalEligibilityResult
    {
        public bool IsEligible { get; set; }
        public List<string> RequiredDocuments { get; set; } = new();
        public List<string> OptionalDocuments { get; set; } = new();
        public List<string> EligibilityCriteria { get; set; } = new();
        public int EligibilityScore { get; set; } // 0-100
    }

    /// <summary>
    /// Role transition validation result
    /// </summary>
    public class RoleTransitionValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> ValidationWarnings { get; set; } = new();
        public List<string> RequiredActions { get; set; } = new();
        public AccountType CurrentRole { get; set; }
        public AccountType TargetRole { get; set; }
        public bool RequiresApproval { get; set; }
        public int EstimatedProcessingDays { get; set; }
    }
}