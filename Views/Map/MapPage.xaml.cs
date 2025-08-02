using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.ViewModels.Map;
using SubExplore.Services.Interfaces;
using System.Linq;

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
                
                // Add gesture recognizer to capture all map touches
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += OnMapTapped;
                MainMap.GestureRecognizers.Add(tapGesture);
                
                System.Diagnostics.Debug.WriteLine("[DEBUG] Added gesture recognizer to map for direct touch handling");
            }
        }

        private void OnMapLoaded(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[INFO] Map loaded event fired");
            _isMapLoaded = true;
            
            // Debug binding context and mini window
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Page BindingContext: {BindingContext?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ViewModel: {ViewModel?.GetType().Name}");
            
            if (SpotMiniWindow != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotMiniWindow found: {SpotMiniWindow.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotMiniWindow BindingContext: {SpotMiniWindow.BindingContext?.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotMiniWindow IsVisible: {SpotMiniWindow.IsVisible}");
                
                // Debug: Mini window found and ready for use
                System.Diagnostics.Debug.WriteLine("[DEBUG] Mini window properly configured and ready");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[ERROR] SpotMiniWindow is null!");
            }
            
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

        private void OnPinInfoWindowClicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] OnPinInfoWindowClicked: Pin info window clicked");
                
                if (sender is Pin pin && pin.BindingContext is Models.Domain.Spot spot)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] InfoWindow clicked for spot: {spot.Name}");
                    
                    // Show our custom mini window
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("[DEBUG] Executing ShowSpotMiniWindow via InfoWindow click");
                            ViewModel?.ShowSpotMiniWindowCommand?.Execute(spot);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] InfoWindow ShowSpotMiniWindow failed: {ex.Message}");
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] InfoWindow clicked but no spot found in BindingContext");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] OnPinInfoWindowClicked failed: {ex.Message}");
            }
        }

        private async void OnMapClicked(object sender, MapClickedEventArgs e)
        {
            try
            {
                var clickedLocation = e.Location;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] OnMapClicked: Map clicked at {clickedLocation?.Latitude:F6}, {clickedLocation?.Longitude:F6}");
                
                if (clickedLocation == null || ViewModel?.Pins == null)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] OnMapClicked: Invalid location or no pins available");
                    return;
                }

                // Check if click is near any pin using enhanced detection
                var foundNearbySpot = await CheckNearbyPinsFromMapClick(clickedLocation);
                
                if (!foundNearbySpot)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] OnMapClicked: No nearby spots found - clicking empty space");
                    
                    // Close mini window if open (clicking empty space)
                    if (ViewModel != null && ViewModel.IsSpotMiniWindowVisible)
                    {
                        ViewModel.CloseSpotMiniWindowCommand?.Execute(null);
                        System.Diagnostics.Debug.WriteLine("[DEBUG] OnMapClicked: Closed mini window - clicked empty space");
                    }
                    
                    // Handle normal map click for adding spots at specific location
                    ViewModel?.MapClickedCommand?.Execute(e);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] OnMapClicked failed: {ex.Message}");
            }
        }

        private async Task<bool> CheckNearbyPinsFromMapClick(Location clickedLocation)
        {
            try
            {
                // Use dynamic tolerance based on zoom level
                var toleranceKm = CalculateDynamicTolerance();
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] CheckNearbyPinsFromMapClick: Checking {ViewModel.Pins.Count} pins with {toleranceKm}km tolerance");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Click location: {clickedLocation.Latitude:F6}, {clickedLocation.Longitude:F6}");
                
                foreach (var pin in ViewModel.Pins)
                {
                    if (pin?.Location != null && pin.BindingContext is Models.Domain.Spot spot)
                    {
                        var distance = CalculateDistance(
                            clickedLocation.Latitude, clickedLocation.Longitude,
                            pin.Location.Latitude, pin.Location.Longitude);
                        
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] CheckNearbyPinsFromMapClick: Spot '{spot.Name}' at ({pin.Location.Latitude:F6}, {pin.Location.Longitude:F6})");
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Distance = {distance:F6}km (tolerance: {toleranceKm:F6}km) - Match: {distance <= toleranceKm}");
                        
                        if (distance <= toleranceKm)
                        {
                            // Found a nearby spot - show mini window
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] ✓ Found nearby spot via MapClick: {spot.Name} at distance {distance:F6}km");
                            
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                try
                                {
                                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Executing ShowSpotMiniWindowCommand from MapClick");
                                    ViewModel.ShowSpotMiniWindowCommand?.Execute(spot);
                                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ShowSpotMiniWindowCommand executed successfully from MapClick");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[ERROR] ShowSpotMiniWindowCommand execution failed in MapClick: {ex.Message}");
                                }
                            });
                            
                            return true; // Found a spot
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] No pins found within {toleranceKm}km tolerance");
                return false; // No spots found
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CheckNearbyPinsFromMapClick failed: {ex.Message}");
                return false;
            }
        }

        
        // Calculate distance between two coordinates using Haversine formula
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Validate input coordinates
            if (double.IsNaN(lat1) || double.IsNaN(lon1) || double.IsNaN(lat2) || double.IsNaN(lon2))
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Invalid coordinates: ({lat1}, {lon1}) to ({lat2}, {lon2})");
                return double.MaxValue; // Return large distance for invalid coordinates
            }
            
            const double earthRadiusKm = 6371.0;
            
            // Convert degrees to radians
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            
            double lat1Rad = lat1 * Math.PI / 180.0;
            double lat2Rad = lat2 * Math.PI / 180.0;
            
            // Haversine formula
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                      Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            double distance = earthRadiusKm * c;
            
            // Debug for very close distances to verify calculation
            if (distance < 0.001) // Less than 1 meter
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Very close distance calculated: {distance * 1000:F1}m between ({lat1:F6}, {lon1:F6}) and ({lat2:F6}, {lon2:F6})");
            }
            
            // Test calculation with known values for validation
            if (Math.Abs(lat1 - lat2) < 0.000001 && Math.Abs(lon1 - lon2) < 0.000001)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Identical coordinates detected: distance = {distance * 1000:F3}m (should be ~0)");
            }
            
            return distance;
        }
        
        private double CalculateDynamicTolerance()
        {
            try
            {
                if (MainMap?.VisibleRegion == null)
                {
                    return 2.0; // Default fallback: 2km - more generous
                }
                
                var visibleRegion = MainMap.VisibleRegion;
                
                // Calculate approximate zoom level based on visible region
                var latSpan = visibleRegion.LatitudeDegrees;
                var lonSpan = visibleRegion.LongitudeDegrees;
                
                // Average span to determine zoom level
                var avgSpan = (latSpan + lonSpan) / 2;
                
                // Calculate tolerance based on visible area - Mobile-optimized thresholds
                // When zoomed out (large span), increase tolerance
                // When zoomed in (small span), decrease tolerance
                double tolerance;
                
                if (avgSpan > 1.0) // Very zoomed out
                {
                    tolerance = 5.0; // 5km tolerance for very wide view - generous for overview
                }
                else if (avgSpan > 0.5) // Moderately zoomed out
                {
                    tolerance = 3.0; // 3km tolerance - comfortable regional view
                }
                else if (avgSpan > 0.1) // Medium zoom
                {
                    tolerance = 2.0; // 2km tolerance - normal usage level
                }
                else if (avgSpan > 0.05) // Good zoom
                {
                    tolerance = 1.0; // 1km tolerance - focused view
                }
                else // Very zoomed in
                {
                    tolerance = 0.5; // 500m tolerance for precise view - still usable
                }
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Mobile-optimized tolerance: {tolerance}km (visible span: {avgSpan:F4} degrees)");
                
                return tolerance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to calculate dynamic tolerance: {ex.Message}");
                return 2.0; // Safe fallback - more generous than before
            }
        }

        private async void OnMapTapped(object sender, TappedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] OnMapTapped: Direct gesture detected");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] OnMapTapped: ViewModel.Pins count: {ViewModel?.Pins?.Count ?? 0}");
                
                if (MainMap == null || ViewModel?.Pins == null)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] OnMapTapped: Map or pins not available");
                    return;
                }

                // Get the position relative to the map
                var position = e.GetPosition(MainMap);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] OnMapTapped: Screen position {position?.X:F2}, {position?.Y:F2}");

                if (position == null) return;

                // Try to convert screen position to map coordinates
                // This is an approximation - exact conversion would require platform-specific code
                var mapBounds = MainMap.VisibleRegion;
                if (mapBounds != null)
                {
                    var mapWidth = MainMap.Width;
                    var mapHeight = MainMap.Height;
                    
                    if (mapWidth > 0 && mapHeight > 0)
                    {
                        // Calculate approximate map coordinates from screen position
                        var relativeX = position.Value.X / mapWidth;
                        var relativeY = position.Value.Y / mapHeight;
                        
                        var latSpan = mapBounds.LatitudeDegrees;
                        var lonSpan = mapBounds.LongitudeDegrees;
                        
                        var clickedLat = mapBounds.Center.Latitude - (latSpan / 2) + (relativeY * latSpan);
                        var clickedLon = mapBounds.Center.Longitude - (lonSpan / 2) + (relativeX * lonSpan);
                        
                        var clickedLocation = new Location(clickedLat, clickedLon);
                        
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] OnMapTapped: Converted to map coordinates {clickedLat:F6}, {clickedLon:F6}");
                        
                        // Now check for nearby pins with more generous tolerance
                        await CheckNearbyPins(clickedLocation);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] OnMapTapped failed: {ex.Message}");
            }
        }

        private async Task CheckNearbyPins(Location clickedLocation)
        {
            try
            {
                var foundNearbySpot = false;
                var toleranceKm = CalculateDynamicTolerance() * 1.5; // 50% more generous for gestures
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] CheckNearbyPins: Checking {ViewModel.Pins.Count} pins with {toleranceKm}km tolerance");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Gesture click location: {clickedLocation.Latitude:F6}, {clickedLocation.Longitude:F6}");
                
                foreach (var pin in ViewModel.Pins)
                {
                    if (pin?.Location != null && pin.BindingContext is Models.Domain.Spot spot)
                    {
                        var distance = CalculateDistance(
                            clickedLocation.Latitude, clickedLocation.Longitude,
                            pin.Location.Latitude, pin.Location.Longitude);
                        
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] CheckNearbyPins: Spot '{spot.Name}' at ({pin.Location.Latitude:F6}, {pin.Location.Longitude:F6})");
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Distance = {distance:F6}km (tolerance: {toleranceKm:F6}km) - Match: {distance <= toleranceKm}");
                        
                        if (distance <= toleranceKm)
                        {
                            // Found a nearby spot - show mini window
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] ✓ Found nearby spot via gesture: {spot.Name} at distance {distance:F6}km");
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] About to execute ShowSpotMiniWindowCommand");
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] Command exists: {ViewModel.ShowSpotMiniWindowCommand != null}");
                            
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                try
                                {
                                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Executing ShowSpotMiniWindowCommand on UI thread");
                                    ViewModel.ShowSpotMiniWindowCommand?.Execute(spot);
                                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ShowSpotMiniWindowCommand executed successfully");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[ERROR] ShowSpotMiniWindowCommand execution failed: {ex.Message}");
                                }
                            });
                            foundNearbySpot = true;
                            break;
                        }
                    }
                }
                
                if (!foundNearbySpot)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] No pins found within {toleranceKm}km tolerance via gesture");
                    
                    // Close mini window if open (tap-to-close on empty map)
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (ViewModel != null && ViewModel.IsSpotMiniWindowVisible)
                        {
                            System.Diagnostics.Debug.WriteLine("[DEBUG] Closing mini window - tapped empty map area");
                            ViewModel.CloseSpotMiniWindowCommand?.Execute(null);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CheckNearbyPins failed: {ex.Message}");
            }
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
                    
                    // Run initialization in background to prevent UI blocking
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ViewModel.InitializeAsync();
                            System.Diagnostics.Debug.WriteLine("[INFO] ViewModel initialization completed");
                            
                            // Schedule UI updates on main thread
                            await Application.Current.Dispatcher.DispatchAsync(async () =>
                            {
                                // Give map time to render before positioning
                                await Task.Delay(100);
                                
                                // Force map refresh to ensure pins are displayed
                                System.Diagnostics.Debug.WriteLine("[INFO] Forcing map refresh");
                                ViewModel.ForceMapRefresh();
                                
                                // Update map position after initialization
                                System.Diagnostics.Debug.WriteLine("[INFO] Updating map position");
                                UpdateMapPosition();
                                System.Diagnostics.Debug.WriteLine("[INFO] Map position update completed");
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] Background initialization failed: {ex.Message}");
                            
                            // Show error on main thread
                            await Application.Current.Dispatcher.DispatchAsync(async () =>
                            {
                                // Simple error handling without complex UI operations
                                System.Diagnostics.Debug.WriteLine($"[ERROR] MapPage background task failed: {ex.Message}");
                            });
                        }
                    });
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
        
        private async void OnMenuClicked(object sender, EventArgs e)
        {
            try
            {
                // Ouvrir le flyout menu manuellement
                var shell = Shell.Current;
                if (shell != null)
                {
                    shell.FlyoutIsPresented = true;
                    System.Diagnostics.Debug.WriteLine("[INFO] Menu flyout ouvert manuellement via ToolbarItem");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Erreur ouverture menu manuel: {ex.Message}");
            }
        }
    }
}