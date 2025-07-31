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
    }
}
