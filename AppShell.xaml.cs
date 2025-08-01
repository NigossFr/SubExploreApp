using SubExplore.Views.Map;
using SubExplore.Views.Spots;
using SubExplore.Views.Favorites;
using SubExplore.Views.Profile;
using SubExplore.Views.Admin;
using SubExplore.Services.Interfaces;

namespace SubExplore
{
    public partial class AppShell : Shell
    {
        private readonly IAuthenticationService? _authenticationService;
        
        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
        }
        
        public AppShell(IAuthenticationService authenticationService) : this()
        {
            _authenticationService = authenticationService;
            UpdateUserInfo();
            
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
        
        // Custom Flyout Navigation Handlers
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
                        await DisplayAlert("Déconnexion", "Vous avez été déconnecté avec succès.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Déconnexion", "Vous avez été déconnecté avec succès.", "OK");
                    }
                    
                    // Navigate back to login or main page
                    await GoToAsync("///map");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] OnLogout error: {ex.Message}");
            }
        }
        
        private void OnAuthenticationStateChanged(object sender, Services.Interfaces.AuthenticationStateChangedEventArgs e)
        {
            UpdateUserInfo();
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
        }
    }
}
