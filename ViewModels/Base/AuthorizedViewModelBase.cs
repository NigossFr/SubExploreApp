using CommunityToolkit.Mvvm.ComponentModel;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;

namespace SubExplore.ViewModels.Base
{
    /// <summary>
    /// Base ViewModel for pages requiring authorization checks
    /// Provides role-based UI control and permission checking
    /// </summary>
    public abstract partial class AuthorizedViewModelBase : ViewModelBase
    {
        protected readonly IAuthorizationService _authorizationService;
        protected readonly IAuthenticationService _authenticationService;

        [ObservableProperty]
        private bool _canCreateSpots;

        [ObservableProperty]
        private bool _canValidateSpots;

        [ObservableProperty]
        private bool _canModerateContent;

        [ObservableProperty]
        private bool _canAccessAdminFeatures;

        [ObservableProperty]
        private bool _canAccessProfessionalFeatures;

        [ObservableProperty]
        private bool _canNominateModerators;

        [ObservableProperty]
        private bool _isStandardUser;

        [ObservableProperty]
        private bool _isExpertModerator;

        [ObservableProperty]
        private bool _isVerifiedProfessional;

        [ObservableProperty]
        private bool _isAdministrator;

        [ObservableProperty]
        private string _userRoleDisplayName = "Standard User";

        [ObservableProperty]
        private string _moderatorSpecializationText = "";

        protected AuthorizedViewModelBase(
            IAuthorizationService authorizationService,
            IAuthenticationService authenticationService,
            IDialogService dialogService = null,
            INavigationService navigationService = null)
            : base(dialogService, navigationService)
        {
            _authorizationService = authorizationService;
            _authenticationService = authenticationService;

            // Subscribe to authentication state changes
            _authenticationService.StateChanged += OnAuthenticationStateChanged;
        }

        /// <summary>
        /// Update UI permissions based on current user
        /// Call this in InitializeAsync or when permissions change
        /// </summary>
        protected virtual void UpdateUIPermissions()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null)
            {
                ResetPermissions();
                return;
            }

            // Update permission flags
            CanCreateSpots = _authorizationService.HasPermission(UserPermissions.CreateSpots);
            CanValidateSpots = _authorizationService.HasPermission(UserPermissions.ValidateSpots);
            CanModerateContent = _authorizationService.HasPermission(UserPermissions.ModerateContent);
            CanAccessAdminFeatures = _authorizationService.HasPermission(UserPermissions.AdminAccess);
            CanAccessProfessionalFeatures = _authorizationService.HasPermission(UserPermissions.ProfessionalFeatures);
            CanNominateModerators = _authorizationService.HasPermission(UserPermissions.NominateModerators);

            // Update role flags
            IsStandardUser = _authorizationService.IsInRole(AccountType.Standard);
            IsExpertModerator = _authorizationService.IsInRole(AccountType.ExpertModerator);
            IsVerifiedProfessional = _authorizationService.IsInRole(AccountType.VerifiedProfessional);
            IsAdministrator = _authorizationService.IsInRole(AccountType.Administrator);

            // Update display names
            UserRoleDisplayName = GetRoleDisplayName(currentUser.AccountType);
            ModeratorSpecializationText = GetSpecializationDisplayName(currentUser.ModeratorSpecialization);
        }

        /// <summary>
        /// Check if current user can perform a specific action
        /// </summary>
        /// <param name="permission">Permission to check</param>
        /// <returns>True if user has permission</returns>
        protected bool HasPermission(UserPermissions permission)
        {
            return _authorizationService.HasPermission(permission);
        }

        /// <summary>
        /// Check if current user is in a specific role
        /// </summary>
        /// <param name="accountType">Account type to check</param>
        /// <returns>True if user has this role</returns>
        protected bool IsInRole(AccountType accountType)
        {
            return _authorizationService.IsInRole(accountType);
        }

        /// <summary>
        /// Get hierarchy level for account comparison
        /// </summary>
        /// <param name="accountType">Account type</param>
        /// <returns>Hierarchy level (0-3)</returns>
        protected int GetHierarchyLevel(AccountType accountType)
        {
            return _authorizationService.GetAccountHierarchyLevel(accountType);
        }

        /// <summary>
        /// Check if current user has higher hierarchy than target account type
        /// </summary>
        /// <param name="targetAccountType">Target account type</param>
        /// <returns>True if current user has higher hierarchy</returns>
        protected bool HasHigherHierarchy(AccountType targetAccountType)
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return false;

            return GetHierarchyLevel(currentUser.AccountType) > GetHierarchyLevel(targetAccountType);
        }

        private void ResetPermissions()
        {
            CanCreateSpots = false;
            CanValidateSpots = false;
            CanModerateContent = false;
            CanAccessAdminFeatures = false;
            CanAccessProfessionalFeatures = false;
            CanNominateModerators = false;
            
            IsStandardUser = false;
            IsExpertModerator = false;
            IsVerifiedProfessional = false;
            IsAdministrator = false;
            
            UserRoleDisplayName = "Not Authenticated";
            ModeratorSpecializationText = "";
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

        private string GetSpecializationDisplayName(ModeratorSpecialization specialization)
        {
            return specialization switch
            {
                ModeratorSpecialization.None => "",
                ModeratorSpecialization.RecreationalDiving => "Recreational Diving",
                ModeratorSpecialization.TechnicalDiving => "Technical Diving",
                ModeratorSpecialization.Freediving => "Freediving",
                ModeratorSpecialization.SnorkelingHiking => "Snorkeling & Hiking",
                ModeratorSpecialization.UnderwaterPhotography => "Underwater Photography",
                _ => "Unknown Specialization"
            };
        }

        private async void OnAuthenticationStateChanged(object? sender, AuthenticationStateChangedEventArgs e)
        {
            // Update UI permissions when authentication state changes
            UpdateUIPermissions();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _authenticationService.StateChanged -= OnAuthenticationStateChanged;
            }
            base.Dispose(disposing);
        }
    }
}