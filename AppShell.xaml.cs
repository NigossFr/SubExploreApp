using SubExplore.Views.Map;
using SubExplore.Views.Spots;
using SubExplore.Views.Favorites;
using SubExplore.Views.Profile;
using SubExplore.Views.Admin;

namespace SubExplore
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
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
                    // Here you would typically call your authentication service to logout
                    // For now, we'll just show a message
                    await DisplayAlert("Déconnexion", "Vous avez été déconnecté avec succès.", "OK");
                    
                    // Navigate back to login or main page
                    await GoToAsync("///map");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] OnLogout error: {ex.Message}");
            }
        }
    }
}
