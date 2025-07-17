using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.ViewModels.Map;
using SubExplore.Services.Interfaces;

namespace SubExplore.Views.Map
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MapPage : ContentPage
    {
        private MapViewModel? ViewModel => BindingContext as MapViewModel;
        private bool _isMapLoaded = false;
        private readonly IPlatformMapService? _platformMapService;

        public MapPage(MapViewModel viewModel, IPlatformMapService platformMapService = null)
        {
            InitializeComponent();
            BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _platformMapService = platformMapService;
            
            // Subscribe to map loaded event
            if (MainMap != null)
            {
                MainMap.Loaded += OnMapLoaded;
            }
        }

        private void OnMapLoaded(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[INFO] Map loaded event fired");
            _isMapLoaded = true;
            
            // Apply platform-specific optimizations
            if (MainMap != null && _platformMapService != null)
            {
                try
                {
                    _platformMapService.ApplyMapOptimizations(MainMap);
                    System.Diagnostics.Debug.WriteLine("[INFO] Applied platform-specific map optimizations");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to apply map optimizations: {ex.Message}");
                }
            }
            
            // Debug map configuration
            if (MainMap != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Map Type: {MainMap.MapType}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Map IsShowingUser: {MainMap.IsShowingUser}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Map IsZoomEnabled: {MainMap.IsZoomEnabled}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Map IsScrollEnabled: {MainMap.IsScrollEnabled}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Map ItemsSource count: {MainMap.ItemsSource?.Cast<object>().Count() ?? 0}");
            }
            
            // Update position once map is loaded
            if (ViewModel != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Map loaded - ViewModel has {ViewModel.Spots?.Count ?? 0} spots and {ViewModel.Pins?.Count ?? 0} pins");
                ViewModel.InitializeMapPosition();
                UpdateMapPosition();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[WARNING] Map loaded but ViewModel is null");
            }
        }

        private void OnMapClicked(object sender, MapClickedEventArgs e)
        {
            ViewModel?.MapClickedCommand?.Execute(e);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            System.Diagnostics.Debug.WriteLine("[INFO] MapPage OnAppearing called");

            if (ViewModel != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[INFO] Starting ViewModel initialization");
                    await ViewModel.InitializeAsync();
                    System.Diagnostics.Debug.WriteLine("[INFO] ViewModel initialization completed");
                    
                    // Give map time to render before positioning
                    await Task.Delay(500);
                    
                    // Update map position after initialization on main thread
                    System.Diagnostics.Debug.WriteLine("[INFO] Updating map position");
                    await Application.Current.Dispatcher.DispatchAsync(UpdateMapPosition);
                    System.Diagnostics.Debug.WriteLine("[INFO] Map position update completed");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] MapPage OnAppearing failed: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[ERROR] ViewModel is null in OnAppearing");
            }
        }

        private void UpdateMapPosition()
        {
            if (ViewModel != null && MainMap != null)
            {
                try
                {
                    var position = new Location(ViewModel.MapLatitude, ViewModel.MapLongitude);
                    
                    // Convert zoom level to appropriate distance (zoom 12 ≈ 10km radius)
                    var distanceKm = Math.Max(1, 20 - ViewModel.MapZoomLevel);
                    var span = MapSpan.FromCenterAndRadius(position, Distance.FromKilometers(distanceKm));
                    
                    MainMap.MoveToRegion(span);
                    System.Diagnostics.Debug.WriteLine($"[INFO] Map positioned to: {ViewModel.MapLatitude}, {ViewModel.MapLongitude}, zoom: {ViewModel.MapZoomLevel}, distance: {distanceKm}km, loaded: {_isMapLoaded}");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Map type: {MainMap.MapType}, IsZoomEnabled: {MainMap.IsZoomEnabled}, IsScrollEnabled: {MainMap.IsScrollEnabled}");
                    
                    // Remove inefficient visibility toggle - use proper refresh method
                    if (_platformMapService != null)
                    {
                        _platformMapService.RefreshMapDisplay(MainMap);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to update map position: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[WARNING] UpdateMapPosition called but ViewModel={ViewModel != null}, MainMap={MainMap != null}, loaded: {_isMapLoaded}");
            }
        }
        
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Cleanup map resources for better memory management
            if (MainMap != null)
            {
                MainMap.Loaded -= OnMapLoaded;
            }
            
            System.Diagnostics.Debug.WriteLine("[INFO] MapPage OnDisappearing - resources cleaned up");
        }
        
        private void OnMapRegionChanged(object sender, EventArgs e)
        {
            // Update ViewModel with current visible region for better performance
            if (ViewModel != null && MainMap != null)
            {
                ViewModel.VisibleRegion = MainMap.VisibleRegion;
            }
        }
    }
}