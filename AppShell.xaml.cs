using SubExplore.Views.Map;
using SubExplore.Views.Spots;

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
            Routing.RegisterRoute("addspot", typeof(AddSpotPage));
            Routing.RegisterRoute("spotdetails", typeof(SpotDetailsPage));
            
            // Register nested routes for complex navigation
            Routing.RegisterRoute("map/addspot", typeof(AddSpotPage));
            Routing.RegisterRoute("map/spotdetails", typeof(SpotDetailsPage));
        }
    }
}
