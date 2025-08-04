using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using SubExplore.ViewModels.Common;
using SubExplore.Helpers;

namespace SubExplore.ViewModels.Navigation
{
    public partial class NavigationTestViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly NavigationBarViewModel _navigationBarViewModel;
        private int _breadcrumbCounter = 0;

        [ObservableProperty]
        private string navigationHistoryInfo = "Chargement...";

        [ObservableProperty]
        private string currentPage = "Test de Navigation";

        [ObservableProperty]
        private int historyCount;

        [ObservableProperty]
        private int breadcrumbCount;

        public NavigationTestViewModel(INavigationService navigationService, NavigationBarViewModel navigationBarViewModel)
        {
            _navigationService = navigationService;
            _navigationBarViewModel = navigationBarViewModel;
            Title = "Test de Navigation";
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                
                // Initialize navigation bar
                _navigationBarViewModel.UpdatePageInfo("🧪 Test de Navigation", "Tests des fonctionnalités de navigation");
                _navigationBarViewModel.AddBreadcrumb("Accueil", "map");
                _navigationBarViewModel.AddBreadcrumb("Tests", "navigationtest", true);
                
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
            _breadcrumbCounter++;
            var breadcrumbTitle = $"Test {_breadcrumbCounter}";
            var breadcrumbRoute = $"test{_breadcrumbCounter}";
            
            _navigationBarViewModel.AddBreadcrumb(breadcrumbTitle, breadcrumbRoute);
            RefreshNavigationInfo();
        }

        [RelayCommand]
        private async Task TestTransitionAsync()
        {
            try
            {
                // Simulate page transition effect
                if (Application.Current?.MainPage is Page currentPage)
                {
                    await NavigationTransitions.ApplyScaleTransition(currentPage, false, 200);
                    await Task.Delay(100);
                    await NavigationTransitions.ApplyScaleTransition(currentPage, true, 200);
                }
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
                BreadcrumbCount = _navigationBarViewModel.BreadcrumbItems.Count;
                
                NavigationHistoryInfo = $"Historique: {HistoryCount} pages • Fil d'Ariane: {BreadcrumbCount} éléments";
                
                System.Diagnostics.Debug.WriteLine($"[NavigationTestViewModel] Navigation info refreshed - History: {HistoryCount}, Breadcrumbs: {BreadcrumbCount}");
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