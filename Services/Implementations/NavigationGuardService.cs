using Microsoft.Extensions.Logging;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Auth;
using SubExplore.ViewModels.Profile;
using SubExplore.ViewModels.Settings;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Implementation of navigation guard service for role-based access control
    /// </summary>
    public class NavigationGuardService : INavigationGuardService
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<NavigationGuardService> _logger;

        // Define page access requirements
        private readonly Dictionary<Type, NavigationRequirement> _pageRequirements = new()
        {
            // Authentication pages - available to unauthenticated users
            { typeof(LoginViewModel), new NavigationRequirement { AllowUnauthenticated = true } },
            { typeof(RegistrationViewModel), new NavigationRequirement { AllowUnauthenticated = true } },

            // Standard user pages - require authentication only
            { typeof(UserProfileViewModel), new NavigationRequirement { RequireAuthentication = true } },

            // Moderator pages - require moderator role
            { typeof(DatabaseTestViewModel), new NavigationRequirement 
                { 
                    RequiredPermissions = UserPermissions.ValidateSpots,
                    RequiredRole = AccountType.ExpertModerator
                }
            },

            // Admin pages - require admin permissions
            { typeof(UserStatsViewModel), new NavigationRequirement 
                { 
                    RequiredPermissions = UserPermissions.AdminAccess,
                    MinimumHierarchyLevel = 3
                }
            }
        };

        public NavigationGuardService(
            IAuthenticationService authenticationService,
            IAuthorizationService authorizationService,
            ILogger<NavigationGuardService> logger)
        {
            _authenticationService = authenticationService;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        public async Task<bool> CanNavigateToAsync(Type pageType)
        {
            try
            {
                // Check if page has specific requirements
                if (!_pageRequirements.TryGetValue(pageType, out var requirements))
                {
                    // Default: allow navigation if authenticated
                    return _authenticationService.IsAuthenticated;
                }

                // Check if unauthenticated access is allowed
                if (requirements.AllowUnauthenticated)
                {
                    return true;
                }

                // Check authentication requirement
                if (requirements.RequireAuthentication && !_authenticationService.IsAuthenticated)
                {
                    _logger.LogWarning("Navigation denied to {PageType}: User not authenticated", pageType.Name);
                    return false;
                }

                // Validate authentication state
                if (!await _authenticationService.ValidateAuthenticationAsync())
                {
                    _logger.LogWarning("Navigation denied to {PageType}: Authentication validation failed", pageType.Name);
                    return false;
                }

                // Check role requirement
                if (requirements.RequiredRole.HasValue && 
                    !_authorizationService.IsInRole(requirements.RequiredRole.Value))
                {
                    _logger.LogWarning("Navigation denied to {PageType}: Required role {RequiredRole} not met", 
                        pageType.Name, requirements.RequiredRole.Value);
                    return false;
                }

                // Check permission requirement
                if (requirements.RequiredPermissions.HasValue && 
                    !_authorizationService.HasPermission(requirements.RequiredPermissions.Value))
                {
                    _logger.LogWarning("Navigation denied to {PageType}: Required permissions {RequiredPermissions} not met", 
                        pageType.Name, requirements.RequiredPermissions.Value);
                    return false;
                }

                // Check hierarchy level requirement
                if (requirements.MinimumHierarchyLevel.HasValue)
                {
                    var currentUser = _authenticationService.CurrentUser;
                    if (currentUser == null || 
                        _authorizationService.GetAccountHierarchyLevel(currentUser.AccountType) < requirements.MinimumHierarchyLevel.Value)
                    {
                        _logger.LogWarning("Navigation denied to {PageType}: Minimum hierarchy level {MinimumLevel} not met", 
                            pageType.Name, requirements.MinimumHierarchyLevel.Value);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking navigation permission for {PageType}", pageType.Name);
                return false;
            }
        }

        public async Task<bool> CanNavigateToAsync<TViewModel>()
        {
            return await CanNavigateToAsync(typeof(TViewModel));
        }

        public bool HasRequiredPermission(UserPermissions requiredPermission)
        {
            return _authorizationService.HasPermission(requiredPermission);
        }

        public bool HasRequiredRole(AccountType requiredRole)
        {
            return _authorizationService.IsInRole(requiredRole);
        }

        public bool HasMinimumHierarchyLevel(int minimumLevel)
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return false;

            return _authorizationService.GetAccountHierarchyLevel(currentUser.AccountType) >= minimumLevel;
        }

        public string GetAccessDeniedMessage(Type pageType)
        {
            if (!_pageRequirements.TryGetValue(pageType, out var requirements))
            {
                return "You do not have permission to access this page.";
            }

            if (requirements.RequireAuthentication && !_authenticationService.IsAuthenticated)
            {
                return "Please log in to access this page.";
            }

            if (requirements.RequiredRole.HasValue)
            {
                return $"This page requires {GetRoleDisplayName(requirements.RequiredRole.Value)} access.";
            }

            if (requirements.RequiredPermissions.HasValue)
            {
                return "You do not have the required permissions to access this page.";
            }

            if (requirements.MinimumHierarchyLevel.HasValue)
            {
                return "This page requires elevated access privileges.";
            }

            return "Access denied.";
        }

        public Type? GetRoleBasedRedirect()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null)
            {
                return typeof(LoginViewModel);
            }

            // Redirect based on user role (could be used for role-specific dashboards)
            return currentUser.AccountType switch
            {
                AccountType.Administrator => null, // Admins can go anywhere
                AccountType.ExpertModerator => null, // Moderators have broad access
                AccountType.VerifiedProfessional => null, // Professionals have standard access
                AccountType.Standard => null, // Standard users have basic access
                _ => typeof(UserProfileViewModel) // Default redirect
            };
        }

        private string GetRoleDisplayName(AccountType accountType)
        {
            return accountType switch
            {
                AccountType.Standard => "Standard User",
                AccountType.ExpertModerator => "Expert Moderator",
                AccountType.VerifiedProfessional => "Verified Professional",
                AccountType.Administrator => "Administrator",
                _ => "Unknown Role"
            };
        }
    }

    /// <summary>
    /// Navigation access requirements for a page or ViewModel
    /// </summary>
    public class NavigationRequirement
    {
        /// <summary>
        /// Allow access without authentication
        /// </summary>
        public bool AllowUnauthenticated { get; set; } = false;

        /// <summary>
        /// Require user to be authenticated
        /// </summary>
        public bool RequireAuthentication { get; set; } = true;

        /// <summary>
        /// Required user role (if any)
        /// </summary>
        public AccountType? RequiredRole { get; set; }

        /// <summary>
        /// Required permissions (if any)
        /// </summary>
        public UserPermissions? RequiredPermissions { get; set; }

        /// <summary>
        /// Minimum hierarchy level required (0-3)
        /// </summary>
        public int? MinimumHierarchyLevel { get; set; }
    }
}