using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubExplore.Models.Enums
{
    public enum DifficultyLevel
    {
        Beginner = 1,
        Intermediate = 2,
        Advanced = 3,
        Expert = 4,
        TechnicalOnly = 5
    }

    public enum SpotValidationStatus
    {
        Draft,
        Pending,
        UnderReview,
        NeedsRevision,
        SafetyReview,
        Approved,
        Rejected,
        Archived
    }

    public enum CurrentStrength
    {
        None,
        Weak,
        Moderate,
        Strong,
        Extreme
    }

    public enum SpotReportType
    {
        SafetyConcern = 1,
        InaccurateInformation = 2,
        InappropriateContent = 3,
        AccessIssues = 4,
        EnvironmentalDamage = 5,
        PrivacyViolation = 6,
        Spam = 7,
        Other = 99
    }

    public enum SpotReportStatus
    {
        Pending = 1,
        UnderReview = 2,
        InProgress = 3,
        Resolved = 4,
        Dismissed = 5,
        RequiresMoreInfo = 6
    }

    public enum SpotReportSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4,
        Emergency = 5
    }
}
