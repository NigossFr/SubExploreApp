using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Role-based authorization service for user permission management
    /// Implements the user hierarchy system from requirements section 2.1.3
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Check if the current user has a specific permission
        /// </summary>
        /// <param name="permission">Permission to check</param>
        /// <returns>True if user has permission</returns>
        bool HasPermission(UserPermissions permission);

        /// <summary>
        /// Check if a specific user has a permission
        /// </summary>
        /// <param name="user">User to check</param>
        /// <param name="permission">Permission to check</param>
        /// <returns>True if user has permission</returns>
        bool HasPermission(User user, UserPermissions permission);

        /// <summary>
        /// Check if current user can perform action on a specific spot
        /// </summary>
        /// <param name="spot">Spot to check permissions for</param>
        /// <param name="action">Action to perform</param>
        /// <returns>True if user can perform action</returns>
        bool CanPerformSpotAction(Spot spot, SpotAction action);

        /// <summary>
        /// Check if specific user can perform action on a specific spot
        /// </summary>
        /// <param name="user">User to check</param>
        /// <param name="spot">Spot to check permissions for</param>
        /// <param name="action">Action to perform</param>
        /// <returns>True if user can perform action</returns>
        bool CanPerformSpotAction(User user, Spot spot, SpotAction action);

        /// <summary>
        /// Check if user can moderate content in a specific specialization area
        /// </summary>
        /// <param name="specialization">Area of specialization</param>
        /// <returns>True if user can moderate in this area</returns>
        bool CanModerateSpecialization(ModeratorSpecialization specialization);

        /// <summary>
        /// Get all permissions for the current user
        /// </summary>
        /// <returns>UserPermissions flags</returns>
        UserPermissions GetCurrentUserPermissions();

        /// <summary>
        /// Get all permissions for a specific user
        /// </summary>
        /// <param name="user">User to get permissions for</param>
        /// <returns>UserPermissions flags</returns>
        UserPermissions GetUserPermissions(User user);

        /// <summary>
        /// Check if current user is in specified role
        /// </summary>
        /// <param name="accountType">Account type to check</param>
        /// <returns>True if user has this account type</returns>
        bool IsInRole(AccountType accountType);

        /// <summary>
        /// Check if current user can elevate to moderator status
        /// </summary>
        /// <param name="specialization">Desired specialization area</param>
        /// <returns>True if elevation is possible</returns>
        Task<bool> CanElevateToModeratorAsync(ModeratorSpecialization specialization);

        /// <summary>
        /// Get hierarchy level for account type (higher number = more permissions)
        /// </summary>
        /// <param name="accountType">Account type</param>
        /// <returns>Hierarchy level (0-3)</returns>
        int GetAccountHierarchyLevel(AccountType accountType);

        /// <summary>
        /// Check if user can access admin features
        /// </summary>
        /// <returns>True if user has admin access</returns>
        bool HasAdminAccess();

        /// <summary>
        /// Check if user can nominate new moderators
        /// </summary>
        /// <returns>True if user can nominate moderators</returns>
        bool CanNominateModerators();
    }

    /// <summary>
    /// Actions that can be performed on spots
    /// </summary>
    public enum SpotAction
    {
        View,
        Create,
        Edit,
        Delete,
        Validate,
        Reject,
        Archive,
        Moderate,
        SafetyReview
    }
}