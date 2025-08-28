using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Enhanced user profile management service
    /// Comprehensive profile features including achievements, preferences, and social features
    /// </summary>
    public interface IEnhancedUserProfileService
    {
        #region Profile Management

        /// <summary>
        /// Get comprehensive user profile
        /// </summary>
        Task<UserProfile?> GetUserProfileAsync(Guid userId, bool includePrivateInfo = false);

        /// <summary>
        /// Update user profile with validation
        /// </summary>
        Task<ProfileUpdateResult> UpdateUserProfileAsync(Guid userId, UserProfileUpdateRequest request);

        /// <summary>
        /// Upload and update user avatar
        /// </summary>
        Task<AvatarUpdateResult> UpdateAvatarAsync(Guid userId, byte[] imageData, string contentType);

        /// <summary>
        /// Delete user avatar
        /// </summary>
        Task<bool> DeleteAvatarAsync(Guid userId);

        /// <summary>
        /// Get user profile visibility settings
        /// </summary>
        Task<ProfileVisibility> GetProfileVisibilityAsync(Guid userId);

        /// <summary>
        /// Update profile visibility settings
        /// </summary>
        Task<bool> UpdateProfileVisibilityAsync(Guid userId, ProfileVisibility visibility);

        #endregion

        #region Experience & Certifications

        /// <summary>
        /// Update user expertise level with validation
        /// </summary>
        Task<bool> UpdateExpertiseLevelAsync(Guid userId, ExpertiseLevel expertiseLevel, List<CertificationInfo> certifications);

        /// <summary>
        /// Add certification to user profile
        /// </summary>
        Task<bool> AddCertificationAsync(Guid userId, CertificationInfo certification);

        /// <summary>
        /// Remove certification from user profile
        /// </summary>
        Task<bool> RemoveCertificationAsync(Guid userId, Guid certificationId);

        /// <summary>
        /// Verify certification (admin/moderator only)
        /// </summary>
        Task<bool> VerifyCertificationAsync(Guid certificationId, bool verified, string notes, Guid verifiedBy);

        /// <summary>
        /// Get user's diving experience summary
        /// </summary>
        Task<DivingExperience> GetDivingExperienceAsync(Guid userId);

        /// <summary>
        /// Update diving experience details
        /// </summary>
        Task<bool> UpdateDivingExperienceAsync(Guid userId, DivingExperience experience);

        #endregion

        #region Achievements & Gamification

        /// <summary>
        /// Get user achievements
        /// </summary>
        Task<IEnumerable<UserAchievement>> GetUserAchievementsAsync(Guid userId);

        /// <summary>
        /// Award achievement to user
        /// </summary>
        Task<bool> AwardAchievementAsync(Guid userId, Achievement achievement);

        /// <summary>
        /// Get user's contribution score
        /// </summary>
        Task<ContributionScore> GetContributionScoreAsync(Guid userId);

        /// <summary>
        /// Update contribution points
        /// </summary>
        Task<bool> UpdateContributionPointsAsync(Guid userId, int points, string reason);

        /// <summary>
        /// Get user's ranking in community
        /// </summary>
        Task<UserRanking> GetUserRankingAsync(Guid userId, RankingCategory category);

        /// <summary>
        /// Get leaderboard for category
        /// </summary>
        Task<IEnumerable<UserRankingEntry>> GetLeaderboardAsync(RankingCategory category, int limit = 50);

        #endregion

        #region Preferences & Settings

        /// <summary>
        /// Get user preferences
        /// </summary>
        Task<UserPreferences?> GetUserPreferencesAsync(Guid userId);

        /// <summary>
        /// Update user preferences
        /// </summary>
        Task<bool> UpdateUserPreferencesAsync(Guid userId, UserPreferences preferences);

        /// <summary>
        /// Get notification settings
        /// </summary>
        Task<NotificationSettings> GetNotificationSettingsAsync(Guid userId);

        /// <summary>
        /// Update notification settings
        /// </summary>
        Task<bool> UpdateNotificationSettingsAsync(Guid userId, NotificationSettings settings);

        /// <summary>
        /// Get privacy settings
        /// </summary>
        Task<PrivacySettings> GetPrivacySettingsAsync(Guid userId);

        /// <summary>
        /// Update privacy settings
        /// </summary>
        Task<bool> UpdatePrivacySettingsAsync(Guid userId, PrivacySettings settings);

        #endregion

        #region Social Features

        /// <summary>
        /// Follow another user
        /// </summary>
        Task<bool> FollowUserAsync(Guid followerId, Guid followeeId);

        /// <summary>
        /// Unfollow a user
        /// </summary>
        Task<bool> UnfollowUserAsync(Guid followerId, Guid followeeId);

        /// <summary>
        /// Get user's followers
        /// </summary>
        Task<IEnumerable<UserSummary>> GetFollowersAsync(Guid userId, int page = 0, int pageSize = 20);

        /// <summary>
        /// Get users that user is following
        /// </summary>
        Task<IEnumerable<UserSummary>> GetFollowingAsync(Guid userId, int page = 0, int pageSize = 20);

        /// <summary>
        /// Block another user
        /// </summary>
        Task<bool> BlockUserAsync(Guid blockerId, Guid blockedId, string reason);

        /// <summary>
        /// Unblock a user
        /// </summary>
        Task<bool> UnblockUserAsync(Guid blockerId, Guid blockedId);

        /// <summary>
        /// Get blocked users
        /// </summary>
        Task<IEnumerable<UserSummary>> GetBlockedUsersAsync(Guid userId);

        #endregion

        #region Activity & History

        /// <summary>
        /// Get user's activity feed
        /// </summary>
        Task<IEnumerable<UserActivity>> GetUserActivityAsync(Guid userId, int page = 0, int pageSize = 20);

        /// <summary>
        /// Log user activity
        /// </summary>
        Task<bool> LogUserActivityAsync(Guid userId, UserActivityType activityType, string description, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Get user's spot contributions
        /// </summary>
        Task<IEnumerable<SpotContribution>> GetSpotContributionsAsync(Guid userId);

        /// <summary>
        /// Get user's moderation history (moderators only)
        /// </summary>
        Task<IEnumerable<ModerationActivity>> GetModerationHistoryAsync(Guid moderatorId, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get user's review history
        /// </summary>
        Task<IEnumerable<ReviewActivity>> GetReviewHistoryAsync(Guid userId);

        #endregion

        #region Search & Discovery

        /// <summary>
        /// Search users with advanced filters
        /// </summary>
        Task<SearchResult<UserSummary>> SearchUsersAsync(UserSearchRequest request);

        /// <summary>
        /// Get suggested users to follow
        /// </summary>
        Task<IEnumerable<UserSuggestion>> GetUserSuggestionsAsync(Guid userId, int limit = 10);

        /// <summary>
        /// Get users by location
        /// </summary>
        Task<IEnumerable<UserSummary>> GetUsersByLocationAsync(double latitude, double longitude, double radiusKm, int limit = 20);

        /// <summary>
        /// Get expert users by specialization
        /// </summary>
        Task<IEnumerable<UserSummary>> GetExpertUsersBySpecializationAsync(ModeratorSpecialization specialization, int limit = 20);

        #endregion

        #region Profile Statistics

        /// <summary>
        /// Get comprehensive user statistics
        /// </summary>
        Task<UserStatistics> GetUserStatisticsAsync(Guid userId);

        /// <summary>
        /// Get user's impact metrics
        /// </summary>
        Task<UserImpactMetrics> GetUserImpactMetricsAsync(Guid userId);

        /// <summary>
        /// Get monthly activity summary
        /// </summary>
        Task<MonthlyActivitySummary> GetMonthlyActivityAsync(Guid userId, int year, int month);

        /// <summary>
        /// Generate user analytics report
        /// </summary>
        Task<byte[]> GenerateUserReportAsync(Guid userId, ReportType reportType);

        #endregion

        #region Profile Validation

        /// <summary>
        /// Validate profile completeness
        /// </summary>
        Task<ProfileCompletenessResult> ValidateProfileCompletenessAsync(Guid userId);

        /// <summary>
        /// Get profile enhancement suggestions
        /// </summary>
        Task<IEnumerable<ProfileSuggestion>> GetProfileSuggestionsAsync(Guid userId);

        /// <summary>
        /// Verify user's identity documents (admin only)
        /// </summary>
        Task<bool> VerifyIdentityDocumentsAsync(Guid userId, List<IdentityDocument> documents, bool approved, string notes);

        #endregion
    }

    #region Data Transfer Objects

    /// <summary>
    /// Comprehensive user profile
    /// </summary>
    public class UserProfile
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? Website { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public AccountType AccountType { get; set; }
        public ExpertiseLevel? ExpertiseLevel { get; set; }
        public ModeratorSpecialization ModeratorSpecialization { get; set; }
        public ContributionScore ContributionScore { get; set; } = new();
        public List<CertificationInfo> Certifications { get; set; } = new();
        public DivingExperience DivingExperience { get; set; } = new();
        public ProfileVisibility Visibility { get; set; } = new();
        public UserStatistics Statistics { get; set; } = new();
        public bool IsFollowing { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime ProfileLastUpdated { get; set; }
    }

    /// <summary>
    /// User profile update request
    /// </summary>
    public class UserProfileUpdateRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? Website { get; set; }
        public ExpertiseLevel? ExpertiseLevel { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? EmergencyContact { get; set; }
        public Dictionary<string, object>? CustomFields { get; set; }
    }

    /// <summary>
    /// Profile update result
    /// </summary>
    public class ProfileUpdateResult
    {
        public bool Success { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> UpdatedFields { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Avatar update result
    /// </summary>
    public class AvatarUpdateResult
    {
        public bool Success { get; set; }
        public string? AvatarUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public long FileSize { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Profile visibility settings
    /// </summary>
    public class ProfileVisibility
    {
        public bool ShowEmail { get; set; } = false;
        public bool ShowLocation { get; set; } = true;
        public bool ShowExpertiseLevel { get; set; } = true;
        public bool ShowCertifications { get; set; } = true;
        public bool ShowStatistics { get; set; } = true;
        public bool ShowActivity { get; set; } = true;
        public bool ShowFollowers { get; set; } = true;
        public bool AllowMessages { get; set; } = true;
        public bool AllowFollowing { get; set; } = true;
        public PrivacyLevel OverallPrivacy { get; set; } = PrivacyLevel.Public;
    }

    /// <summary>
    /// Privacy levels
    /// </summary>
    public enum PrivacyLevel
    {
        Public,
        FriendsOnly,
        Private
    }

    /// <summary>
    /// Certification information
    /// </summary>
    public class CertificationInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IssuingOrganization { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? CertificationNumber { get; set; }
        public string? DocumentUrl { get; set; }
        public bool IsVerified { get; set; }
        public Guid? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public CertificationType Type { get; set; }
        public CertificationLevel Level { get; set; }
    }

    /// <summary>
    /// Certification types
    /// </summary>
    public enum CertificationType
    {
        OpenWater,
        Advanced,
        Rescue,
        Divemaster,
        Instructor,
        Technical,
        Specialty,
        FirstAid,
        Safety,
        Other
    }

    /// <summary>
    /// Certification levels
    /// </summary>
    public enum CertificationLevel
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert,
        Instructor
    }

    /// <summary>
    /// Diving experience details
    /// </summary>
    public class DivingExperience
    {
        public int TotalDives { get; set; }
        public DateTime? FirstDive { get; set; }
        public DateTime? LastDive { get; set; }
        public double MaxDepthMeters { get; set; }
        public List<DivingEnvironment> Environments { get; set; } = new();
        public List<string> Specialties { get; set; } = new();
        public Dictionary<string, int> DivesByLocation { get; set; } = new();
        public string? PreferredEquipment { get; set; }
        public string? DivingGoals { get; set; }
        public List<EmergencyTraining> EmergencyTraining { get; set; } = new();
    }

    /// <summary>
    /// Diving environments
    /// </summary>
    public enum DivingEnvironment
    {
        Ocean,
        Lake,
        River,
        Cave,
        Wreck,
        Reef,
        Shore,
        Boat,
        Night,
        Deep,
        Technical
    }

    /// <summary>
    /// Emergency training record
    /// </summary>
    public class EmergencyTraining
    {
        public string Type { get; set; } = string.Empty;
        public DateTime CompletedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? IssuingOrganization { get; set; }
    }

    /// <summary>
    /// User achievement
    /// </summary>
    public class UserAchievement
    {
        public Guid Id { get; set; }
        public Achievement Achievement { get; set; } = null!;
        public DateTime EarnedAt { get; set; }
        public int Progress { get; set; } // 0-100
        public bool IsUnlocked { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Achievement definition
    /// </summary>
    public class Achievement
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public AchievementCategory Category { get; set; }
        public AchievementRarity Rarity { get; set; }
        public int Points { get; set; }
        public List<AchievementCriteria> Criteria { get; set; } = new();
    }

    /// <summary>
    /// Achievement categories
    /// </summary>
    public enum AchievementCategory
    {
        Contribution,
        Exploration,
        Community,
        Safety,
        Knowledge,
        Leadership,
        Special
    }

    /// <summary>
    /// Achievement rarity
    /// </summary>
    public enum AchievementRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// Achievement criteria
    /// </summary>
    public class AchievementCriteria
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Target { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// User contribution score
    /// </summary>
    public class ContributionScore
    {
        public int TotalPoints { get; set; }
        public int SpotsCreated { get; set; }
        public int SpotsValidated { get; set; }
        public int CommunityHelpful { get; set; }
        public int QualityRating { get; set; } // 0-100
        public ContributionLevel Level { get; set; }
        public int PointsToNextLevel { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Contribution levels
    /// </summary>
    public enum ContributionLevel
    {
        Newcomer,
        Contributor,
        Regular,
        Veteran,
        Expert,
        Master,
        Legend
    }

    /// <summary>
    /// User ranking information
    /// </summary>
    public class UserRanking
    {
        public int Rank { get; set; }
        public int TotalUsers { get; set; }
        public double Percentile { get; set; }
        public RankingCategory Category { get; set; }
        public int Score { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    /// <summary>
    /// Ranking categories
    /// </summary>
    public enum RankingCategory
    {
        Overall,
        Contribution,
        SpotCreation,
        Validation,
        Community,
        Safety,
        Knowledge
    }

    /// <summary>
    /// Leaderboard entry
    /// </summary>
    public class UserRankingEntry
    {
        public int Rank { get; set; }
        public UserSummary User { get; set; } = null!;
        public int Score { get; set; }
        public string? Badge { get; set; }
        public DateTime LastActive { get; set; }
    }

    /// <summary>
    /// User summary for lists
    /// </summary>
    public class UserSummary
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public ExpertiseLevel? ExpertiseLevel { get; set; }
        public AccountType AccountType { get; set; }
        public ModeratorSpecialization ModeratorSpecialization { get; set; }
        public string? Location { get; set; }
        public int ContributionPoints { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
    }

    /// <summary>
    /// User search request
    /// </summary>
    public class UserSearchRequest
    {
        public string? SearchTerm { get; set; }
        public ExpertiseLevel? MinExpertiseLevel { get; set; }
        public ExpertiseLevel? MaxExpertiseLevel { get; set; }
        public AccountType? AccountType { get; set; }
        public ModeratorSpecialization? Specialization { get; set; }
        public string? Location { get; set; }
        public double? LocationRadius { get; set; }
        public bool? IsOnline { get; set; }
        public DateTime? ActiveSince { get; set; }
        public UserSearchSort SortBy { get; set; } = UserSearchSort.Relevance;
        public bool SortDescending { get; set; } = true;
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// User search sorting options
    /// </summary>
    public enum UserSearchSort
    {
        Relevance,
        LastActive,
        ContributionPoints,
        JoinDate,
        Name
    }

    /// <summary>
    /// Search result wrapper
    /// </summary>
    public class SearchResult<T>
    {
        public IEnumerable<T> Results { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// User suggestion for following
    /// </summary>
    public class UserSuggestion
    {
        public UserSummary User { get; set; } = null!;
        public string SuggestionReason { get; set; } = string.Empty;
        public double MatchScore { get; set; } // 0-1
        public List<string> CommonInterests { get; set; } = new();
        public int MutualConnections { get; set; }
    }

    /// <summary>
    /// User activity entry
    /// </summary>
    public class UserActivity
    {
        public Guid Id { get; set; }
        public UserActivityType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public bool IsPublic { get; set; }
    }

    /// <summary>
    /// User activity types
    /// </summary>
    public enum UserActivityType
    {
        SpotCreated,
        SpotValidated,
        SpotFavorited,
        UserFollowed,
        AchievementEarned,
        CertificationAdded,
        ProfileUpdated,
        ReviewPosted,
        CommentAdded,
        Login,
        Other
    }

    /// <summary>
    /// User impact metrics
    /// </summary>
    public class UserImpactMetrics
    {
        public int SpotsCreated { get; set; }
        public int SpotsValidated { get; set; }
        public int UsersHelped { get; set; }
        public int CommunityContributions { get; set; }
        public double AverageRating { get; set; }
        public int SafetyReports { get; set; }
        public int KnowledgeShared { get; set; }
        public ImpactLevel ImpactLevel { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    /// <summary>
    /// Impact levels
    /// </summary>
    public enum ImpactLevel
    {
        Minimal,
        Low,
        Medium,
        High,
        Exceptional
    }

    /// <summary>
    /// Profile completeness result
    /// </summary>
    public class ProfileCompletenessResult
    {
        public int CompletionPercentage { get; set; }
        public List<string> CompletedSections { get; set; } = new();
        public List<string> MissingSections { get; set; } = new();
        public List<ProfileSuggestion> Suggestions { get; set; } = new();
        public bool IsConsideredComplete { get; set; }
    }

    /// <summary>
    /// Profile improvement suggestion
    /// </summary>
    public class ProfileSuggestion
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SuggestionPriority Priority { get; set; }
        public SuggestionCategory Category { get; set; }
        public string? ActionUrl { get; set; }
        public int PointsValue { get; set; }
    }

    /// <summary>
    /// Suggestion priorities
    /// </summary>
    public enum SuggestionPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Suggestion categories
    /// </summary>
    public enum SuggestionCategory
    {
        BasicInfo,
        Experience,
        Certifications,
        Safety,
        Community,
        Privacy
    }

    /// <summary>
    /// Report types for user analytics
    /// </summary>
    public enum ReportType
    {
        ProfileSummary,
        ActivityReport,
        ContributionReport,
        AchievementReport,
        PrivacyReport
    }

    #endregion
}