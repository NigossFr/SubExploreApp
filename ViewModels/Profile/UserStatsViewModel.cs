using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.DTOs;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Profile
{
    public partial class UserStatsViewModel : ViewModelBase
    {
        private readonly IUserProfileService _userProfileService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private UserStatsDto? _userStats;

        [ObservableProperty]
        private int _userId;

        public UserStatsViewModel(
            IUserProfileService userProfileService,
            INavigationService navigationService)
        {
            _userProfileService = userProfileService;
            _navigationService = navigationService;
            
            Title = "Diving Statistics";
        }

        public override async Task InitializeAsync(object? parameter = null)
        {
            if (parameter is int userId)
            {
                UserId = userId;
                await LoadStatsAsync();
            }
        }

        [RelayCommand]
        private async Task LoadStatsAsync()
        {
            try
            {
                IsLoading = true;
                IsError = false;
                ErrorMessage = string.Empty;

                UserStats = await _userProfileService.GetUserStatsAsync(UserId);

                if (UserStats == null)
                {
                    IsError = true;
                    ErrorMessage = "Unable to load statistics. Please try again.";
                }
            }
            catch (Exception ex)
            {
                IsError = true;
                ErrorMessage = "An error occurred while loading statistics.";
                System.Diagnostics.Debug.WriteLine($"[UserStatsViewModel] LoadStatsAsync error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadStatsAsync();
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await _navigationService.GoBackAsync();
        }

        // Computed properties for UI binding
        public string ValidationRate
        {
            get
            {
                if (UserStats == null || UserStats.TotalSpots == 0)
                    return "0%";

                var rate = (double)UserStats.ValidatedSpots / UserStats.TotalSpots * 100;
                return $"{rate:F1}%";
            }
        }

        public string AverageDepthDisplay => UserStats?.AverageDepth != null 
            ? $"{UserStats.AverageDepth:F1}m" 
            : "N/A";

        public string MaxDepthDisplay => UserStats?.MaxDepth != null 
            ? $"{UserStats.MaxDepth:F1}m" 
            : "N/A";

        public string LastActivityDisplay => UserStats?.LastActivity?.ToString("MMM dd, yyyy") ?? "Unknown";

        public string LastSpotCreatedDisplay => UserStats?.LastSpotCreated?.ToString("MMM dd, yyyy") ?? "None";

        public string ExpertiseLevelDisplay => UserStats?.ExpertiseLevel?.ToString() ?? "Not Set";

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

        public bool HasSpots => UserStats?.TotalSpots > 0;

        public bool HasPhotos => UserStats?.TotalPhotos > 0;

        public bool HasCertifications => UserStats?.RecentCertifications?.Count > 0;

        public bool HasSpotsByType => UserStats?.SpotsByType?.Count > 0;

        public string PhotosPerSpotDisplay
        {
            get
            {
                if (UserStats?.TotalSpots == 0) return "0";
                var ratio = (double)(UserStats?.TotalPhotos ?? 0) / (UserStats?.TotalSpots ?? 1);
                return $"{ratio:F1}";
            }
        }
    }
}