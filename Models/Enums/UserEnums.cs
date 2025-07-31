using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubExplore.Models.Enums
{
    /// <summary>
    /// User account types as defined in requirements section 2.1.3
    /// Hierarchical system from basic user to administrator
    /// </summary>
    public enum AccountType
    {
        /// <summary>
        /// Standard user - Basic functionality, spot creation/consultation
        /// </summary>
        Standard = 0,
        
        /// <summary>
        /// Expert moderator - Specialized by activity type, spot validation
        /// </summary>
        ExpertModerator = 1,
        
        /// <summary>
        /// Verified professional - Commercial entity representative
        /// </summary>
        VerifiedProfessional = 2,
        
        /// <summary>
        /// Platform administrator - Full system management
        /// </summary>
        Administrator = 3
    }

    public enum SubscriptionStatus
    {
        Free,
        Premium,
        PremiumPlus,
        Suspended
    }

    /// <summary>
    /// User expertise levels for diving activities as per requirements section 2.1.2
    /// </summary>
    public enum ExpertiseLevel
    {
        /// <summary>
        /// New to underwater activities
        /// </summary>
        Beginner = 0,
        
        /// <summary>
        /// Some experience, basic skills
        /// </summary>
        Intermediate = 1,
        
        /// <summary>
        /// Significant experience, advanced techniques
        /// </summary>
        Advanced = 2,
        
        /// <summary>
        /// Highly experienced, complex dive planning
        /// </summary>
        Expert = 3,
        
        /// <summary>
        /// Professional level, instructor/guide qualifications
        /// </summary>
        Professional = 4
    }

    /// <summary>
    /// Moderator specialization areas as per requirements section 2.2.1
    /// Expert moderators are specialized by activity type
    /// </summary>
    public enum ModeratorSpecialization
    {
        /// <summary>
        /// No specialization (Standard users)
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Recreational diving specialization
        /// </summary>
        RecreationalDiving = 1,
        
        /// <summary>
        /// Technical diving specialization (deep, cave, wreck)
        /// </summary>
        TechnicalDiving = 2,
        
        /// <summary>
        /// Freediving/Apnea specialization
        /// </summary>
        Freediving = 3,
        
        /// <summary>
        /// Snorkeling and aquatic hiking specialization
        /// </summary>
        SnorkelingHiking = 4,
        
        /// <summary>
        /// Underwater photography specialization
        /// </summary>
        UnderwaterPhotography = 5,
        
        /// <summary>
        /// Dive spots specialization
        /// </summary>
        DiveSpots = 6,
        
        /// <summary>
        /// Freedive spots specialization
        /// </summary>
        FreediveSpots = 7,
        
        /// <summary>
        /// Snorkel spots specialization
        /// </summary>
        SnorkelSpots = 8,
        
        /// <summary>
        /// Safety and regulations specialization
        /// </summary>
        SafetyAndRegulations = 9,
        
        /// <summary>
        /// Marine conservation specialization
        /// </summary>
        MarineConservation = 10,
        
        /// <summary>
        /// Community management specialization
        /// </summary>
        CommunityManagement = 11
    }

    /// <summary>
    /// User permissions based on account type and role
    /// Supports fine-grained access control
    /// </summary>
    [Flags]
    public enum UserPermissions
    {
        /// <summary>
        /// No special permissions
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Can create and edit spots
        /// </summary>
        CreateSpots = 1 << 0,
        
        /// <summary>
        /// Can validate spots in specialized area
        /// </summary>
        ValidateSpots = 1 << 1,
        
        /// <summary>
        /// Can moderate community content
        /// </summary>
        ModerateContent = 1 << 2,
        
        /// <summary>
        /// Can manage organization profile
        /// </summary>
        ManageOrganization = 1 << 3,
        
        /// <summary>
        /// Can access professional features
        /// </summary>
        ProfessionalFeatures = 1 << 4,
        
        /// <summary>
        /// Can nominate moderators
        /// </summary>
        NominateModerators = 1 << 5,
        
        /// <summary>
        /// Can access admin panel
        /// </summary>
        AdminAccess = 1 << 6,
        
        /// <summary>
        /// Can manage user accounts
        /// </summary>
        ManageUsers = 1 << 7,
        
        /// <summary>
        /// Can view all moderation actions
        /// </summary>
        ViewModerationLogs = 1 << 8,
        
        /// <summary>
        /// Can access platform analytics
        /// </summary>
        ViewAnalytics = 1 << 9
    }

    /// <summary>
    /// Moderation status for tracking expert moderator performance
    /// As per requirements section 2.2.3
    /// </summary>
    public enum ModeratorStatus
    {
        /// <summary>
        /// Not a moderator
        /// </summary>
        None = 0,
        
        /// <summary>
        /// New moderator in probationary period
        /// </summary>
        Probationary = 1,
        
        /// <summary>
        /// Active moderator in good standing
        /// </summary>
        Active = 2,
        
        /// <summary>
        /// Temporarily suspended due to performance issues
        /// </summary>
        Suspended = 3,
        
        /// <summary>
        /// Retired moderator (maintains some privileges)
        /// </summary>
        Retired = 4
    }
}