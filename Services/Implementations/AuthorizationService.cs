using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Implementation of role-based authorization service
    /// Handles the complete user hierarchy system from requirements
    /// </summary>
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<AuthorizationService> _logger;

        public AuthorizationService(
            IAuthenticationService authenticationService,
            ILogger<AuthorizationService> logger)
        {
            _authenticationService = authenticationService;
            _logger = logger;
        }

        public bool HasPermission(UserPermissions permission)
        {
            var currentUser = _authenticationService.CurrentUser;
            return currentUser != null && HasPermission(currentUser, permission);
        }

        public bool HasPermission(User user, UserPermissions permission)
        {
            if (user == null) return false;

            var userPermissions = GetUserPermissions(user);
            return userPermissions.HasFlag(permission);
        }

        public bool CanPerformSpotAction(Spot spot, SpotAction action)
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return false;

            return action switch
            {
                SpotAction.View => true, // Everyone can view public spots
                SpotAction.Create => HasPermission(UserPermissions.CreateSpots),
                SpotAction.Edit => spot.CreatorId == currentUser.Id || HasPermission(UserPermissions.ValidateSpots),
                SpotAction.Delete => spot.CreatorId == currentUser.Id || HasAdminAccess(),
                SpotAction.Validate => CanValidateSpot(spot),
                SpotAction.Reject => CanValidateSpot(spot),
                SpotAction.Archive => HasAdminAccess(),
                _ => false
            };
        }

        public bool CanModerateSpecialization(ModeratorSpecialization specialization)
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return false;

            // Administrators can moderate any specialization
            if (currentUser.AccountType == AccountType.Administrator)
                return true;

            // Expert moderators can only moderate their specialization
            if (currentUser.AccountType == AccountType.ExpertModerator)
            {
                return currentUser.ModeratorSpecialization == specialization &&
                       currentUser.ModeratorStatus == ModeratorStatus.Active;
            }

            return false;
        }

        public UserPermissions GetCurrentUserPermissions()
        {
            var currentUser = _authenticationService.CurrentUser;
            return currentUser != null ? GetUserPermissions(currentUser) : UserPermissions.None;
        }

        public UserPermissions GetUserPermissions(User user)
        {
            if (user == null) return UserPermissions.None;

            // Start with user's explicit permissions
            var permissions = user.Permissions;

            // Add role-based permissions
            permissions |= GetRoleBasedPermissions(user.AccountType);

            // Add moderation permissions if applicable
            if (user.AccountType == AccountType.ExpertModerator && 
                user.ModeratorStatus == ModeratorStatus.Active)
            {
                permissions |= UserPermissions.ValidateSpots | UserPermissions.ModerateContent;
            }

            // Professional features for verified professionals
            if (user.AccountType == AccountType.VerifiedProfessional)
            {
                permissions |= UserPermissions.ProfessionalFeatures | UserPermissions.ManageOrganization;
            }

            // Administrator gets all permissions
            if (user.AccountType == AccountType.Administrator)
            {
                permissions = GetAllPermissions();
            }

            return permissions;
        }

        public bool IsInRole(AccountType accountType)
        {
            var currentUser = _authenticationService.CurrentUser;
            return currentUser?.AccountType == accountType;
        }

        public async Task<bool> CanElevateToModeratorAsync(ModeratorSpecialization specialization)
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return false;

            // Must be Standard user to elevate
            if (currentUser.AccountType != AccountType.Standard) return false;

            // Must have adequate expertise level
            if (currentUser.ExpertiseLevel == null || 
                currentUser.ExpertiseLevel < ExpertiseLevel.Advanced) return false;

            // Must have some platform activity (this could be enhanced with actual metrics)
            var accountAge = DateTime.UtcNow - currentUser.CreatedAt;
            if (accountAge.TotalDays < 30) return false;

            // Additional validation could include:
            // - Number of validated spots created
            // - Community engagement metrics
            // - Certifications in the specialization area

            return true;
        }

        public int GetAccountHierarchyLevel(AccountType accountType)
        {
            return accountType switch
            {
                AccountType.Standard => 0,
                AccountType.ExpertModerator => 1,
                AccountType.VerifiedProfessional => 2,
                AccountType.Administrator => 3,
                _ => 0
            };
        }

        public bool HasAdminAccess()
        {
            return HasPermission(UserPermissions.AdminAccess);
        }

        public bool CanNominateModerators()
        {
            return HasPermission(UserPermissions.NominateModerators);
        }

        /// <summary>
        /// Check if a specific user can perform action on a spot
        /// </summary>
        public bool CanPerformSpotAction(User user, Spot spot, SpotAction action)
        {
            if (user == null) return false;

            return action switch
            {
                SpotAction.View => true, // Everyone can view public spots
                SpotAction.Create => HasPermission(user, UserPermissions.CreateSpots),
                SpotAction.Edit => spot.CreatorId == user.Id || HasPermission(user, UserPermissions.ValidateSpots),
                SpotAction.Delete => spot.CreatorId == user.Id || GetUserPermissions(user).HasFlag(UserPermissions.AdminAccess),
                SpotAction.Validate => HasPermission(user, UserPermissions.ValidateSpots) && CanValidateSpot(user, spot),
                SpotAction.Reject => HasPermission(user, UserPermissions.ValidateSpots) && CanValidateSpot(user, spot),
                SpotAction.Archive => GetUserPermissions(user).HasFlag(UserPermissions.AdminAccess),
                SpotAction.Moderate => HasPermission(user, UserPermissions.ValidateSpots),
                SpotAction.SafetyReview => HasPermission(user, UserPermissions.ValidateSpots),
                _ => false
            };
        }

        /// <summary>
        /// Check if user can validate a specific spot based on specialization
        /// </summary>
        private bool CanValidateSpot(Spot spot)
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return false;
            return CanValidateSpot(currentUser, spot);
        }

        /// <summary>
        /// Check if specific user can validate a specific spot based on specialization
        /// </summary>
        private bool CanValidateSpot(User user, Spot spot)
        {
            if (user == null) return false;

            // Administrators can validate any spot
            if (user.AccountType == AccountType.Administrator)
                return true;

            // Expert moderators can validate spots in their specialization
            if (user.AccountType == AccountType.ExpertModerator &&
                user.ModeratorStatus == ModeratorStatus.Active)
            {
                // This would need to be enhanced to match spot type to moderator specialization
                // For now, active moderators can validate spots
                return HasPermission(user, UserPermissions.ValidateSpots);
            }

            return false;
        }

        /// <summary>
        /// Get role-based permissions for account types
        /// </summary>
        private static UserPermissions GetRoleBasedPermissions(AccountType accountType)
        {
            return accountType switch
            {
                AccountType.Standard => UserPermissions.CreateSpots,
                
                AccountType.ExpertModerator => UserPermissions.CreateSpots | 
                                             UserPermissions.ValidateSpots | 
                                             UserPermissions.ModerateContent |
                                             UserPermissions.ViewModerationLogs,
                
                AccountType.VerifiedProfessional => UserPermissions.CreateSpots | 
                                                   UserPermissions.ProfessionalFeatures | 
                                                   UserPermissions.ManageOrganization,
                
                AccountType.Administrator => GetAllPermissions(),
                
                _ => UserPermissions.None
            };
        }

        /// <summary>
        /// Get all available permissions (for administrators)
        /// </summary>
        private static UserPermissions GetAllPermissions()
        {
            return UserPermissions.CreateSpots |
                   UserPermissions.ValidateSpots |
                   UserPermissions.ModerateContent |
                   UserPermissions.ManageOrganization |
                   UserPermissions.ProfessionalFeatures |
                   UserPermissions.NominateModerators |
                   UserPermissions.AdminAccess |
                   UserPermissions.ManageUsers |
                   UserPermissions.ViewModerationLogs |
                   UserPermissions.ViewAnalytics;
        }
    }
}