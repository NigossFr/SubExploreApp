using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Map
{
    public partial class MapViewModel : ViewModelBase
    {
        private readonly ISpotRepository _spotRepository;
        private readonly ILocationService _locationService;
        private readonly ISpotTypeRepository _spotTypeRepository;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IPlatformMapService _platformMapService;

        // Map Configuration Constants
        private const double DEFAULT_SEARCH_RADIUS_KM = 10.0;
        private const int MAX_SPOTS_LIMIT = 100;
        private const int MIN_SPOTS_FOR_AUTO_ZOOM = 1;
        private const int MAX_SPOTS_FOR_AUTO_ZOOM = 5;
        private const double MIN_ZOOM_LEVEL = 1.0;
        private const double MAX_ZOOM_LEVEL = 18.0;
        private const int SPOTS_BATCH_SIZE = 20;
        private const int MAP_UPDATE_DELAY_MS = 500;

        [ObservableProperty]
        private ObservableCollection<Models.Domain.Spot> _spots;

        [ObservableProperty]
        private ObservableCollection<Pin> _pins;

        [ObservableProperty]
        private double _userLatitude;

        [ObservableProperty]
        private double _userLongitude;

        [ObservableProperty]
        private double _mapLatitude;

        [ObservableProperty]
        private double _mapLongitude;

        [ObservableProperty]
        private double _mapZoomLevel;

        [ObservableProperty]
        private bool _isLocationAvailable;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private ObservableCollection<SpotType> _spotTypes;

        [ObservableProperty]
        private SpotType _selectedSpotType;

        [ObservableProperty]
        private bool _isFiltering;

        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private MapSpan _visibleRegion;

        [ObservableProperty]
        private bool _isEmptyState;

        [ObservableProperty]
        private bool _isNetworkError;

        [ObservableProperty]
        private System.Threading.CancellationTokenSource _searchCancellationToken;

        public MapViewModel(
            ISpotRepository spotRepository,
            ILocationService locationService,
            ISpotTypeRepository spotTypeRepository,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            IPlatformMapService platformMapService,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _spotRepository = spotRepository;
            _locationService = locationService;
            _spotTypeRepository = spotTypeRepository;
            _configuration = configuration;
            _platformMapService = platformMapService;

            Spots = new ObservableCollection<Models.Domain.Spot>();
            Pins = new ObservableCollection<Pin>();
            SpotTypes = new ObservableCollection<SpotType>();

            // Valeurs par défaut, seront remplacées par la géolocalisation si disponible
            double defaultLat = _configuration.GetValue<double>("AppSettings:DefaultLatitude", 43.2965);
            double defaultLong = _configuration.GetValue<double>("AppSettings:DefaultLongitude", 5.3698);
            double defaultZoom = _configuration.GetValue<double>("AppSettings:DefaultZoomLevel", 12);

            MapLatitude = defaultLat;
            MapLongitude = defaultLong;
            MapZoomLevel = defaultZoom;
            
            System.Diagnostics.Debug.WriteLine($"[INFO] MapViewModel initialized with default coordinates: {MapLatitude}, {MapLongitude}, zoom: {MapZoomLevel}");

            Title = "Carte";
            
            // Initialize empty and network error states
            UpdateEmptyState();
            CheckNetworkConnectivity();
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] MapViewModel InitializeAsync started");
                
                // Initialize platform-specific map configuration
                System.Diagnostics.Debug.WriteLine("[DEBUG] Initializing platform map service");
                var mapInitialized = await _platformMapService.InitializePlatformMapAsync();
                if (!mapInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] Platform map initialization failed");
                    await DialogService.ShowAlertAsync("Erreur", "Impossible d'initialiser les cartes pour cette plateforme", "OK");
                }
                
                // Validate map configuration
                System.Diagnostics.Debug.WriteLine("[DEBUG] Validating map configuration");
                var configValid = await _platformMapService.ValidateMapConfigurationAsync();
                if (!configValid)
                {
                    System.Diagnostics.Debug.WriteLine("[WARNING] Map configuration validation failed");
                }
                
                // Récupération des types de spots pour les filtres
                System.Diagnostics.Debug.WriteLine("[DEBUG] Loading spot types");
                await LoadSpotTypesCommand.ExecuteAsync(null);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Loaded {SpotTypes?.Count ?? 0} spot types");

                // Tentative de géolocalisation
                System.Diagnostics.Debug.WriteLine("[DEBUG] Refreshing location");
                await RefreshLocationCommand.ExecuteAsync(null);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Location available: {IsLocationAvailable}");

                // Chargement des spots
                System.Diagnostics.Debug.WriteLine("[DEBUG] Loading spots");
                await LoadSpotsCommand.ExecuteAsync(null);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] InitializeAsync completed. Final counts - Spots: {Spots?.Count ?? 0}, Pins: {Pins?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] InitializeAsync failed: {ex.Message}");
                await DialogService.ShowAlertAsync("Erreur", $"Une erreur est survenue lors de l'initialisation : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task LoadSpots()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadSpots started. IsLocationAvailable: {IsLocationAvailable}");

                IEnumerable<Models.Domain.Spot> spots;

                // Si la géolocalisation est disponible, récupérer les spots à proximité
                if (IsLocationAvailable)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Loading nearby spots for location: {UserLatitude}, {UserLongitude}");
                    spots = await _spotRepository.GetNearbySpots(
                        (decimal)UserLatitude,
                        (decimal)UserLongitude,
                        DEFAULT_SEARCH_RADIUS_KM,
                        MAX_SPOTS_LIMIT);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Loading all approved spots (no location available)");
                    // Sinon, récupérer tous les spots validés
                    spots = await _spotRepository.GetSpotsByValidationStatusAsync(SpotValidationStatus.Approved);
                }

                var spotsCount = spots?.Count() ?? 0;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Retrieved {spotsCount} spots from repository");

                if (spotsCount == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[WARNING] No spots found in repository");
                    await DialogService.ShowToastAsync("Aucun spot trouvé dans la région");
                }
                else
                {
                    foreach (var spot in spots.Take(3)) // Log first 3 spots for debugging
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Sample spot: {spot.Name} at {spot.Latitude}, {spot.Longitude}");
                    }
                }

                RefreshSpotsList(spots);
                UpdatePins();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpots failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les spots. Veuillez réessayer plus tard.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoadSpotTypes()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var types = await _spotTypeRepository.GetActiveTypesAsync();

                RefreshSpotTypesList(types);
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les types de spots.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RefreshLocation()
        {
            try
            {
                // Vérifier si nous avons déjà les permissions
                bool hasPermission = await _locationService.RequestLocationPermissionAsync();

                if (!hasPermission)
                {
                    IsLocationAvailable = false;
                    await DialogService.ShowAlertAsync("Permissions", "L'accès à la localisation est nécessaire pour utiliser cette fonctionnalité.", "OK");
                    return;
                }

                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    // Conversion de decimal à double pour l'affichage
                    UserLatitude = Convert.ToDouble(location.Latitude);
                    UserLongitude = Convert.ToDouble(location.Longitude);

                    // Centrer la carte sur la position de l'utilisateur
                    MapLatitude = UserLatitude;
                    MapLongitude = UserLongitude;

                    IsLocationAvailable = true;

                    // Recharger les spots à proximité
                    await LoadSpotsCommand.ExecuteAsync(null);
                    
                    // Notify that map position has changed
                    OnPropertyChanged(nameof(MapLatitude));
                    OnPropertyChanged(nameof(MapLongitude));
                    OnPropertyChanged(nameof(MapZoomLevel));
                }
                else
                {
                    IsLocationAvailable = false;
                    await DialogService.ShowAlertAsync("Localisation", "Impossible d'obtenir votre position. Vérifiez que les services de localisation sont activés.", "OK");
                }
            }
            catch (Exception ex)
            {
                IsLocationAvailable = false;
                await DialogService.ShowAlertAsync("Localisation", "La géolocalisation n'est pas disponible.", "OK");
            }
        }

        [RelayCommand]
        private async Task FilterSpots(string filterType)
        {
            if (string.IsNullOrEmpty(filterType) || IsBusy)
                return;

            try
            {
                IsBusy = true;
                IsFiltering = true;

                // Convertir le filterType en typeId
                int typeId;
                switch (filterType.ToLower())
                {
                    case "diving":
                        typeId = 1; // Plongée
                        break;
                    case "freediving":
                        typeId = 2; // Apnée
                        break;
                    case "snorkeling":
                        typeId = 3; // Randonnée
                        break;
                    default:
                        typeId = 0;
                        break;
                }

                if (typeId > 0)
                {
                    var filteredSpots = await _spotRepository.GetSpotsByTypeAsync(typeId).ConfigureAwait(false);

                    RefreshSpotsList(filteredSpots);
                    UpdatePins();
                }
                else
                {
                    // Si pas de filtre valide, charger tous les spots
                    await LoadSpotsCommand.ExecuteAsync(null).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de filtrer les spots. Veuillez réessayer plus tard.", "OK");
            }
            finally
            {
                IsBusy = false;
                IsFiltering = false;
            }
        }

        [RelayCommand]
        private async Task FilterSpotsByType(SpotType spotType)
        {
            if (spotType == null || IsBusy)
                return;

            try
            {
                IsBusy = true;
                IsFiltering = true;
                SelectedSpotType = spotType;

                var filteredSpots = await _spotRepository.GetSpotsByTypeAsync(spotType.Id).ConfigureAwait(false);

                RefreshSpotsList(filteredSpots);
                UpdatePins();

                // Optionnel : zoomer sur les résultats si peu nombreux
                if (Spots.Count >= MIN_SPOTS_FOR_AUTO_ZOOM && Spots.Count <= MAX_SPOTS_FOR_AUTO_ZOOM)
                {
                    CenterMapOnSpots(Spots);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de filtrer les spots. Veuillez réessayer plus tard.", "OK");
            }
            finally
            {
                IsBusy = false;
                IsFiltering = false;
            }
        }

        [RelayCommand]
        private async Task SpotSelected(Models.Domain.Spot spot)
        {
            if (spot == null) return;

            // Pour naviguer vers les détails du spot
            await NavigationService.NavigateToAsync<ViewModels.Spot.SpotDetailsViewModel>(spot.Id).ConfigureAwait(false);
        }

        [RelayCommand]
        private async Task NavigateToAddSpot()
        {
            // Pour naviguer vers l'ajout d'un spot
            await NavigationService.NavigateToAsync<ViewModels.Spot.AddSpotViewModel>().ConfigureAwait(false);
        }

        [RelayCommand]
        private async Task SearchTextChanged()
        {
            // Cancel previous search
            _searchCancellationToken?.Cancel();
            _searchCancellationToken = new System.Threading.CancellationTokenSource();
            
            // Debounce search - wait 500ms after user stops typing
            try
            {
                await Task.Delay(500, _searchCancellationToken.Token);
                if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
                {
                    await SearchSpots();
                }
                else if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadSpots();
                }
            }
            catch (TaskCanceledException)
            {
                // Search was cancelled - this is expected
            }
        }

        [RelayCommand]
        private async Task SearchSpots()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || IsBusy)
                return;

            try
            {
                IsBusy = true;
                IsSearching = true;

                var searchResults = await _spotRepository.SearchSpotsAsync(SearchText).ConfigureAwait(false);

                RefreshSpotsList(searchResults);
                UpdatePins();

                // Zoom sur les résultats de recherche
                if (Spots.Count > 0)
                {
                    CenterMapOnSpots(Spots);
                }
                else
                {
                    await DialogService.ShowToastAsync("Aucun spot trouvé pour cette recherche").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'effectuer la recherche. Veuillez réessayer plus tard.", "OK").ConfigureAwait(false);
            }
            finally
            {
                IsBusy = false;
                IsSearching = false;
            }
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SelectedSpotType = null;
            SearchText = string.Empty;
            IsFiltering = false;
            IsSearching = false;

            // Recharger tous les spots
            LoadSpotsCommand.Execute(null);
        }

        [RelayCommand]
        private async Task Initialize()
        {
            await InitializeAsync();
        }

        [RelayCommand]
        private void PinSelected(Pin pin)
        {
            if (pin?.BindingContext is Models.Domain.Spot spot)
            {
                SpotSelected(spot);
            }
        }

        [RelayCommand]
        private void MapClicked(Microsoft.Maui.Controls.Maps.MapClickedEventArgs args)
        {
            // Gérer les clics sur la carte
            // Par exemple, vous pourriez permettre l'ajout d'un nouveau spot à cet endroit
        }

        [RelayCommand]
        private void VisibleRegionChanged(MapSpan mapSpan)
        {
            VisibleRegion = mapSpan;

            // Vous pourriez déclencher un chargement de spots dans la région visible
            // si l'utilisateur a déplacé la carte d'une distance significative
        }

        public void ForceMapRefresh()
        {
            // Force UI to refresh map position
            OnPropertyChanged(nameof(MapLatitude));
            OnPropertyChanged(nameof(MapLongitude));
            OnPropertyChanged(nameof(MapZoomLevel));
            OnPropertyChanged(nameof(Pins));
            
            System.Diagnostics.Debug.WriteLine($"[INFO] ForceMapRefresh called: {MapLatitude}, {MapLongitude}, zoom: {MapZoomLevel}, pins: {Pins?.Count}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Map coordinates valid: Lat={MapLatitude >= -90 && MapLatitude <= 90}, Lng={MapLongitude >= -180 && MapLongitude <= 180}");
            
            // Additional map debugging
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Current map state - Location available: {IsLocationAvailable}, User position: {UserLatitude}, {UserLongitude}");
        }
        
        public void InitializeMapPosition()
        {
            // Ensure we have valid coordinates
            if (MapLatitude == 0 && MapLongitude == 0)
            {
                // Use default coordinates from configuration
                double defaultLat = _configuration.GetValue<double>("AppSettings:DefaultLatitude", 43.2965);
                double defaultLong = _configuration.GetValue<double>("AppSettings:DefaultLongitude", 5.3698);
                double defaultZoom = _configuration.GetValue<double>("AppSettings:DefaultZoomLevel", 12);
                
                MapLatitude = defaultLat;
                MapLongitude = defaultLong;
                MapZoomLevel = defaultZoom;
                
                System.Diagnostics.Debug.WriteLine($"[INFO] Map initialized with default coordinates: {MapLatitude}, {MapLongitude}, zoom: {MapZoomLevel}");
            }
            
            ForceMapRefresh();
        }

        private void UpdatePins()
        {
            Application.Current?.Dispatcher.Dispatch(() => {
                try
                {
                    var validPins = Spots
                        .Select(CreatePinFromSpot)
                        .Where(pin => pin != null)
                        .ToList();

                    // Efficient batch update
                    Pins.Clear();
                    foreach (var pin in validPins)
                    {
                        Pins.Add(pin!);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] UpdatePins failed: {ex.Message}");
                }
            });
        }

        private Pin CreatePinFromSpot(Models.Domain.Spot spot)
        {
            try
            {
                double lat = Convert.ToDouble(spot.Latitude);
                double lon = Convert.ToDouble(spot.Longitude);

                // Add this debug line
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Creating pin for {spot.Name} at {lat}, {lon}");

                // Validate coordinates
                if (double.IsNaN(lat) || double.IsInfinity(lat) || lat < -90 || lat > 90 ||
                    double.IsNaN(lon) || double.IsInfinity(lon) || lon < -180 || lon > 180)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Invalid coordinates for spot {spot.Name}: Lat={spot.Latitude}, Lng={spot.Longitude}");
                    return null; // Return null for invalid coordinates
                }

                var pin = new Pin
                {
                    Label = spot.Name,
                    Address = $"{spot.Type?.Name ?? "Spot"} - {spot.DifficultyLevel}",
                    Location = new Location(lat, lon),
                    Type = PinType.Place,
                    BindingContext = spot
                };

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Successfully created pin for {spot.Name}");
                return pin;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to create pin for spot {spot.Name}: {ex.Message}");
                return null;
            }
        }

        private void CenterMapOnSpots(IEnumerable<Models.Domain.Spot> spots)
        {
            if (!spots.Any()) return;

            // Calculer le centre du groupe de spots
            double minLat = Convert.ToDouble(spots.Min(s => s.Latitude));
            double maxLat = Convert.ToDouble(spots.Max(s => s.Latitude));
            double minLon = Convert.ToDouble(spots.Min(s => s.Longitude));
            double maxLon = Convert.ToDouble(spots.Max(s => s.Longitude));

            double centerLat = (minLat + maxLat) / 2;
            double centerLon = (minLon + maxLon) / 2;

            // Calculer un niveau de zoom approprié
            double latSpan = maxLat - minLat;
            double lonSpan = maxLon - minLon;

            // Appliquer les valeurs
            MapLatitude = centerLat;
            MapLongitude = centerLon;

            // Le zoom devrait être défini en fonction de l'étendue
            // Plus la valeur est grande, plus on est zoomé
            double maxSpan = Math.Max(latSpan, lonSpan);
            if (maxSpan > 0)
            {
                // Cette formule est approximative et dépend de l'API de carte utilisée
                MapZoomLevel = Math.Max(MIN_ZOOM_LEVEL, Math.Min(MAX_ZOOM_LEVEL, Math.Log(180 / maxSpan) / Math.Log(2)));
            }
            
            // Notify that map position has changed
            OnPropertyChanged(nameof(MapLatitude));
            OnPropertyChanged(nameof(MapLongitude));
            OnPropertyChanged(nameof(MapZoomLevel));
        }

        private void RefreshSpotsList(IEnumerable<Models.Domain.Spot> spots)
        {
            Application.Current?.Dispatcher.Dispatch(() => {
                try
                {
                    var spotsList = spots.ToList();
                    Spots.Clear();
                    
                    // Efficient batch processing
                    for (int i = 0; i < spotsList.Count; i += SPOTS_BATCH_SIZE)
                    {
                        var batch = spotsList.Skip(i).Take(SPOTS_BATCH_SIZE);
                        foreach (var spot in batch)
                        {
                            Spots.Add(spot);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] RefreshSpotsList failed: {ex.Message}");
                }
            });
        }

        private void RefreshSpotTypesList(IEnumerable<SpotType> types)
        {
            // Use batch update for better performance
            Application.Current?.Dispatcher.Dispatch(() => {
                SpotTypes.Clear();
                foreach (var type in types)
                {
                    SpotTypes.Add(type);
                }
            });
        }
        
        private void UpdateEmptyState()
        {
            Application.Current?.Dispatcher.Dispatch(() => {
                IsEmptyState = !IsBusy && (Spots?.Count ?? 0) == 0 && !IsNetworkError;
            });
        }
        
        private void CheckNetworkConnectivity()
        {
            try
            {
                var connectivity = Connectivity.Current;
                IsNetworkError = connectivity.NetworkAccess != NetworkAccess.Internet;
                
                // Subscribe to connectivity changes
                connectivity.ConnectivityChanged += OnConnectivityChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to check network connectivity: {ex.Message}");
                IsNetworkError = false;
            }
        }
        
        private void OnConnectivityChanged(object sender, Microsoft.Maui.Networking.ConnectivityChangedEventArgs e)
        {
            Application.Current?.Dispatcher.Dispatch(() => {
                var wasNetworkError = IsNetworkError;
                IsNetworkError = e.NetworkAccess != NetworkAccess.Internet;
                
                // If we just regained connectivity, reload spots
                if (wasNetworkError && !IsNetworkError)
                {
                    LoadSpotsCommand.Execute(null);
                }
                
                UpdateEmptyState();
            });
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _searchCancellationToken?.Cancel();
                _searchCancellationToken?.Dispose();
                
                // Unsubscribe from connectivity events
                try
                {
                    Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[WARNING] Failed to unsubscribe from connectivity events: {ex.Message}");
                }
            }
        }

    }
}