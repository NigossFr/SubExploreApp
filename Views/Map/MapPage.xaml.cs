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
        private System.Timers.Timer? _markerUpdateTimer;
        private System.Timers.Timer? _regionMonitorTimer;
        private MapSpan? _lastKnownRegion;

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
                
                // Start monitoring region changes
                StartRegionMonitoring();
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
                
                // Calculate dynamic marker size based on current zoom level
                var markerRadius = CalculateDynamicMarkerSize();
                var strokeWidth = CalculateDynamicStrokeWidth();
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Dynamic marker size: {markerRadius}m radius, {strokeWidth}px stroke");
                
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
                        
                        // Get spot type color if available, otherwise use blue
                        var spotColor = GetSpotTypeColor(spot);
                        
                        // Create a pin-shaped polygon instead of a circle
                        var pinPolygon = CreatePinShapePolygon(lat, lon, markerRadius, spotColor, strokeWidth);
                        
                        // Store spot reference for click detection
                        pinPolygon.ClassId = spot.Id.ToString();
                        
                        MainMap.MapElements.Add(pinPolygon);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Added dynamic marker for spot {spot.Name} at {lat}, {lon} (radius: {markerRadius}m)");
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
                    markerRadius = 25; // 25m - très petit pour vue détaillée
                }
                else if (avgSpan < 0.005) // Zoomed in (neighborhood level)
                {
                    markerRadius = 50; // 50m - petit pour vue de quartier
                }
                else if (avgSpan < 0.01) // Medium zoom (district level)
                {
                    markerRadius = 100; // 100m - taille normale
                }
                else if (avgSpan < 0.05) // Zoomed out (city level)
                {
                    markerRadius = 300; // 300m - plus grand pour visibilité
                }
                else if (avgSpan < 0.1) // More zoomed out (city region)
                {
                    markerRadius = 800; // 800m - grand pour vue régionale
                }
                else if (avgSpan < 0.5) // Country level
                {
                    markerRadius = 2000; // 2km - très grand pour vue nationale
                }
                else if (avgSpan < 1.0) // Large country level
                {
                    markerRadius = 5000; // 5km - énorme pour vue continentale
                }
                else if (avgSpan < 2.0) // Continental level
                {
                    markerRadius = 10000; // 10km - très énorme pour vue continentale
                }
                else // World level - très dézoomé
                {
                    markerRadius = 20000; // 20km - maximum pour vue mondiale
                }
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Calculated marker size: {markerRadius}m for zoom span {avgSpan:F6}°");
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
        
        private Color GetSpotTypeColor(Models.Domain.Spot spot)
        {
            try
            {
                // Si le spot a un type avec une couleur définie
                if (spot.Type?.ColorCode != null && !string.IsNullOrEmpty(spot.Type.ColorCode))
                {
                    return Color.FromArgb(spot.Type.ColorCode);
                }
                
                // Couleurs par défaut selon le type d'activité - Plus vives pour meilleure visibilité
                if (spot.Type != null)
                {
                    return spot.Type.Name?.ToLowerInvariant() switch
                    {
                        "plongée" or "diving" => Color.FromArgb("#0077FF"), // Bleu vif
                        "apnée" or "freediving" => Color.FromArgb("#00CC55"), // Vert vif
                        "snorkeling" or "randonnée palmée" => Color.FromArgb("#FF7700"), // Orange vif
                        "exploration" => Color.FromArgb("#AA00FF"), // Violet vif
                        "pêche" or "fishing" => Color.FromArgb("#FF4444"), // Rouge
                        "photographie" or "photography" => Color.FromArgb("#FFAA00"), // Jaune/orange
                        _ => Color.FromArgb("#0099FF") // Bleu vif par défaut
                    };
                }
                
                return Color.FromArgb("#0099FF"); // Bleu vif par défaut
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to get spot type color: {ex.Message}");
                return Color.FromArgb("#0088FF"); // Safe fallback
            }
        }
        
        private int GetAlphaForFill(double markerRadius)
        {
            // Ajuster la transparence selon la taille du marqueur
            // Plus le marqueur est grand, plus il doit être transparent pour ne pas masquer la carte
            if (markerRadius <= 50) return 200; // 78% opacity pour petits marqueurs - plus visible
            else if (markerRadius <= 100) return 180; // 70% opacity pour marqueurs normaux
            else if (markerRadius <= 500) return 160; // 63% opacity pour grands marqueurs
            else if (markerRadius <= 2000) return 140; // 55% opacity pour très grands marqueurs
            else return 120; // 47% opacity pour marqueurs énormes
        }
        
        private Microsoft.Maui.Controls.Maps.Polygon CreatePinShapePolygon(double lat, double lon, double radiusMeters, Color pinColor, int strokeWidth)
        {
            try
            {
                // Convertir le radius en degrés approximatifs (1 degré ≈ 111km)
                double radiusDegrees = radiusMeters / 111000.0;
                
                var geoPositions = new List<Location>();
                
                // Créer une forme de pin Google Maps en utilisant des coordonnées géographiques
                // Le pin est composé d'un cercle (tête) et d'un triangle pointant vers le bas
                
                double headRadius = radiusDegrees * 0.6; // Tête du pin (60% du radius total)
                double tailLength = radiusDegrees * 0.8; // Longueur de la pointe (80% du radius total)
                
                // Créer la tête circulaire du pin (partie supérieure)
                int circlePoints = 16; // Nombre de points pour approximer le cercle
                for (int i = 0; i < circlePoints; i++)
                {
                    double angle = (2 * Math.PI * i) / circlePoints;
                    double offsetLat = headRadius * Math.Cos(angle);
                    double offsetLon = headRadius * Math.Sin(angle) / Math.Cos(lat * Math.PI / 180); // Correction pour la courbure terrestre
                    
                    geoPositions.Add(new Location(lat + offsetLat, lon + offsetLon));
                }
                
                // Ajouter la pointe du pin (triangle pointant vers le centre du spot)
                geoPositions.Add(new Location(lat - tailLength, lon)); // Pointe vers le bas
                
                var polygon = new Microsoft.Maui.Controls.Maps.Polygon
                {
                    StrokeColor = Colors.White, // Contour blanc pour contraste
                    StrokeWidth = Math.Max(2, strokeWidth), // Contour plus épais pour visibilité
                    FillColor = Color.FromArgb($"#{GetAlphaForFill(radiusMeters):X2}{pinColor.ToArgbHex()[2..]}") // Couleur avec transparence
                };
                
                // Ajouter tous les points du polygone
                foreach (var position in geoPositions)
                {
                    polygon.Geopath.Add(position);
                }
                
                return polygon;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to create pin shape polygon: {ex.Message}");
                
                // Fallback: créer un petit cercle simple
                return CreateFallbackCirclePolygon(lat, lon, radiusMeters, pinColor, strokeWidth);
            }
        }
        
        private Microsoft.Maui.Controls.Maps.Polygon CreateFallbackCirclePolygon(double lat, double lon, double radiusMeters, Color pinColor, int strokeWidth)
        {
            double radiusDegrees = radiusMeters / 111000.0;
            var geoPositions = new List<Location>();
            
            // Créer un cercle simple avec 12 points
            for (int i = 0; i < 12; i++)
            {
                double angle = (2 * Math.PI * i) / 12;
                double offsetLat = radiusDegrees * Math.Cos(angle);
                double offsetLon = radiusDegrees * Math.Sin(angle) / Math.Cos(lat * Math.PI / 180);
                
                geoPositions.Add(new Location(lat + offsetLat, lon + offsetLon));
            }
            
            var polygon = new Microsoft.Maui.Controls.Maps.Polygon
            {
                StrokeColor = Colors.White,
                StrokeWidth = strokeWidth,
                FillColor = Color.FromArgb($"#{GetAlphaForFill(radiusMeters):X2}{pinColor.ToArgbHex()[2..]}")
            };
            
            foreach (var position in geoPositions)
            {
                polygon.Geopath.Add(position);
            }
            
            return polygon;
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
                
                // Calculate tolerance based on marker size for consistent click experience
                var markerRadius = CalculateDynamicMarkerSize();
                
                // Base tolerance on marker size + generous buffer for touch targets
                var tolerance = (markerRadius / 1000.0) * 2.0; // Convert meters to km and add 100% buffer
                
                // Ensure minimum and maximum tolerances for usability
                tolerance = Math.Max(0.1, Math.Min(tolerance, 10.0)); // Min 100m, Max 10km
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Dynamic tolerance: {tolerance:F3}km based on marker size {markerRadius}m (visible span: {avgSpan:F6}°)");
                
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
                
                System.Diagnostics.Debug.WriteLine("[DEBUG] Started region monitoring timer");
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
                            ViewModel.VisibleRegion = currentRegion;
                            
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
                                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Map region changed - updated marker sizes for new zoom level (monitored)");
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
    }
}
