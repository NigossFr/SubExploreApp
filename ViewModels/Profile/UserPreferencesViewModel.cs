using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using System.Text.Json;

namespace SubExplore.ViewModels.Profile
{
    public partial class UserPreferencesViewModel : ViewModelBase
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private UserPreferences? _currentPreferences;

        [ObservableProperty]
        private string _selectedTheme = "light";

        [ObservableProperty]
        private string _selectedDisplayName = "username";

        [ObservableProperty]
        private string _selectedLanguage = "fr";

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

        [ObservableProperty]
        private bool _isUpdating;

        public List<string> ThemeOptions { get; } = new() { "light", "dark", "auto" };
        public List<string> DisplayNameOptions { get; } = new() { "username", "full_name", "first_name" };
        public List<string> LanguageOptions { get; } = new() { "fr", "en", "es", "de", "it" };

        public UserPreferencesViewModel(
            IUserProfileService userProfileService,
            IDialogService dialogService,
            INavigationService navigationService)
        {
            _userProfileService = userProfileService;
            _dialogService = dialogService;
            _navigationService = navigationService;
            
            Title = "Preferences";
        }

        public override async Task InitializeAsync(object? parameter = null)
        {
            await LoadPreferencesAsync();
        }

        [RelayCommand]
        private async Task LoadPreferencesAsync()
        {
            try
            {
                IsLoading = true;
                IsError = false;
                ErrorMessage = string.Empty;

                var currentUser = await _userProfileService.GetCurrentUserAsync();
                if (currentUser?.Preferences != null)
                {
                    CurrentPreferences = currentUser.Preferences;
                    PopulateFields();
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
                    PopulateFields();
                }
            }
            catch (Exception ex)
            {
                IsError = true;
                ErrorMessage = "Unable to load preferences. Please try again.";
                System.Diagnostics.Debug.WriteLine($"[UserPreferencesViewModel] LoadPreferencesAsync error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
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
                CurrentPreferences.Theme = SelectedTheme;
                CurrentPreferences.DisplayNamePreference = SelectedDisplayName;
                CurrentPreferences.Language = SelectedLanguage;
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
                        "Success",
                        "Your preferences have been saved successfully!",
                        "OK");
                }
                else
                {
                    await _dialogService.ShowAlertAsync(
                        "Error",
                        "Unable to save preferences. Please try again.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync(
                    "Error",
                    "An error occurred while saving your preferences.",
                    "OK");
                System.Diagnostics.Debug.WriteLine($"[UserPreferencesViewModel] SavePreferencesAsync error: {ex.Message}");
            }
            finally
            {
                IsUpdating = false;
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await _navigationService.GoBackAsync();
        }

        private void PopulateFields()
        {
            if (CurrentPreferences == null)
                return;

            SelectedTheme = CurrentPreferences.Theme ?? "light";
            SelectedDisplayName = CurrentPreferences.DisplayNamePreference ?? "username";
            SelectedLanguage = CurrentPreferences.Language ?? "fr";

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
                    System.Diagnostics.Debug.WriteLine($"[UserPreferencesViewModel] Error parsing notification settings: {ex.Message}");
                    // Use defaults if parsing fails
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

        // Display names for UI
        public string ThemeDisplayName => SelectedTheme switch
        {
            "light" => "Light",
            "dark" => "Dark",
            "auto" => "Auto",
            _ => "Light"
        };

        public string DisplayNameDisplayName => SelectedDisplayName switch
        {
            "username" => "Username",
            "full_name" => "Full Name",
            "first_name" => "First Name",
            _ => "Username"
        };

        public string LanguageDisplayName => SelectedLanguage switch
        {
            "fr" => "Français",
            "en" => "English",
            "es" => "Español",
            "de" => "Deutsch",
            "it" => "Italiano",
            _ => "Français"
        };
    }
}