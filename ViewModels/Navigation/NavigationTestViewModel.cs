using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using SubExplore.Helpers;

namespace SubExplore.ViewModels.Navigation
{
    public partial class NavigationTestViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private int _testCounter = 0;

        [ObservableProperty]
        private string navigationHistoryInfo = "Chargement...";

        [ObservableProperty]
        private string currentPage = "Test de Navigation";

        [ObservableProperty]
        private int historyCount;

        [ObservableProperty]
        private int breadcrumbCount;

        [ObservableProperty]
        private int transitionCount;

        [ObservableProperty]
        private int navigationCount;

        public NavigationTestViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            Title = "Test de Navigation";
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                
                // Initialize navigation statistics
                RefreshNavigationInfo();
                
                await Task.Delay(500); // Simulate loading
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationTestViewModel] InitializeAsync error: {ex.Message}");
                await ShowErrorAsync("Erreur d'initialisation", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void RefreshHistory()
        {
            RefreshNavigationInfo();
        }

        [RelayCommand]
        private void AddBreadcrumb()
        {
            _testCounter++;
            BreadcrumbCount = _testCounter;
            RefreshNavigationInfo();
        }

        [RelayCommand]
        private async Task TestTransitionAsync()
        {
            try
            {
                TransitionCount++;
                
                // Simulate page transition effect
                if (Application.Current?.MainPage is Page currentPage)
                {
                    await NavigationTransitions.ApplyScaleTransition(currentPage, false, 200);
                    await Task.Delay(100);
                    await NavigationTransitions.ApplyScaleTransition(currentPage, true, 200);
                }
                
                RefreshNavigationInfo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationTestViewModel] TestTransition error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task NavigateToMapAsync()
        {
            try
            {
                NavigationCount++;
                RefreshNavigationInfo();
                await _navigationService.NavigateToAsync<ViewModels.Map.MapViewModel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationTestViewModel] NavigateToMap error: {ex.Message}");
                await ShowErrorAsync("Erreur de navigation", ex.Message);
            }
        }

        [RelayCommand]
        private async Task NavigateToFavoritesAsync()
        {
            try
            {
                NavigationCount++;
                RefreshNavigationInfo();
                await _navigationService.NavigateToAsync<ViewModels.Favorites.FavoriteSpotsViewModel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationTestViewModel] NavigateToFavorites error: {ex.Message}");
                await ShowErrorAsync("Erreur de navigation", ex.Message);
            }
        }

        [RelayCommand]
        private async Task NavigateToProfileAsync()
        {
            try
            {
                NavigationCount++;
                RefreshNavigationInfo();
                await _navigationService.NavigateToAsync<ViewModels.Profile.UserProfileViewModel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationTestViewModel] NavigateToProfile error: {ex.Message}");
                await ShowErrorAsync("Erreur de navigation", ex.Message);
            }
        }

        [RelayCommand]
        private async Task NavigateToHomeAsync()
        {
            try
            {
                NavigationCount++;
                RefreshNavigationInfo();
                await _navigationService.GoToHomeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationTestViewModel] NavigateToHome error: {ex.Message}");
                await ShowErrorAsync("Erreur de navigation", ex.Message);
            }
        }

        private void RefreshNavigationInfo()
        {
            try
            {
                HistoryCount = _navigationService.GetNavigationHistoryCount();
                CurrentPage = _navigationService.GetCurrentNavigationPath();
                
                NavigationHistoryInfo = $"Historique: {HistoryCount} pages • Tests: {TransitionCount} transitions • Navigations: {NavigationCount}";
                
                System.Diagnostics.Debug.WriteLine($"[NavigationTestViewModel] Navigation info refreshed - History: {HistoryCount}, Tests: {TransitionCount}, Navigations: {NavigationCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationTestViewModel] RefreshNavigationInfo error: {ex.Message}");
                NavigationHistoryInfo = "Erreur lors du rafraîchissement";
            }
        }

        private async Task ShowErrorAsync(string title, string message)
        {
            try
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(title, message, "D'accord");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationTestViewModel] ShowError failed: {ex.Message}");
            }
        }
    }
}