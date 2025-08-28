using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Models.DTOs
{
    /// <summary>
    /// Missing data transfer objects for user profile service
    /// </summary>

    /// <summary>
    /// Monthly activity summary
    /// </summary>
    public class MonthlyActivitySummary
    {
        public Guid UserId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int SpotsCreated { get; set; }
        public int SpotsValidated { get; set; }
        public int CommunityCentributions { get; set; }
        public int LoginDays { get; set; }
        public TimeSpan TotalActiveTime { get; set; }
        public int AchievementsEarned { get; set; }
        public int PointsGained { get; set; }
        public Dictionary<string, int> ActivityBreakdown { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Identity document
    /// </summary>
    public class IdentityDocument
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string IssuingAuthority { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? DocumentImageUrl { get; set; }
        public bool IsVerified { get; set; }
        public Guid? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerificationNotes { get; set; }
    }

    /// <summary>
    /// Spot contribution information
    /// </summary>
    public class SpotContribution
    {
        public Guid SpotId { get; set; }
        public Spot Spot { get; set; } = null!;
        public string ContributionType { get; set; } = string.Empty; // Created, Validated, Updated
        public DateTime ContributionDate { get; set; }
        public string? Notes { get; set; }
        public int PointsAwarded { get; set; }
        public bool IsRecognized { get; set; } // Featured, highlighted, etc.
    }

    /// <summary>
    /// Moderation activity record
    /// </summary>
    public class ModerationActivity
    {
        public Guid Id { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ActivityDate { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public string? OutcomeSummary { get; set; }
        public int PointsAwarded { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Review activity record
    /// </summary>
    public class ReviewActivity
    {
        public Guid Id { get; set; }
        public Guid ReviewedEntityId { get; set; }
        public string ReviewedEntityType { get; set; } = string.Empty;
        public int Rating { get; set; } // 1-5
        public string? ReviewText { get; set; }
        public DateTime ReviewDate { get; set; }
        public bool IsHelpful { get; set; }
        public int HelpfulVotes { get; set; }
        public bool IsVerifiedReview { get; set; }
    }

    /// <summary>
    /// Notification settings
    /// </summary>
    public class NotificationSettings
    {
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public bool SMSNotifications { get; set; } = false;
        public bool NewFollowerNotifications { get; set; } = true;
        public bool SpotValidationNotifications { get; set; } = true;
        public bool ModerationNotifications { get; set; } = true;
        public bool AchievementNotifications { get; set; } = true;
        public bool SystemNotifications { get; set; } = true;
        public bool MarketingNotifications { get; set; } = false;
        public string PreferredNotificationTime { get; set; } = "09:00";
        public string TimeZone { get; set; } = "UTC";
        public List<string> BlockedNotificationTypes { get; set; } = new();
    }

    /// <summary>
    /// Privacy settings
    /// </summary>
    public class PrivacySettings
    {
        public bool ProfileVisibleToPublic { get; set; } = true;
        public bool ShowEmailToPublic { get; set; } = false;
        public bool ShowLocationToPublic { get; set; } = true;
        public bool ShowActivityToFollowers { get; set; } = true;
        public bool AllowDirectMessages { get; set; } = true;
        public bool AllowFollowing { get; set; } = true;
        public bool ShowOnlineStatus { get; set; } = true;
        public bool AllowSearchByEmail { get; set; } = false;
        public bool AllowSearchByPhone { get; set; } = false;
        public bool ShareDataWithPartners { get; set; } = false;
        public bool PersonalizedAds { get; set; } = true;
        public string DataRetentionPolicy { get; set; } = "Standard";
    }
}