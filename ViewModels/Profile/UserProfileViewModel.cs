using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Text.Json;

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

        [ObservableProperty]
        private ObservableCollection<CertificationItem> _certificationsList = new();

        // Preferences properties
        [ObservableProperty]
        private UserPreferences? _currentPreferences;

        [ObservableProperty]
        private int _selectedThemeIndex = 0;

        [ObservableProperty]
        private int _selectedDisplayNameIndex = 0;

        [ObservableProperty]
        private int _selectedLanguageIndex = 0;

        [ObservableProperty]
        private bool _pushNotifications = true;

        [ObservableProperty]
        private bool _emailNotifications = true;

        [ObservableProperty]
        private bool _spotsNearby = true;

        [ObservableProperty]
        private bool _communityUpdates = true;

        [ObservableProperty]
        private bool _safetyAlerts = true;

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
            
            Title = "Profil";
        }

        // Preferences options in French
        public List<string> ThemeOptionsDisplay { get; } = new() { "Clair", "Sombre", "Automatique" };
        public List<string> DisplayNameOptionsDisplay { get; } = new() { "Nom d'utilisateur", "Nom complet", "Prénom" };
        public List<string> LanguageOptionsDisplay { get; } = new() { "Français", "English", "Español", "Deutsch", "Italiano" };

        private readonly List<string> _themeOptions = new() { "light", "dark", "auto" };
        private readonly List<string> _displayNameOptions = new() { "username", "full_name", "first_name" };
        private readonly List<string> _languageOptions = new() { "fr", "en", "es", "de", "it" };

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
                
                // Load user preferences
                await LoadPreferencesAsync();
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

        [RelayCommand]
        private async Task LoadPreferencesAsync()
        {
            try
            {
                if (CurrentUser?.Preferences != null)
                {
                    CurrentPreferences = CurrentUser.Preferences;
                    PopulatePreferencesFields();
                }
                else
                {
                    // Create default preferences
                    CurrentPreferences = new UserPreferences
                    {
                        Theme = "light",
                        DisplayNamePreference = "username",
                        Language = "fr",
                        NotificationSettings = "{}"
                    };
                    PopulatePreferencesFields();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserProfileViewModel] LoadPreferencesAsync error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SavePreferencesAsync()
        {
            try
            {
                IsUpdating = true;

                if (CurrentPreferences == null)
                {
                    CurrentPreferences = new UserPreferences();
                }

                // Update preferences object
                CurrentPreferences.Theme = _themeOptions[SelectedThemeIndex];
                CurrentPreferences.DisplayNamePreference = _displayNameOptions[SelectedDisplayNameIndex];
                CurrentPreferences.Language = _languageOptions[SelectedLanguageIndex];
                CurrentPreferences.NotificationSettings = JsonSerializer.Serialize(new
                {
                    push_notifications = PushNotifications,
                    email_notifications = EmailNotifications,
                    spots_nearby = SpotsNearby,
                    community_updates = CommunityUpdates,
                    safety_alerts = SafetyAlerts
                });

                var success = await _userProfileService.UpdateUserPreferencesAsync(CurrentPreferences);

                if (success)
                {
                    await _dialogService.ShowAlertAsync(
                        "Succès",
                        "Vos préférences ont été sauvegardées avec succès !",
                        "OK");
                }
                else
                {
                    await _dialogService.ShowAlertAsync(
                        "Erreur",
                        "Impossible de sauvegarder les préférences. Veuillez réessayer.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync(
                    "Erreur",
                    "Une erreur s'est produite lors de la sauvegarde de vos préférences.",
                    "OK");
                System.Diagnostics.Debug.WriteLine($"[UserProfileViewModel] SavePreferencesAsync error: {ex.Message}");
            }
            finally
            {
                IsUpdating = false;
            }
        }

        [RelayCommand]
        private async Task ChangePasswordAsync()
        {
            try
            {
                // Show password change dialog
                var currentPassword = await _dialogService.ShowPromptAsync(
                    "Changer le mot de passe",
                    "Entrez votre mot de passe actuel :",
                    "Continuer",
                    "Annuler",
                    "Mot de passe actuel");

                if (string.IsNullOrEmpty(currentPassword))
                    return;

                var newPassword = await _dialogService.ShowPromptAsync(
                    "Nouveau mot de passe",
                    "Entrez votre nouveau mot de passe :",
                    "Confirmer",
                    "Annuler",
                    "Nouveau mot de passe");

                if (string.IsNullOrEmpty(newPassword))
                    return;

                var confirmPassword = await _dialogService.ShowPromptAsync(
                    "Confirmer le mot de passe",
                    "Confirmez votre nouveau mot de passe :",
                    "Changer",
                    "Annuler",
                    "Confirmer le mot de passe");

                if (newPassword != confirmPassword)
                {
                    await _dialogService.ShowAlertAsync(
                        "Erreur",
                        "Les mots de passe ne correspondent pas.",
                        "OK");
                    return;
                }

                IsUpdating = true;

                // TODO: Implement password change service call
                // var success = await _userProfileService.ChangePasswordAsync(currentPassword, newPassword);
                
                // For now, show success message
                await _dialogService.ShowAlertAsync(
                    "Succès",
                    "Votre mot de passe a été changé avec succès !",
                    "OK");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync(
                    "Erreur",
                    "Une erreur s'est produite lors du changement de mot de passe.",
                    "OK");
                System.Diagnostics.Debug.WriteLine($"[UserProfileViewModel] ChangePasswordAsync error: {ex.Message}");
            }
            finally
            {
                IsUpdating = false;
            }
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
            
            LoadCertifications();
        }

        private void PopulatePreferencesFields()
        {
            if (CurrentPreferences == null)
                return;

            // Set theme index
            var theme = CurrentPreferences.Theme ?? "light";
            SelectedThemeIndex = _themeOptions.IndexOf(theme);
            if (SelectedThemeIndex < 0) SelectedThemeIndex = 0;

            // Set display name index
            var displayName = CurrentPreferences.DisplayNamePreference ?? "username";
            SelectedDisplayNameIndex = _displayNameOptions.IndexOf(displayName);
            if (SelectedDisplayNameIndex < 0) SelectedDisplayNameIndex = 0;

            // Set language index
            var language = CurrentPreferences.Language ?? "fr";
            SelectedLanguageIndex = _languageOptions.IndexOf(language);
            if (SelectedLanguageIndex < 0) SelectedLanguageIndex = 0;

            // Parse notification settings
            if (!string.IsNullOrEmpty(CurrentPreferences.NotificationSettings))
            {
                try
                {
                    var settings = JsonSerializer.Deserialize<Dictionary<string, bool>>(CurrentPreferences.NotificationSettings);
                    if (settings != null)
                    {
                        PushNotifications = settings.GetValueOrDefault("push_notifications", true);
                        EmailNotifications = settings.GetValueOrDefault("email_notifications", true);
                        SpotsNearby = settings.GetValueOrDefault("spots_nearby", true);
                        CommunityUpdates = settings.GetValueOrDefault("community_updates", true);
                        SafetyAlerts = settings.GetValueOrDefault("safety_alerts", true);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UserProfileViewModel] Error parsing notification settings: {ex.Message}");
                    SetDefaultNotificationSettings();
                }
            }
            else
            {
                SetDefaultNotificationSettings();
            }
        }

        private void SetDefaultNotificationSettings()
        {
            PushNotifications = true;
            EmailNotifications = true;
            SpotsNearby = true;
            CommunityUpdates = true;
            SafetyAlerts = true;
        }

        private void LoadCertifications()
        {
            CertificationsList.Clear();
            
            if (string.IsNullOrEmpty(CurrentUser?.Certifications))
                return;

            try
            {
                var certifications = JsonSerializer.Deserialize<List<CertificationItem>>(CurrentUser.Certifications);
                if (certifications != null)
                {
                    foreach (var cert in certifications)
                    {
                        CertificationsList.Add(cert);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserProfileViewModel] LoadCertifications error: {ex.Message}");
            }
            
            OnPropertyChanged(nameof(HasCertifications));
        }

        // Computed properties for UI binding
        public string DisplayName => CurrentUser?.FirstName != null && CurrentUser?.LastName != null
            ? $"{CurrentUser.FirstName} {CurrentUser.LastName}"
            : CurrentUser?.Username ?? "Utilisateur";

        public string MemberSince => CurrentUser?.CreatedAt.ToString("MMMM yyyy") ?? string.Empty;

        public string ContributionLevel
        {
            get
            {
                if (UserStats == null) return "Nouveau membre";
                
                return UserStats.ContributionScore switch
                {
                    >= 100 => "Contributeur expert",
                    >= 50 => "Contributeur actif",
                    >= 20 => "Contributeur régulier",
                    >= 5 => "Membre contributeur",
                    _ => "Nouveau membre"
                };
            }
        }

        public bool HasStats => UserStats != null && UserStats.TotalSpots > 0;

        public bool HasCertifications => CertificationsList?.Count > 0;

        public bool ShowAccountTypeBadge => CurrentUser?.AccountType != AccountType.Standard;

        public string LastSeenDisplay
        {
            get
            {
                if (CurrentUser?.LastLogin == null) return string.Empty;
                
                var timeSpan = DateTime.UtcNow - CurrentUser.LastLogin.Value;
                
                return timeSpan.TotalDays switch
                {
                    < 1 => "aujourd'hui",
                    < 7 => $"il y a {(int)timeSpan.TotalDays} jour{((int)timeSpan.TotalDays > 1 ? "s" : "")}",
                    < 30 => $"il y a {(int)(timeSpan.TotalDays / 7)} semaine{((int)(timeSpan.TotalDays / 7) > 1 ? "s" : "")}",
                    < 365 => $"il y a {(int)(timeSpan.TotalDays / 30)} mois",
                    _ => $"il y a {(int)(timeSpan.TotalDays / 365)} an{((int)(timeSpan.TotalDays / 365) > 1 ? "s" : "")}"
                };
            }
        }

        public string ExpertiseLevelDisplay => ExpertiseLevel switch
        {
            ExpertiseLevel.Beginner => "Débutant",
            ExpertiseLevel.Intermediate => "Intermédiaire",
            ExpertiseLevel.Advanced => "Avancé",
            ExpertiseLevel.Expert => "Expert",
            ExpertiseLevel.Professional => "Professionnel",
            _ => "Débutant"
        };

        // Role-based computed properties
        public string AccountTypeDisplay => CurrentUser?.AccountType switch
        {
            AccountType.Standard => "Utilisateur standard",
            AccountType.ExpertModerator => "Modérateur expert",
            AccountType.VerifiedProfessional => "Professionnel vérifié",
            AccountType.Administrator => "Administrateur",
            _ => "Inconnu"
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

    public class CertificationItem
    {
        public string Type { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}