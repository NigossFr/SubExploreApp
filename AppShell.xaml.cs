using SubExplore.Views.Map;
using SubExplore.Views.Spots;
using SubExplore.Views.Favorites;
using SubExplore.Views.Profile;
using SubExplore.Views.Admin;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels;
using SubExplore.Controls;
using SubExplore.Helpers;

namespace SubExplore
{
    public partial class AppShell : Shell
    {
        private readonly IAuthenticationService? _authenticationService;
        private readonly FlyoutMenuViewModel _flyoutMenuViewModel;
        
        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
            
            // Initialize flyout menu ViewModel
            try
            {
                var navigationService = ServiceHelper.GetService<INavigationService>();
                var authService = ServiceHelper.TryGetService<IAuthenticationService>();
                _flyoutMenuViewModel = new FlyoutMenuViewModel(navigationService, authService);
                _authenticationService = authService;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] Constructor error: {ex.Message}");
                // Fallback initialization
                _flyoutMenuViewModel = new FlyoutMenuViewModel(new DefaultNavigationService(), null);
            }
            
            // Set the flyout content binding context
            SetFlyoutMenuContext();
            UpdateUserInfo();
            
            // Subscribe to authentication state changes if service is available
            if (_authenticationService != null)
            {
                _authenticationService.StateChanged += OnAuthenticationStateChanged;
            }
        }
        
        // Alternative constructor for explicit dependency injection
        public AppShell(IAuthenticationService authenticationService, INavigationService navigationService) : this()
        {
            _authenticationService = authenticationService;
            
            // Re-initialize flyout menu ViewModel with provided services
            _flyoutMenuViewModel = new FlyoutMenuViewModel(navigationService, authenticationService);
            
            UpdateUserInfo();
            SetFlyoutMenuContext();
            
            // Subscribe to authentication state changes
            if (_authenticationService != null)
            {
                _authenticationService.StateChanged += OnAuthenticationStateChanged;
            }
        }

        private void RegisterRoutes()
        {
            // Only register additional routes that aren't already defined in AppShell.xaml
            // Main routes are handled by ShellContent elements in XAML
            
            // Register nested routes for complex navigation workflows only
            Routing.RegisterRoute("map/addspot", typeof(AddSpotPage));
            Routing.RegisterRoute("map/spotdetails", typeof(SpotDetailsPage));
            
            // Register routes for spot editing workflow
            Routing.RegisterRoute("spotdetails/editspot", typeof(AddSpotPage));
            
            // NOTE: Removed duplicate nested routes to avoid ambiguity
            // Direct routes (userprofile, favorites, etc.) are defined in AppShell.xaml
        }
        
        // Enhanced Flyout Navigation Handlers for Staged Menu Buttons
        private async void OnMenuButtonTapped(object sender, StagedMenuButtonTappedEventArgs e)
        {
            try
            {
                if (sender is StagedMenuButton button && button.CommandParameter is string route)
                {
                    await GoToAsync(route);
                    FlyoutIsPresented = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] OnMenuButtonTapped error: {ex.Message}");
            }
        }

        private async void OnLogoutButtonTapped(object sender, StagedMenuButtonTappedEventArgs e)
        {
            try
            {
                FlyoutIsPresented = false;
                
                // Show confirmation dialog
                bool confirm = await DisplayAlert("Déconnexion", "Êtes-vous sûr de vouloir vous déconnecter ?", "Oui", "Annuler");
                
                if (confirm)
                {
                    // Call authentication service to logout if available
                    if (_authenticationService != null)
                    {
                        await _authenticationService.LogoutAsync();
                        await DisplayAlert("Déconnexion", "Vous avez été déconnecté avec succès.", "D'accord");
                    }
                    else
                    {
                        await DisplayAlert("Déconnexion", "Vous avez été déconnecté avec succès.", "D'accord");
                    }
                    
                    // Navigate back to login or main page
                    await GoToAsync("///map");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] OnLogoutButtonTapped error: {ex.Message}");
            }
        }

        // Legacy navigation handlers for backward compatibility
        private async void OnNavigateToMap(object sender, EventArgs e)
        {
            await GoToAsync("///map");
            FlyoutIsPresented = false;
        }
        
        private async void OnNavigateToMySpots(object sender, EventArgs e)
        {
            await GoToAsync("///myspots");
            FlyoutIsPresented = false;
        }
        
        private async void OnNavigateToFavorites(object sender, EventArgs e)
        {
            await GoToAsync("///favorites");
            FlyoutIsPresented = false;
        }
        
        private async void OnNavigateToProfile(object sender, EventArgs e)
        {
            await GoToAsync("///userprofile");
            FlyoutIsPresented = false;
        }
        
        private async void OnNavigateToPreferences(object sender, EventArgs e)
        {
            await GoToAsync("///userpreferences");
            FlyoutIsPresented = false;
        }
        
        private async void OnNavigateToStats(object sender, EventArgs e)
        {
            await GoToAsync("///userstats");
            FlyoutIsPresented = false;
        }
        
        private async void OnNavigateToValidation(object sender, EventArgs e)
        {
            await GoToAsync("///spotvalidation");
            FlyoutIsPresented = false;
        }
        
        private async void OnLogout(object sender, EventArgs e)
        {
            OnLogoutButtonTapped(sender, new StagedMenuButtonTappedEventArgs(MenuButtonStage.Error, "Déconnexion"));
        }
        
        private void OnAuthenticationStateChanged(object sender, Services.Interfaces.AuthenticationStateChangedEventArgs e)
        {
            UpdateUserInfo();
            _flyoutMenuViewModel?.RefreshMenu();
        }
        
        private void SetFlyoutMenuContext()
        {
            try
            {
                // Set the binding context directly on the flyout content template
                // The actual flyout content will be created with this context
                if (FlyoutContentTemplate != null)
                {
                    // Store the ViewModel as a bindable property or resource for access by the template
                    this.Resources["FlyoutMenuViewModel"] = _flyoutMenuViewModel;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] SetFlyoutMenuContext error: {ex.Message}");
            }
        }
        
        // Public methods to update menu state from external components
        public void UpdateMenuItemStage(string itemId, MenuButtonStage stage, string? badgeText = null)
        {
            _flyoutMenuViewModel?.UpdateMenuItemStage(itemId, stage, badgeText);
        }
        
        public void SetMenuItemBadge(string itemId, string badgeText, bool show = true)
        {
            _flyoutMenuViewModel?.SetMenuItemBadge(itemId, badgeText, show);
        }
        
        private void UpdateUserInfo()
        {
            try
            {
                // Find the UserNameLabel in the flyout header template
                var flyoutHeader = FlyoutHeader;
                if (flyoutHeader is View headerView)
                {
                    var userNameLabel = headerView.FindByName<Label>("UserNameLabel");
                    if (userNameLabel != null && _authenticationService?.CurrentUser != null)
                    {
                        var user = _authenticationService.CurrentUser;
                        var displayName = !string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName)
                            ? $"{user.FirstName} {user.LastName}"
                            : user.Username ?? "Utilisateur SubExplore";
                        
                        userNameLabel.Text = displayName;
                        System.Diagnostics.Debug.WriteLine($"[AppShell] Updated user name to: {displayName}");
                    }
                    else if (userNameLabel != null)
                    {
                        userNameLabel.Text = "Utilisateur SubExplore";
                        System.Diagnostics.Debug.WriteLine($"[AppShell] Set default user name");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] UpdateUserInfo error: {ex.Message}");
            }
        }
        
        
        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateUserInfo();
            _flyoutMenuViewModel?.RefreshMenu();
        }
    }
}
