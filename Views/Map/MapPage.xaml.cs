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

            if (MainMap != null)
            {
                MainMap.Loaded += OnMapLoaded;
                
                // Add gesture recognizer to capture all map touches
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += OnMapTapped;
                MainMap.GestureRecognizers.Add(tapGesture);
                
                System.Diagnostics.Debug.WriteLine("[DEBUG] Added gesture recognizer to map for direct touch handling");
            }

            if (_platformMapService != null)
            {
                _platformMapService.PinClicked += OnPlatformPinClicked;
            }
        }

        private void OnMapLoaded(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[INFO] Map loaded event fired");
            _isMapLoaded = true;

            if (SpotMiniWindow != null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] SpotMiniWindow found and ready");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[ERROR] SpotMiniWindow is null!");
            }

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

            if (ViewModel != null)
            {
                ViewModel.InitializeMapPosition();
                UpdateMapPosition();
                
                // Subscribe to spots changes to update custom markers
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
                UpdateCustomMarkers();
            }
        }
        
        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.Spots))
            {
                UpdateCustomMarkers();
            }
        }
        
        private void UpdateCustomMarkers()
        {
            try
            {
                if (MainMap?.MapElements == null || ViewModel?.Spots == null)
                    return;
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] UpdateCustomMarkers: Adding {ViewModel.Spots.Count} custom markers");
                
                // Clear existing markers
                MainMap.MapElements.Clear();
                
                // Add custom circle markers for each spot
                foreach (var spot in ViewModel.Spots)
                {
                    try
                    {
                        double lat = Convert.ToDouble(spot.Latitude);
                        double lon = Convert.ToDouble(spot.Longitude);
                        
                        // Validate coordinates
                        if (double.IsNaN(lat) || double.IsInfinity(lat) || lat < -90 || lat > 90 ||
                            double.IsNaN(lon) || double.IsInfinity(lon) || lon < -180 || lon > 180)
                        {
                            System.Diagnostics.Debug.WriteLine($"[WARNING] Invalid coordinates for spot {spot.Name}: {lat}, {lon}");
                            continue;
                        }
                        
                        var circle = new Microsoft.Maui.Controls.Maps.Circle
                        {
                            Center = new Location(lat, lon),
                            Radius = Distance.FromMeters(100), // 100m radius circle
                            StrokeColor = Colors.Blue,
                            StrokeWidth = 3,
                            FillColor = Color.FromArgb("#4000BFFF") // Semi-transparent blue
                        };
                        
                        // Store spot reference for click detection
                        circle.ClassId = spot.Id.ToString();
                        
                        MainMap.MapElements.Add(circle);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Added custom marker for spot {spot.Name} at {lat}, {lon}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to create marker for spot {spot.Name}: {ex.Message}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] UpdateCustomMarkers completed: {MainMap.MapElements.Count} markers added");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] UpdateCustomMarkers failed: {ex.Message}");
            }
        }

        private void OnPlatformPinClicked(object sender, MapPinClickedEventArgs e)
        {
            try
            {
                if (e.Pin?.BindingContext is Models.Domain.Spot spot)
                {
                    System.Diagnostics.Debug.WriteLine($"[INFO] Pin clicked: {spot.Name}");
                    ViewModel?.ShowSpotMiniWindowCommand?.Execute(spot);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] OnPlatformPinClicked failed: {ex.Message}");
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
                if (ViewModel?.PinSelectionService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[WARNING] PinSelectionService not available, falling back to original logic");
                    return await CheckNearbyPinsFromMapClickLegacy(clickedLocation);
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Using PinSelectionService for map click at {clickedLocation.Latitude:F6}, {clickedLocation.Longitude:F6}");
                
                var selectedSpot = await ViewModel.PinSelectionService.SelectPinAsync(
                    clickedLocation, 
                    ViewModel.Pins,
                    MainMap?.VisibleRegion,
                    ViewModel.Pins?.Count ?? 0);

                if (selectedSpot != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✓ PinSelectionService found spot: {selectedSpot.Name}");
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        try
                        {
                            ViewModel.ShowSpotMiniWindowCommand?.Execute(selectedSpot);
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] ShowSpotMiniWindowCommand executed via PinSelectionService");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] ShowSpotMiniWindowCommand execution failed: {ex.Message}");
                        }
                    });
                    
                    return true;
                }
                
                System.Diagnostics.Debug.WriteLine("[DEBUG] PinSelectionService found no nearby spots");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CheckNearbyPinsFromMapClick failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> CheckNearbyPinsFromMapClickLegacy(Location clickedLocation)
        {
            try
            {
                // Use dynamic tolerance based on zoom level
                var toleranceKm = CalculateDynamicTolerance();
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] CheckNearbyPinsFromMapClickLegacy: Checking {ViewModel.Spots?.Count ?? 0} spots with {toleranceKm}km tolerance");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Click location: {clickedLocation.Latitude:F6}, {clickedLocation.Longitude:F6}");
                
                if (ViewModel?.Spots == null)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] No spots available for checking");
                    return false;
                }
                
                foreach (var spot in ViewModel.Spots)
                {
                    if (spot != null)
                    {
                        try
                        {
                            double lat = Convert.ToDouble(spot.Latitude);
                            double lon = Convert.ToDouble(spot.Longitude);
                            
                            var distance = CalculateDistance(
                                clickedLocation.Latitude, clickedLocation.Longitude,
                                lat, lon);
                            
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] CheckNearbyPinsFromMapClickLegacy: Spot '{spot.Name}' at ({lat:F6}, {lon:F6})");
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] Distance = {distance:F6}km (tolerance: {toleranceKm:F6}km) - Match: {distance <= toleranceKm}");
                            
                            if (distance <= toleranceKm)
                            {
                                // Found a nearby spot - show mini window
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✓ Found nearby spot via MapClick (legacy): {spot.Name} at distance {distance:F6}km");
                                
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    try
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Executing ShowSpotMiniWindowCommand from MapClick (legacy)");
                                        ViewModel.ShowSpotMiniWindowCommand?.Execute(spot);
                                        System.Diagnostics.Debug.WriteLine($"[DEBUG] ShowSpotMiniWindowCommand executed successfully from MapClick (legacy)");
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[ERROR] ShowSpotMiniWindowCommand execution failed in MapClick (legacy): {ex.Message}");
                                    }
                                });
                                
                                return true; // Found a spot
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] Error processing spot {spot.Name}: {ex.Message}");
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] No spots found within {toleranceKm}km tolerance (legacy)");
                return false; // No spots found
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CheckNearbyPinsFromMapClickLegacy failed: {ex.Message}");
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
                double tolerance;
                
                if (avgSpan > 1.0) // Very zoomed out
                {
                    tolerance = 5.0; // 5km tolerance for very wide view
                }
                else if (avgSpan > 0.5) // Moderately zoomed out
                {
                    tolerance = 3.0; // 3km tolerance
                }
                else if (avgSpan > 0.1) // Medium zoom
                {
                    tolerance = 2.0; // 2km tolerance
                }
                else if (avgSpan > 0.05) // Good zoom
                {
                    tolerance = 1.0; // 1km tolerance
                }
                else // Very zoomed in
                {
                    tolerance = 0.5; // 500m tolerance
                }
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Mobile-optimized tolerance: {tolerance}km (visible span: {avgSpan:F4} degrees)");
                
                return tolerance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to calculate dynamic tolerance: {ex.Message}");
                return 2.0; // Safe fallback
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
                if (ViewModel?.PinSelectionService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[WARNING] PinSelectionService not available for gesture, falling back to legacy logic");
                    await CheckNearbyPinsLegacy(clickedLocation);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Using PinSelectionService for gesture at {clickedLocation.Latitude:F6}, {clickedLocation.Longitude:F6}");
                
                var selectedSpot = await ViewModel.PinSelectionService.SelectPinAsync(
                    clickedLocation, 
                    ViewModel.Pins,
                    MainMap?.VisibleRegion,
                    ViewModel.Pins?.Count ?? 0);

                if (selectedSpot != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✓ PinSelectionService found spot via gesture: {selectedSpot.Name}");
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        try
                        {
                            ViewModel.ShowSpotMiniWindowCommand?.Execute(selectedSpot);
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] ShowSpotMiniWindowCommand executed via PinSelectionService (gesture)");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] ShowSpotMiniWindowCommand execution failed (gesture): {ex.Message}");
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] PinSelectionService found no nearby spots via gesture");
                    
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

        private async Task CheckNearbyPinsLegacy(Location clickedLocation)
        {
            try
            {
                var foundNearbySpot = false;
                var toleranceKm = CalculateDynamicTolerance() * 1.5; // 50% more generous for gestures
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] CheckNearbyPinsLegacy: Checking {ViewModel.Spots?.Count ?? 0} spots with {toleranceKm}km tolerance");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Gesture click location: {clickedLocation.Latitude:F6}, {clickedLocation.Longitude:F6}");
                
                if (ViewModel?.Spots == null)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] No spots available for gesture checking");
                    return;
                }
                
                foreach (var spot in ViewModel.Spots)
                {
                    if (spot != null)
                    {
                        try
                        {
                            double lat = Convert.ToDouble(spot.Latitude);
                            double lon = Convert.ToDouble(spot.Longitude);
                            
                            var distance = CalculateDistance(
                                clickedLocation.Latitude, clickedLocation.Longitude,
                                lat, lon);
                            
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] CheckNearbyPinsLegacy: Spot '{spot.Name}' at ({lat:F6}, {lon:F6})");
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] Distance = {distance:F6}km (tolerance: {toleranceKm:F6}km) - Match: {distance <= toleranceKm}");
                            
                            if (distance <= toleranceKm)
                            {
                                // Found a nearby spot - show mini window
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✓ Found nearby spot via gesture (legacy): {spot.Name} at distance {distance:F6}km");
                                
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    try
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Executing ShowSpotMiniWindowCommand on UI thread (legacy)");
                                        ViewModel.ShowSpotMiniWindowCommand?.Execute(spot);
                                        System.Diagnostics.Debug.WriteLine($"[DEBUG] ShowSpotMiniWindowCommand executed successfully (legacy)");
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[ERROR] ShowSpotMiniWindowCommand execution failed (legacy): {ex.Message}");
                                    }
                                });
                                foundNearbySpot = true;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] Error processing spot {spot.Name} in gesture: {ex.Message}");
                        }
                    }
                }
                
                if (!foundNearbySpot)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] No spots found within {toleranceKm}km tolerance via gesture (legacy)");
                    
                    // Close mini window if open (tap-to-close on empty map)
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (ViewModel != null && ViewModel.IsSpotMiniWindowVisible)
                        {
                            System.Diagnostics.Debug.WriteLine("[DEBUG] Closing mini window - tapped empty map area (legacy)");
                            ViewModel.CloseSpotMiniWindowCommand?.Execute(null);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CheckNearbyPinsLegacy failed: {ex.Message}");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            System.Diagnostics.Debug.WriteLine("[INFO] MapPage OnAppearing called");

            if (ViewModel != null)
            {
                await ViewModel.InitializeAsync();
                await Application.Current.Dispatcher.DispatchAsync(() =>
                {
                    ViewModel.ForceMapRefresh();
                    UpdateMapPosition();
                });
            }
        }

        private void UpdateMapPosition()
        {
            if (ViewModel != null && MainMap != null)
            {
                try
                {
                    var position = new Location(ViewModel.MapLatitude, ViewModel.MapLongitude);
                    var distanceKm = Math.Max(1, 20 - ViewModel.MapZoomLevel);
                    var span = MapSpan.FromCenterAndRadius(position, Distance.FromKilometers(distanceKm));
                    MainMap.MoveToRegion(span);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to update map position: {ex.Message}");
                }
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (MainMap != null)
            {
                MainMap.Loaded -= OnMapLoaded;
            }

            if (_platformMapService != null)
            {
                _platformMapService.PinClicked -= OnPlatformPinClicked;
            }

            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            System.Diagnostics.Debug.WriteLine("[INFO] MapPage OnDisappearing - resources cleaned up");
        }

        private void OnMapRegionChanged(object sender, EventArgs e)
        {
            if (ViewModel != null && MainMap != null)
            {
                ViewModel.VisibleRegion = MainMap.VisibleRegion;
            }
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            try
            {
                var shell = Shell.Current;
                if (shell != null)
                {
                    shell.FlyoutIsPresented = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Erreur ouverture menu manuel: {ex.Message}");
            }
        }
    }
}
