using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.ViewModels.Map;
using SubExplore.Services.Interfaces;
using SubExplore.Models.Domain;
using System.Linq;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SubExplore.Views.Spots;
using SubExplore.ViewModels.Spots;

namespace SubExplore.Views.Map
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MapPage : ContentPage
    {
        private MapViewModel? ViewModel => BindingContext as MapViewModel;
        private bool _isMapLoaded = false;
        private readonly IPlatformMapService? _platformMapService;
        private readonly ILogger<MapPage> _logger;
        private System.Timers.Timer? _markerUpdateTimer;
        private System.Timers.Timer? _regionMonitorTimer;
        private MapSpan? _lastKnownRegion;

        public MapPage(MapViewModel viewModel, IPlatformMapService platformMapService = null, ILogger<MapPage> logger = null)
        {
            InitializeComponent();
            BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _platformMapService = platformMapService;
            _logger = logger;

            if (MainMap != null)
            {
                MainMap.Loaded += OnMapLoaded;
                
                // Add gesture recognizer to capture all map touches
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += OnMapTapped;
                MainMap.GestureRecognizers.Add(tapGesture);
                
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

            if (EntityMiniWindow != null)
            {
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[ERROR] EntityMiniWindow is null!");
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
                // Note: InitializeMapPosition removed in new architecture
                UpdateMapPosition();
                
                // Subscribe to spots changes to update custom markers
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
                UpdateCustomMarkers();
                
                // Start monitoring region changes
                StartRegionMonitoring();
            }
        }
        
        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.PracticeSpots) ||
                e.PropertyName == nameof(ViewModel.Organizations) ||
                e.PropertyName == nameof(ViewModel.Businesses) ||
                e.PropertyName == nameof(ViewModel.Pins))
            {
                UpdateCustomMarkers();
            }
        }
        
        private void UpdateCustomMarkers()
        {
            try
            {
                if (MainMap?.MapElements == null || ViewModel?.Pins == null)
                {
                    return;
                }
                
                
                // Clear existing markers
                MainMap.MapElements.Clear();
                
                // Add markers for each entity type
                CreateMarkersFromEntities();
                
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] UpdateCustomMarkers failed: {ex.Message}");
            }
        }
        
        private double CalculateDynamicMarkerSize()
        {
            try
            {
                if (MainMap?.VisibleRegion == null)
                {
                    return 150; // Default fallback size
                }
                
                var visibleRegion = MainMap.VisibleRegion;
                var latSpan = visibleRegion.LatitudeDegrees;
                var lonSpan = visibleRegion.LongitudeDegrees;
                var avgSpan = (latSpan + lonSpan) / 2;
                
                // Calculate marker size based on visible area
                // The idea is: smaller visible area (zoomed in) = smaller markers
                // Larger visible area (zoomed out) = larger markers
                double markerRadius;
                
                if (avgSpan < 0.001) // Very zoomed in (street level)
                {
                    markerRadius = 8; // 8m - très petit pour vue détaillée
                }
                else if (avgSpan < 0.005) // Zoomed in (neighborhood level)
                {
                    markerRadius = 15; // 15m - petit pour vue de quartier
                }
                else if (avgSpan < 0.01) // Medium zoom (district level)
                {
                    markerRadius = 30; // 30m - taille normale
                }
                else if (avgSpan < 0.05) // Zoomed out (city level)
                {
                    markerRadius = 80; // 80m - plus grand pour visibilité
                }
                else if (avgSpan < 0.1) // More zoomed out (city region)
                {
                    markerRadius = 200; // 200m - grand pour vue régionale
                }
                else if (avgSpan < 0.5) // Country level
                {
                    markerRadius = 500; // 500m - très grand pour vue nationale
                }
                else if (avgSpan < 1.0) // Large country level
                {
                    markerRadius = 1200; // 1.2km - énorme pour vue continentale
                }
                else if (avgSpan < 2.0) // Continental level
                {
                    markerRadius = 2500; // 2.5km - très énorme pour vue continentale
                }
                else // World level - très dézoomé
                {
                    markerRadius = 5000; // 5km - maximum pour vue mondiale
                }
                
                return markerRadius;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to calculate dynamic marker size: {ex.Message}");
                return 150; // Safe fallback
            }
        }
        
        private int CalculateDynamicStrokeWidth()
        {
            try
            {
                if (MainMap?.VisibleRegion == null)
                {
                    return 3; // Default stroke width
                }
                
                var visibleRegion = MainMap.VisibleRegion;
                var avgSpan = (visibleRegion.LatitudeDegrees + visibleRegion.LongitudeDegrees) / 2;
                
                // Adjust stroke width based on zoom level
                if (avgSpan < 0.01) // Very zoomed in
                {
                    return 2; // Fin pour vue détaillée
                }
                else if (avgSpan < 0.1) // Medium zoom
                {
                    return 3; // Normal
                }
                else if (avgSpan < 0.5) // Zoomed out
                {
                    return 4; // Plus épais pour visibilité
                }
                else // Very zoomed out
                {
                    return 5; // Maximum pour vue globale
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to calculate stroke width: {ex.Message}");
                return 3; // Safe fallback
            }
        }
        
        private void CreateMarkersFromEntities()
        {
            try
            {
                var markerRadius = CalculateDynamicMarkerSize();
                
                // Create markers for Spots
                if (ViewModel?.PracticeSpots != null)
                {
                    foreach (var spot in ViewModel.PracticeSpots)
                    {
                        CreateMarkerFromPracticeSpot(spot, markerRadius);
                    }
                }
                
                // Create markers for Spots
                if (ViewModel?.Organizations != null)
                {
                    foreach (var org in ViewModel.Organizations)
                    {
                        CreateMarkerFromOrganization(org, markerRadius);
                    }
                }
                
                // Create markers for Spots
                if (ViewModel?.Businesses != null)
                {
                    foreach (var business in ViewModel.Businesses)
                    {
                        CreateMarkerFromBusiness(business, markerRadius);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CreateMarkersFromEntities failed: {ex.Message}");
            }
        }
        
        private void CreateMarkerFromPracticeSpot(Models.Supabase.SupabasePracticeSpot spot, double markerRadius)
        {
            try
            {
                double lat = (double)spot.Latitude;
                double lon = (double)spot.Longitude;
                
                // Validate coordinates
                if (double.IsNaN(lat) || double.IsInfinity(lat) || lat < -90 || lat > 90 ||
                    double.IsNaN(lon) || double.IsInfinity(lon) || lon < -180 || lon > 180)
                {
                    System.Diagnostics.Debug.WriteLine($"[WARNING] Invalid coordinates for practice spot {spot.Name}: {lat}, {lon}");
                    return;
                }
                
                var spotColor = GetMarkerColorForPracticeSpot(spot);
                CreateDoubleCircleMarker(lat, lon, markerRadius, spotColor, spot.Id.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to create marker for practice spot {spot.Name}: {ex.Message}");
            }
        }
        
        private void CreateMarkerFromOrganization(Models.Supabase.SupabaseOrganization org, double markerRadius)
        {
            try
            {
                double lat = (double)org.Latitude;
                double lon = (double)org.Longitude;
                
                // Validate coordinates
                if (double.IsNaN(lat) || double.IsInfinity(lat) || lat < -90 || lat > 90 ||
                    double.IsNaN(lon) || double.IsInfinity(lon) || lon < -180 || lon > 180)
                {
                    System.Diagnostics.Debug.WriteLine($"[WARNING] Invalid coordinates for organization {org.Name}: {lat}, {lon}");
                    return;
                }
                
                var orgColor = GetMarkerColorForOrganization(org);
                CreateDoubleCircleMarker(lat, lon, markerRadius, orgColor, org.Id.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to create marker for organization {org.Name}: {ex.Message}");
            }
        }
        
        private void CreateMarkerFromBusiness(Models.Supabase.SupabaseBusiness business, double markerRadius)
        {
            try
            {
                double lat = (double)business.Latitude;
                double lon = (double)business.Longitude;
                
                // Validate coordinates
                if (double.IsNaN(lat) || double.IsInfinity(lat) || lat < -90 || lat > 90 ||
                    double.IsNaN(lon) || double.IsInfinity(lon) || lon < -180 || lon > 180)
                {
                    System.Diagnostics.Debug.WriteLine($"[WARNING] Invalid coordinates for business {business.Name}: {lat}, {lon}");
                    return;
                }
                
                var businessColor = GetMarkerColorForBusiness(business);
                CreateDoubleCircleMarker(lat, lon, markerRadius, businessColor, business.Id.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to create marker for business {business.Name}: {ex.Message}");
            }
        }
        
        private void CreateDoubleCircleMarker(double lat, double lon, double markerRadius, Color color, string id)
        {
            try
            {
                // Create a double-circle system for maximum visibility
                // 1. Outer circle - white background for contrast
                var outerCircle = new Microsoft.Maui.Controls.Maps.Circle
                {
                    Center = new Location(lat, lon),
                    Radius = Distance.FromMeters(markerRadius * 1.15), // 15% larger
                    StrokeColor = Colors.DarkGray,
                    StrokeWidth = 1,
                    FillColor = Colors.White
                };
                
                // 2. Inner circle - colored for entity type
                var innerCircle = new Microsoft.Maui.Controls.Maps.Circle
                {
                    Center = new Location(lat, lon),
                    Radius = Distance.FromMeters(markerRadius),
                    StrokeColor = Colors.DarkGray,
                    StrokeWidth = 1,
                    FillColor = color
                };
                
                // Store entity reference for click detection
                outerCircle.ClassId = id;
                innerCircle.ClassId = id;
                
                // Add both circles
                MainMap.MapElements.Add(outerCircle);
                MainMap.MapElements.Add(innerCircle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CreateDoubleCircleMarker failed: {ex.Message}");
            }
        }
        
        private Color GetMarkerColorForPracticeSpot(Models.Supabase.SupabasePracticeSpot spot)
        {
            try
            {
                // Dans la nouvelle architecture 3-tables, les practice spots utilisent une couleur fixe
                // Couleur bleu pour les spots de pratique
                return Color.FromArgb("#0077FF"); // Bleu vif pour les spots de pratique
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to get practice spot color: {ex.Message}");
                return Color.FromArgb("#0077FF"); // Safe fallback
            }
        }
        
        private Color GetMarkerColorForOrganization(Models.Supabase.SupabaseOrganization org)
        {
            try
            {
                // Couleur verte pour les organisations
                return Color.FromArgb("#00CC55");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to get organization color: {ex.Message}");
                return Color.FromArgb("#00CC55"); // Safe fallback
            }
        }
        
        private Color GetMarkerColorForBusiness(Models.Supabase.SupabaseBusiness business)
        {
            try
            {
                // Couleurs selon le type de business (BusinessType est maintenant un string dans Supabase)
                return business.BusinessType?.ToLowerInvariant() switch
                {
                    "dive_shop" or "diveshop" => Color.FromArgb("#FF7700"), // Orange pour dive shops
                    "equipment_rental" or "equipmentrental" => Color.FromArgb("#AA00FF"), // Violet pour location
                    "boat_charter" or "boatcharter" => Color.FromArgb("#FF4444"), // Rouge pour bateaux
                    _ => Color.FromArgb("#FFAA00") // Jaune par défaut pour commerces
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to get business color: {ex.Message}");
                return Color.FromArgb("#FFAA00"); // Safe fallback
            }
        }

        private async void OnPlatformPinClicked(object sender, MapPinClickedEventArgs e)
        {
            try
            {
                if (e.Pin?.BindingContext != null)
                {
                    var context = (dynamic)e.Pin.BindingContext;
                    string type = context.Type;
                    var data = context.Data;
                    System.Diagnostics.Debug.WriteLine($"[INFO] Pin clicked: {type}");
                    
                    // Show mini window with entity data
                    ViewModel?.ShowEntityMiniWindowCommand?.Execute(data);
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
                
                if (clickedLocation == null || ViewModel?.Pins == null)
                {
                    return;
                }

                // Check if click is near any pin using enhanced detection
                var foundNearbySpot = await CheckNearbyPinsFromMapClick(clickedLocation);
                
                if (!foundNearbySpot)
                {

                    // Close mini window if open (clicking empty space)
                    if (ViewModel != null && ViewModel.IsSpotMiniWindowVisible)
                    {
                        ViewModel.CloseSpotMiniWindowCommand?.Execute(null);
                    }

                    // ✅ NOUVELLE FONCTIONNALITÉ: Ajouter spot au clic sur carte
                    // Si pas de spot trouvé à proximité, proposer d'ajouter un nouveau spot
                    await PromptAddSpotAtLocation(clickedLocation);
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
                // Note: PinSelectionService not implemented in new architecture, using legacy approach
                return await CheckNearbyPinsFromMapClickLegacy(clickedLocation);
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
                
                
                // Check all entity collections for nearby items
                var allEntities = new List<object>();
                if (ViewModel?.PracticeSpots != null)
                    allEntities.AddRange(ViewModel.PracticeSpots.Cast<object>());
                if (ViewModel?.Organizations != null)
                    allEntities.AddRange(ViewModel.Organizations.Cast<object>());
                if (ViewModel?.Businesses != null)
                    allEntities.AddRange(ViewModel.Businesses.Cast<object>());
                
                if (!allEntities.Any())
                {
                    return false;
                }
                
                foreach (var entity in allEntities)
                {
                    if (entity != null)
                    {
                        try
                        {
                            double lat, lon;
                            string name;
                            
                            // Extract coordinates based on entity type
                            switch (entity)
                            {
                                case Models.Supabase.SupabasePracticeSpot spot:
                                    lat = (double)spot.Latitude;
                                    lon = (double)spot.Longitude;
                                    name = spot.Name;
                                    break;
                                case Models.Supabase.SupabaseOrganization org:
                                    lat = (double)org.Latitude;
                                    lon = (double)org.Longitude;
                                    name = org.Name;
                                    break;
                                case Models.Supabase.SupabaseBusiness business:
                                    lat = (double)business.Latitude;
                                    lon = (double)business.Longitude;
                                    name = business.Name;
                                    break;
                                default:
                                    continue;
                            }
                            
                            var distance = CalculateDistance(
                                clickedLocation.Latitude, clickedLocation.Longitude,
                                lat, lon);
                            
                            
                            if (distance <= toleranceKm)
                            {
                                // Found a nearby entity - navigate to details
                                
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    try
                                    {
                                        // Show mini window instead of direct navigation
                                        ViewModel.ShowEntityMiniWindowCommand?.Execute(entity);
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[ERROR] Entity details navigation failed in MapClick (legacy): {ex.Message}");
                                    }
                                });
                                
                                return true; // Found an entity
                            }
                        }
                        catch (Exception ex)
                        {
                            var entityType = entity.GetType().Name;
                            System.Diagnostics.Debug.WriteLine($"[ERROR] Error processing {entityType}: {ex.Message}");
                        }
                    }
                }
                
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
                
                // Calculate tolerance based on marker size for consistent click experience
                var markerRadius = CalculateDynamicMarkerSize();
                
                // Base tolerance on marker size + generous buffer for touch targets
                var tolerance = (markerRadius / 1000.0) * 2.0; // Convert meters to km and add 100% buffer
                
                // Ensure minimum and maximum tolerances for usability
                tolerance = Math.Max(0.1, Math.Min(tolerance, 10.0)); // Min 100m, Max 10km
                
                
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
                
                if (MainMap == null || ViewModel?.Pins == null)
                {
                    return;
                }

                // Get the position relative to the map
                var position = e.GetPosition(MainMap);

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
                // Note: PinSelectionService not implemented in new architecture, using legacy approach
                await CheckNearbyPinsLegacy(clickedLocation);
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
                
                
                // Check all entity collections for nearby items
                var allEntities = new List<object>();
                if (ViewModel?.PracticeSpots != null)
                    allEntities.AddRange(ViewModel.PracticeSpots.Cast<object>());
                if (ViewModel?.Organizations != null)
                    allEntities.AddRange(ViewModel.Organizations.Cast<object>());
                if (ViewModel?.Businesses != null)
                    allEntities.AddRange(ViewModel.Businesses.Cast<object>());
                
                if (!allEntities.Any())
                {
                    return;
                }
                
                foreach (var entity in allEntities)
                {
                    if (entity != null)
                    {
                        try
                        {
                            double lat, lon;
                            string name;
                            
                            // Extract coordinates based on entity type
                            switch (entity)
                            {
                                case Models.Supabase.SupabasePracticeSpot spot:
                                    lat = (double)spot.Latitude;
                                    lon = (double)spot.Longitude;
                                    name = spot.Name;
                                    break;
                                case Models.Supabase.SupabaseOrganization org:
                                    lat = (double)org.Latitude;
                                    lon = (double)org.Longitude;
                                    name = org.Name;
                                    break;
                                case Models.Supabase.SupabaseBusiness business:
                                    lat = (double)business.Latitude;
                                    lon = (double)business.Longitude;
                                    name = business.Name;
                                    break;
                                default:
                                    continue;
                            }
                            
                            var distance = CalculateDistance(
                                clickedLocation.Latitude, clickedLocation.Longitude,
                                lat, lon);
                            
                            
                            if (distance <= toleranceKm)
                            {
                                // Found a nearby entity - navigate to details
                                
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    try
                                    {
                                        // Show mini window instead of direct navigation
                                        ViewModel.ShowEntityMiniWindowCommand?.Execute(entity);
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[ERROR] Entity details navigation failed (legacy gesture): {ex.Message}");
                                    }
                                });
                                foundNearbySpot = true;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            var entityType = entity.GetType().Name;
                            System.Diagnostics.Debug.WriteLine($"[ERROR] Error processing {entityType} in gesture: {ex.Message}");
                        }
                    }
                }
                
                if (!foundNearbySpot)
                {
                    // Close mini window if open (tap-to-close on empty map)
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (ViewModel != null && ViewModel.IsSpotMiniWindowVisible)
                        {
                            ViewModel.CloseSpotMiniWindowCommand?.Execute(null);
                        }
                        else
                        {
                            // ✅ NOUVELLE FONCTIONNALITÉ: Ajouter spot au tap sur carte vide
                            // Si pas de spot trouvé et pas de mini-window ouverte, proposer d'ajouter un spot
                            await PromptAddSpotAtLocation(clickedLocation);
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
                    // Note: ForceMapRefresh not implemented in new architecture
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

            // Cleanup timers
            _markerUpdateTimer?.Stop();
            _markerUpdateTimer?.Dispose();
            _markerUpdateTimer = null;
            
            _regionMonitorTimer?.Stop();
            _regionMonitorTimer?.Dispose();
            _regionMonitorTimer = null;

            System.Diagnostics.Debug.WriteLine("[INFO] MapPage OnDisappearing - resources cleaned up");
        }

        private void StartRegionMonitoring()
        {
            try
            {
                // Start a timer to monitor region changes
                _regionMonitorTimer = new System.Timers.Timer(500); // Check every 500ms
                _regionMonitorTimer.Elapsed += OnRegionMonitorTick;
                _regionMonitorTimer.Start();
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to start region monitoring: {ex.Message}");
            }
        }
        
        private void OnRegionMonitorTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (MainMap?.VisibleRegion != null && ViewModel != null)
                    {
                        var currentRegion = MainMap.VisibleRegion;
                        
                        // Check if region has changed significantly
                        if (HasRegionChanged(currentRegion))
                        {
                            _lastKnownRegion = currentRegion;
                            // Note: VisibleRegion property removed in new architecture
                            
                            // Debounce marker updates
                            _markerUpdateTimer?.Stop();
                            _markerUpdateTimer?.Dispose();
                            
                            _markerUpdateTimer = new System.Timers.Timer(300); // 300ms delay
                            _markerUpdateTimer.Elapsed += (s, args) =>
                            {
                                _markerUpdateTimer?.Stop();
                                _markerUpdateTimer?.Dispose();
                                _markerUpdateTimer = null;
                                
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    UpdateCustomMarkers();
                                });
                            };
                            _markerUpdateTimer.Start();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Region monitor tick failed: {ex.Message}");
            }
        }
        
        private bool HasRegionChanged(MapSpan currentRegion)
        {
            if (_lastKnownRegion == null) return true;
            
            // Check for significant changes in latitude/longitude span (zoom level change)
            var latDiff = Math.Abs(currentRegion.LatitudeDegrees - _lastKnownRegion.LatitudeDegrees);
            var lonDiff = Math.Abs(currentRegion.LongitudeDegrees - _lastKnownRegion.LongitudeDegrees);
            
            // Consider it changed if zoom changed significantly (>10% difference)
            return latDiff > (_lastKnownRegion.LatitudeDegrees * 0.1) || 
                   lonDiff > (_lastKnownRegion.LongitudeDegrees * 0.1);
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

        /// <summary>
        /// Custom hamburger button clicked - guaranteed flyout access
        /// </summary>
        private void OnCustomHamburgerClicked(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("[MapPage] Custom hamburger button clicked - bypassing MAUI Shell bugs");

                if (Shell.Current != null)
                {
                    Shell.Current.FlyoutIsPresented = true;
                    Debug.WriteLine("[MapPage] ✅ Flyout opened successfully via custom navigation bar");
                }
                else
                {
                    Debug.WriteLine("[MapPage] ❌ No Shell.Current available for custom hamburger");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapPage] ❌ Custom hamburger error: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NOUVELLE MÉTHODE: Proposer d'ajouter un spot à un emplacement cliqué avec sélection du type
        /// </summary>
        private async Task PromptAddSpotAtLocation(Location location)
        {
            try
            {
                if (ViewModel == null) return;

                // Afficher un ActionSheet pour choisir le type de spot
                var spotTypeChoice = await Application.Current.MainPage.DisplayActionSheet(
                    $"Ajouter un spot à cet emplacement\nCoordonnées: {location.Latitude:F6}, {location.Longitude:F6}",
                    "Annuler",
                    null,
                    "🏊 Spot de Pratique",
                    "🏢 Organisation/Club",
                    "🛍️ Commerce/Boutique"
                );

                if (spotTypeChoice != null && spotTypeChoice != "Annuler")
                {
                    // Naviguer vers la page appropriée selon le choix
                    await NavigateToSpotCreation(location, spotTypeChoice);
                    System.Diagnostics.Debug.WriteLine($"[INFO] Navigating to {spotTypeChoice} creation at: {location.Latitude}, {location.Longitude}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] PromptAddSpotAtLocation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigation vers la page de création appropriée selon le type de spot
        /// </summary>
        private async Task NavigateToSpotCreation(Location location, string spotType)
        {
            try
            {
                _logger?.LogInformation("🔍 DIAGNOSTIC: NavigateToSpotCreation START - spotType: {spotType}", spotType);

                // Test manual instantiation to check dependency injection
                try
                {
                    _logger?.LogInformation("🔍 DIAGNOSTIC: Testing manual AddSpotPage instantiation...");
                    var serviceProvider = Handler?.MauiContext?.Services ?? Application.Current?.Handler?.MauiContext?.Services;
                    if (serviceProvider != null)
                    {
                        var addSpotPage = serviceProvider.GetService<AddSpotPage>();
                        _logger?.LogInformation("🔍 DIAGNOSTIC: Manual AddSpotPage instantiation: {Result}", addSpotPage != null ? "SUCCESS" : "FAILED");

                        var viewModel = serviceProvider.GetService<RefactoredAddSpotViewModel>();
                        _logger?.LogInformation("🔍 DIAGNOSTIC: Manual RefactoredAddSpotViewModel instantiation: {Result}", viewModel != null ? "SUCCESS" : "FAILED");
                    }
                    else
                    {
                        _logger?.LogWarning("🔍 DIAGNOSTIC: ServiceProvider not available for manual testing");
                    }
                }
                catch (Exception testEx)
                {
                    _logger?.LogError(testEx, "🔍 DIAGNOSTIC: Manual instantiation test FAILED: {Error}", testEx.Message);
                }

                var parameters = new Dictionary<string, object>
                {
                    ["Latitude"] = location.Latitude,
                    ["Longitude"] = location.Longitude,
                    ["Mode"] = "create"
                };

                _logger?.LogInformation("🔍 DIAGNOSTIC: Parameters created successfully");

                switch (spotType)
                {
                    case "🏊 Spot de Pratique":
                        _logger?.LogInformation("🔍 DIAGNOSTIC: About to navigate to //addspot");
                        // Navigation vers SimpleApiAddSpotViewModel (spots de pratique)
                        await Shell.Current.GoToAsync("//addspot", parameters);
                        _logger?.LogInformation("🔍 DIAGNOSTIC: Navigation to //addspot COMPLETED");
                        break;

                    case "🏢 Organisation/Club":
                        // Navigation vers OrganizationAddViewModel (à créer)
                        await Shell.Current.GoToAsync("//addorganization", parameters);
                        break;

                    case "🛍️ Commerce/Boutique":
                        // Navigation vers BusinessAddViewModel (à créer)
                        await Shell.Current.GoToAsync("//addbusiness", parameters);
                        break;

                    default:
                        _logger?.LogWarning("🔍 DIAGNOSTIC: Unknown spot type: {spotType}", spotType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "🔍 DIAGNOSTIC: NavigateToSpotCreation EXCEPTION: {Error}", ex.Message);
                _logger?.LogError("🔍 DIAGNOSTIC: Exception StackTrace: {StackTrace}", ex.StackTrace);

                // Fallback: afficher un message d'erreur à l'utilisateur
                await Application.Current.MainPage.DisplayAlert(
                    "Erreur",
                    "Impossible d'ouvrir la page de création de spot. Veuillez réessayer.",
                    "OK"
                );
            }
        }
    }
}
