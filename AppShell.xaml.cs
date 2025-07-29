using SubExplore.Views.Map;
using SubExplore.Views.Spots;
using SubExplore.Views.Favorites;
using SubExplore.Views.Profile;

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
            // Register page routes for programmatic navigation
            Routing.RegisterRoute("map", typeof(MapPage));
            Routing.RegisterRoute("favorites", typeof(FavoriteSpotsPage));
            Routing.RegisterRoute("myspots", typeof(MySpotsPage));
            Routing.RegisterRoute("userprofile", typeof(UserProfilePage));
            Routing.RegisterRoute("addspot", typeof(AddSpotPage));
            Routing.RegisterRoute("spotdetails", typeof(SpotDetailsPage));
            Routing.RegisterRoute("userpreferences", typeof(UserPreferencesPage));
            Routing.RegisterRoute("userstats", typeof(UserStatsPage));
            
            // Register nested routes for complex navigation
            Routing.RegisterRoute("map/addspot", typeof(AddSpotPage));
            Routing.RegisterRoute("map/spotdetails", typeof(SpotDetailsPage));
        }
    }
}
