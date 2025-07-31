using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Profile
{
    public partial class UserProfileViewModel : AuthorizedViewModelBase
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;
        private readonly IMediaService _mediaService;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private UserStatsDto? _userStats;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _firstName = string.Empty;

        [ObservableProperty]
        private string _lastName = string.Empty;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _avatarUrl = string.Empty;

        [ObservableProperty]
        private ExpertiseLevel _expertiseLevel = ExpertiseLevel.Beginner;

        [ObservableProperty]
        private string _certifications = string.Empty;

        [ObservableProperty]
        private bool _isUpdating;

        [ObservableProperty]
        private string _validationMessage = string.Empty;

        public UserProfileViewModel(
            IUserProfileService userProfileService,
            IDialogService dialogService,
            INavigationService navigationService,
            IMediaService mediaService,
            IAuthorizationService authorizationService,
            IAuthenticationService authenticationService)
            : base(authorizationService, authenticationService)
        {
            _userProfileService = userProfileService;
            _dialogService = dialogService;
            _navigationService = navigationService;
            _mediaService = mediaService;
            
            Title = "Profile";
        }

        public override async Task InitializeAsync(object? parameter = null)
        {
            await LoadUserProfileAsync();
            UpdateUIPermissions();
        }

        [RelayCommand]
        private async Task LoadUserProfileAsync()
        {
            try
            {
                IsLoading = true;
                IsError = false;
                ErrorMessage = string.Empty;

                CurrentUser = await _userProfileService.GetCurrentUserAsync();
                
                if (CurrentUser == null)
                {
                    IsError = true;
                    ErrorMessage = "Unable to load user profile. Please try again.";
                    return;
                }

                // Load user statistics
                UserStats = await _userProfileService.GetUserStatsAsync(CurrentUser.Id);

                // Populate edit fields
                PopulateEditFields();
            }
            catch (Exception ex)
            {
                IsError = true;
                ErrorMessage = "An error occurred while loading your profile.";
                System.Diagnostics.Debug.WriteLine($"[UserProfileViewModel] LoadUserProfileAsync error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
            
            if (IsEditMode)
            {
                PopulateEditFields();
            }
            else
            {
                // Reset validation message when exiting edit mode
                ValidationMessage = string.Empty;
            }
        }

        [RelayCommand]
        private async Task SaveProfileAsync()
        {
            if (CurrentUser == null)
                return;

            try
            {
                IsUpdating = true;
                ValidationMessage = string.Empty;

                // Create updated user object
                var updatedUser = new User
                {
                    Id = CurrentUser.Id,
                    FirstName = FirstName,
                    LastName = LastName,
                    Username = Username,
                    Email = Email,
                    AvatarUrl = AvatarUrl,
                    ExpertiseLevel = ExpertiseLevel,
                    Certifications = Certifications,
                    CreatedAt = CurrentUser.CreatedAt
                };

                // Update profile
                var success = await _userProfileService.UpdateUserProfileAsync(updatedUser);

                if (success)
                {
                    // Refresh current user data
                    CurrentUser = await _userProfileService.GetCurrentUserAsync();
                    IsEditMode = false;
                    
                    await _dialogService.ShowAlertAsync(
                        "Success",
                        "Your profile has been updated successfully!",
                        "OK");
                }
                else
                {
                    ValidationMessage = "Unable to update profile. Please check your information and try again.";
                }
            }
            catch (Exception ex)
            {
                ValidationMessage = "An error occurred while updating your profile.";
                System.Diagnostics.Debug.WriteLine($"[UserProfileViewModel] SaveProfileAsync error: {ex.Message}");
            }
            finally
            {
                IsUpdating = false;
            }
        }

        [RelayCommand]
        private async Task ChangeAvatarAsync()
        {
            try
            {
                IsUpdating = true;

                // Use media service to select photo
                var photoPath = await _mediaService.PickPhotoAsync();
                
                if (!string.IsNullOrEmpty(photoPath))
                {
                    // Update avatar URL
                    var success = await _userProfileService.UpdateUserAvatarAsync(photoPath);
                    
                    if (success)
                    {
                        AvatarUrl = photoPath;
                        
                        // Refresh current user data
                        CurrentUser = await _userProfileService.GetCurrentUserAsync();
                        
                        await _dialogService.ShowAlertAsync(
                            "Success",
                            "Your profile picture has been updated!",
                            "OK");
                    }
                    else
                    {
                        await _dialogService.ShowAlertAsync(
                            "Error",
                            "Unable to update profile picture. Please try again.",
                            "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync(
                    "Error",
                    "An error occurred while updating your profile picture.",
                    "OK");
                System.Diagnostics.Debug.WriteLine($"[UserProfileViewModel] ChangeAvatarAsync error: {ex.Message}");
            }
            finally
            {
                IsUpdating = false;
            }
        }

        [RelayCommand]
        private async Task EditPreferencesAsync()
        {
            // Navigate to preferences page
            await _navigationService.NavigateToAsync<UserPreferencesViewModel>();
        }

        [RelayCommand]
        private async Task ViewStatsAsync()
        {
            // Navigate to detailed stats page
            await _navigationService.NavigateToAsync<UserStatsViewModel>(CurrentUser?.Id);
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadUserProfileAsync();
        }

        private void PopulateEditFields()
        {
            if (CurrentUser == null)
                return;

            FirstName = CurrentUser.FirstName ?? string.Empty;
            LastName = CurrentUser.LastName ?? string.Empty;
            Username = CurrentUser.Username ?? string.Empty;
            Email = CurrentUser.Email ?? string.Empty;
            AvatarUrl = CurrentUser.AvatarUrl ?? string.Empty;
            ExpertiseLevel = CurrentUser.ExpertiseLevel ?? ExpertiseLevel.Beginner;
            Certifications = CurrentUser.Certifications ?? string.Empty;
        }

        // Computed properties for UI binding
        public string DisplayName => CurrentUser?.FirstName != null && CurrentUser?.LastName != null
            ? $"{CurrentUser.FirstName} {CurrentUser.LastName}"
            : CurrentUser?.Username ?? "User";

        public string MemberSince => CurrentUser?.CreatedAt.ToString("MMMM yyyy") ?? string.Empty;

        public string ContributionLevel
        {
            get
            {
                if (UserStats == null) return "New Member";
                
                return UserStats.ContributionScore switch
                {
                    >= 100 => "Expert Contributor",
                    >= 50 => "Active Contributor",
                    >= 20 => "Regular Contributor",
                    >= 5 => "Contributing Member",
                    _ => "New Member"
                };
            }
        }

        public bool HasStats => UserStats != null && UserStats.TotalSpots > 0;

        public string ExpertiseLevelDisplay => ExpertiseLevel switch
        {
            ExpertiseLevel.Beginner => "Beginner",
            ExpertiseLevel.Intermediate => "Intermediate",
            ExpertiseLevel.Advanced => "Advanced",
            ExpertiseLevel.Expert => "Expert",
            ExpertiseLevel.Professional => "Professional",
            _ => "Beginner"
        };

        // Role-based computed properties
        public string AccountTypeDisplay => CurrentUser?.AccountType switch
        {
            AccountType.Standard => "Standard User",
            AccountType.ExpertModerator => "Expert Moderator",
            AccountType.VerifiedProfessional => "Verified Professional",
            AccountType.Administrator => "Administrator",
            _ => "Unknown"
        };

        public bool ShowModeratorInfo => CurrentUser?.AccountType == AccountType.ExpertModerator;
        public bool ShowProfessionalInfo => CurrentUser?.AccountType == AccountType.VerifiedProfessional;
        public bool ShowAdminInfo => CurrentUser?.AccountType == AccountType.Administrator;

        protected override void UpdateUIPermissions()
        {
            base.UpdateUIPermissions();
            
            // Trigger property change notifications for computed properties
            OnPropertyChanged(nameof(AccountTypeDisplay));
            OnPropertyChanged(nameof(ShowModeratorInfo));
            OnPropertyChanged(nameof(ShowProfessionalInfo));
            OnPropertyChanged(nameof(ShowAdminInfo));
        }
    }
}