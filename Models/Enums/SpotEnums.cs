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
}
