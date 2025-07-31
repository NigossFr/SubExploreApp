using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Common
{
    public partial class NavigationBarViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private string currentPageTitle = "SubExplore";

        [ObservableProperty]
        private string navigationContext = string.Empty;

        [ObservableProperty]
        private bool hasNavigationContext;

        [ObservableProperty]
        private bool canGoBack;

        [ObservableProperty]
        private bool hasNavigationHistory;

        [ObservableProperty]
        private bool showHomeButton = true;

        [ObservableProperty]
        private bool hasBreadcrumbs;

        [ObservableProperty]
        private BreadcrumbItem? selectedBreadcrumb;

        public ObservableCollection<BreadcrumbItem> BreadcrumbItems { get; } = new();

        public NavigationBarViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            UpdateNavigationState();
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            try
            {
                await _navigationService.GoBackAsync();
                UpdateNavigationState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationBarViewModel] GoBack error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task GoToHomeAsync()
        {
            try
            {
                await _navigationService.GoToHomeAsync();
                UpdateNavigationState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationBarViewModel] GoToHome error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ToggleMenuAsync()
        {
            try
            {
                // Toggle Shell flyout if available
                if (Application.Current?.MainPage is Shell shell)
                {
                    shell.FlyoutIsPresented = !shell.FlyoutIsPresented;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationBarViewModel] ToggleMenu error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task NavigateToBreadcrumbAsync(BreadcrumbItem? breadcrumb)
        {
            if (breadcrumb == null || breadcrumb.IsActive) return;

            try
            {
                // Navigate to the selected breadcrumb route
                if (!string.IsNullOrEmpty(breadcrumb.Route))
                {
                    if (Application.Current?.MainPage is Shell)
                    {
                        await Shell.Current.GoToAsync($"///{breadcrumb.Route}", true);
                    }
                }
                UpdateNavigationState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationBarViewModel] NavigateToBreadcrumb error: {ex.Message}");
            }
        }

        public void UpdatePageInfo(string title, string context = "")
        {
            CurrentPageTitle = title;
            NavigationContext = context;
            HasNavigationContext = !string.IsNullOrEmpty(context);
            UpdateNavigationState();
        }

        public void AddBreadcrumb(string title, string route, bool isActive = false)
        {
            // Remove existing active breadcrumb
            foreach (var item in BreadcrumbItems)
            {
                item.IsActive = false;
                item.ShowSeparator = true;
            }

            // Check if breadcrumb already exists
            var existing = BreadcrumbItems.FirstOrDefault(b => b.Route == route);
            if (existing != null)
            {
                existing.IsActive = isActive;
                // Remove all breadcrumbs after this one (user navigated back)
                var index = BreadcrumbItems.IndexOf(existing);
                for (int i = BreadcrumbItems.Count - 1; i > index; i--)
                {
                    BreadcrumbItems.RemoveAt(i);
                }
            }
            else
            {
                BreadcrumbItems.Add(new BreadcrumbItem
                {
                    Title = title,
                    Route = route,
                    IsActive = isActive,
                    ShowSeparator = !isActive
                });
            }

            // Last item should not show separator
            if (BreadcrumbItems.Count > 0)
            {
                BreadcrumbItems.Last().ShowSeparator = false;
            }

            HasBreadcrumbs = BreadcrumbItems.Count > 1;
        }

        public void ClearBreadcrumbs()
        {
            BreadcrumbItems.Clear();
            HasBreadcrumbs = false;
        }

        private void UpdateNavigationState()
        {
            try
            {
                CanGoBack = _navigationService.GetNavigationHistoryCount() > 0;
                HasNavigationHistory = CanGoBack;
                
                // Update page context based on current route
                var currentPath = _navigationService.GetCurrentNavigationPath();
                if (string.IsNullOrEmpty(NavigationContext))
                {
                    NavigationContext = GetNavigationContextForPath(currentPath);
                    HasNavigationContext = !string.IsNullOrEmpty(NavigationContext);
                }

                ShowHomeButton = !CurrentPageTitle.Contains("Carte"); // Don't show home on map page
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationBarViewModel] UpdateNavigationState error: {ex.Message}");
            }
        }

        private string GetNavigationContextForPath(string path)
        {
            return path switch
            {
                "🗺️ Carte" => "Explorer les spots de plongée",
                "⭐ Favoris" => "Vos spots préférés",
                "➕ Ajouter un Spot" => "Partager un nouveau spot",
                "📖 Détails du Spot" => "Informations détaillées",
                "📍 Mes Spots" => "Spots que vous avez créés",
                "👤 Profil" => "Votre profil utilisateur",
                "⚙️ Préférences" => "Paramètres de l'application",
                "📊 Statistiques" => "Vos statistiques de plongée",
                "⚖️ Validation" => "Administration des spots",
                _ => string.Empty
            };
        }
    }

    public partial class BreadcrumbItem : ObservableObject
    {
        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string route = string.Empty;

        [ObservableProperty]
        private bool isActive;

        [ObservableProperty]
        private bool showSeparator = true;
    }
}