using SubExplore.Models.Enums;

namespace SubExplore.Models.DTOs
{
    /// <summary>
    /// Enhanced authentication state change event arguments
    /// </summary>
    public class AuthenticationStateChangedEventArgs : EventArgs
    {
        public bool IsAuthenticated { get; set; }
        public Domain.User? User { get; set; }
        public string? Reason { get; set; }
        public AuthenticationEventType EventType { get; set; }
    }

    /// <summary>
    /// Authentication event types
    /// </summary>
    public enum AuthenticationEventType
    {
        Login,
        Logout,
        TokenRefresh,
        PasswordChange,
        AccountSuspended,
        AccountReactivated,
        ProfileUpdated,
        RoleChanged
    }
    /// <summary>
    /// Enhanced authentication result with detailed information
    /// </summary>
    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public Domain.User? User { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool RequiresEmailVerification { get; set; }
        public bool RequiresTwoFactor { get; set; }
    }

    /// <summary>
    /// User registration request with comprehensive data
    /// </summary>
    public class UserRegistrationRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Username { get; set; }
        public ExpertiseLevel? ExpertiseLevel { get; set; }
        public Dictionary<string, object>? Certifications { get; set; }
        public bool AcceptTermsAndConditions { get; set; }
        public bool AcceptPrivacyPolicy { get; set; }
        public bool SubscribeToNewsletter { get; set; }
        public string? ReferralCode { get; set; }
    }

    /// <summary>
    /// Password validation result
    /// </summary>
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public int StrengthScore { get; set; } // 0-100
        public List<string> Issues { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        public bool MeetsMinimumRequirements { get; set; }
    }

    /// <summary>
    /// Password reset result from enhanced service
    /// </summary>
    public class PasswordResetResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public PasswordResetResultType ResultType { get; set; }
        public DateTime? EmailSentAt { get; set; }
        public TimeSpan? TokenExpiresIn { get; set; }
        public DateTime? PasswordResetAt { get; set; }
    }

    /// <summary>
    /// Password reset result types
    /// </summary>
    public enum PasswordResetResultType
    {
        EmailSent,
        PasswordReset,
        UserNotFound,
        UserNotVerified,
        DailyLimitReached,
        TokenInvalid,
        TokenExpired,
        SendingFailed
    }

    /// <summary>
    /// Professional verification request
    /// </summary>
    public class ProfessionalVerificationRequest
    {
        public string OrganizationName { get; set; } = string.Empty;
        public string OrganizationWebsite { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string BusinessEmail { get; set; } = string.Empty;
        public string BusinessPhone { get; set; } = string.Empty;
        public List<string> DocumentUrls { get; set; } = new();
        public string AdditionalNotes { get; set; } = string.Empty;
        public ModeratorSpecialization? Specialization { get; set; }
    }

    /// <summary>
    /// Professional verification status
    /// </summary>
    public class ProfessionalVerificationStatus
    {
        public Guid RequestId { get; set; }
        public VerificationStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string? ReviewNotes { get; set; }
        public Guid? ReviewerId { get; set; }
        public List<string> RequiredDocuments { get; set; } = new();
        public List<string> SubmittedDocuments { get; set; } = new();
    }


    /// <summary>
    /// Moderator application status
    /// </summary>
    public class ModeratorApplicationStatus
    {
        public Guid ApplicationId { get; set; }
        public ModeratorApplicationState Status { get; set; }
        public DateTime ApplicationDate { get; set; }
        public DateTime? ReviewDate { get; set; }
        public ModeratorSpecialization Specialization { get; set; }
        public string? Justification { get; set; }
        public string? ReviewNotes { get; set; }
        public Guid? ReviewerId { get; set; }
        public int RequiredEndorsements { get; set; }
        public int CurrentEndorsements { get; set; }
        public List<ModeratorEndorsement> Endorsements { get; set; } = new();
    }

    /// <summary>
    /// Moderator application states
    /// </summary>
    public enum ModeratorApplicationState
    {
        Draft,
        Submitted,
        UnderReview,
        PendingEndorsements,
        Approved,
        Rejected,
        Withdrawn
    }

    /// <summary>
    /// Moderator endorsement
    /// </summary>
    public class ModeratorEndorsement
    {
        public Guid Id { get; set; }
        public Guid EndorserId { get; set; }
        public string EndorserName { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public DateTime EndorsementDate { get; set; }
        public ModeratorSpecialization EndorserSpecialization { get; set; }
    }

    /// <summary>
    /// User search criteria
    /// </summary>
    public class UserSearchCriteria
    {
        public string? SearchTerm { get; set; }
        public AccountType? AccountType { get; set; }
        public ModeratorSpecialization? Specialization { get; set; }
        public ExpertiseLevel? MinExpertiseLevel { get; set; }
        public bool? IsEmailConfirmed { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public int PageSize { get; set; } = 20;
        public int Page { get; set; } = 0;
        public UserSortBy SortBy { get; set; } = UserSortBy.CreatedDate;
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// User sorting options
    /// </summary>
    public enum UserSortBy
    {
        CreatedDate,
        LastLogin,
        Name,
        Email,
        ExpertiseLevel
    }

    /// <summary>
    /// Account activity entry
    /// </summary>
    public class AccountActivity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public AccountActivityType ActivityType { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Account activity types
    /// </summary>
    public enum AccountActivityType
    {
        Login,
        Logout,
        PasswordChange,
        ProfileUpdate,
        EmailVerification,
        TwoFactorEnabled,
        TwoFactorDisabled,
        AccountSuspended,
        AccountReactivated,
        RoleChanged,
        PermissionsChanged
    }

    /// <summary>
    /// User statistics
    /// </summary>
    public class UserStatistics
    {
        public Guid UserId { get; set; }
        public DateTime MemberSince { get; set; }
        public DateTime? LastLogin { get; set; }
        public int TotalSpots { get; set; }
        public int ValidatedSpots { get; set; }
        public int FavoriteSpots { get; set; }
        public int ContributionScore { get; set; }
        public int CommunityRating { get; set; }
        public int ModerationActions { get; set; }
        public Dictionary<string, int> SpecializationMetrics { get; set; } = new();
    }

    /// <summary>
    /// Authentication metrics for admin dashboard
    /// </summary>
    public class AuthenticationMetrics
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalLogins { get; set; }
        public int UniqueUsers { get; set; }
        public int NewRegistrations { get; set; }
        public int PasswordResets { get; set; }
        public int FailedLogins { get; set; }
        public Dictionary<AccountType, int> UsersByType { get; set; } = new();
        public Dictionary<string, int> LoginsByDay { get; set; } = new();
    }

    /// <summary>
    /// Moderator performance metrics
    /// </summary>
    public class ModeratorMetrics
    {
        public Guid ModeratorId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int SpotsValidated { get; set; }
        public int SpotsRejected { get; set; }
        public int CommunityReports { get; set; }
        public double AverageResponseTime { get; set; } // in hours
        public int AppealsChallenged { get; set; }
        public int AppealsUpheld { get; set; }
        public double AccuracyRating { get; set; } // 0-100
        public ModeratorSpecialization Specialization { get; set; }
    }

    /// <summary>
    /// Password reset token validation result
    /// </summary>
    public class PasswordResetTokenValidation
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public PasswordResetResultType ResultType { get; set; }
        public TimeSpan? ExpiresIn { get; set; }
        public int? RemainingAttempts { get; set; }
        public Domain.User? User { get; set; }
    }

    /// <summary>
    /// Password reset statistics
    /// </summary>
    public class PasswordResetStatistics
    {
        public int TotalResetRequests { get; set; }
        public int SuccessfulResets { get; set; }
        public int ExpiredTokens { get; set; }
        public int InvalidAttempts { get; set; }
        public double SuccessRate => TotalResetRequests > 0 ? (double)SuccessfulResets / TotalResetRequests * 100 : 0;
        public DateTime StatisticsGeneratedAt { get; set; } = DateTime.UtcNow;
    }
}