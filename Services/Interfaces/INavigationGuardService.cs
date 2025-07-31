using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Navigation guard service for role-based access control
    /// Implements security checks before navigation occurs
    /// </summary>
    public interface INavigationGuardService
    {
        /// <summary>
        /// Check if current user can navigate to a specific page
        /// </summary>
        /// <param name="pageType">Target page type</param>
        /// <returns>True if navigation is allowed</returns>
        Task<bool> CanNavigateToAsync(Type pageType);

        /// <summary>
        /// Check if current user can navigate to a ViewModel type
        /// </summary>
        /// <typeparam name="TViewModel">Target ViewModel type</typeparam>
        /// <returns>True if navigation is allowed</returns>
        Task<bool> CanNavigateToAsync<TViewModel>();

        /// <summary>
        /// Check if user has required permission for navigation
        /// </summary>
        /// <param name="requiredPermission">Required permission</param>
        /// <returns>True if user has permission</returns>
        bool HasRequiredPermission(UserPermissions requiredPermission);

        /// <summary>
        /// Check if user has required role for navigation
        /// </summary>
        /// <param name="requiredRole">Required account type</param>
        /// <returns>True if user has role</returns>
        bool HasRequiredRole(AccountType requiredRole);

        /// <summary>
        /// Check if user has minimum hierarchy level for navigation
        /// </summary>
        /// <param name="minimumLevel">Minimum hierarchy level (0-3)</param>
        /// <returns>True if user meets minimum level</returns>
        bool HasMinimumHierarchyLevel(int minimumLevel);

        /// <summary>
        /// Get access denied message for user feedback
        /// </summary>
        /// <param name="pageType">Target page type</param>
        /// <returns>User-friendly access denied message</returns>
        string GetAccessDeniedMessage(Type pageType);

        /// <summary>
        /// Redirect user to appropriate page based on their role
        /// </summary>
        /// <returns>Redirect page type or null if no redirect needed</returns>
        Type? GetRoleBasedRedirect();
    }
}