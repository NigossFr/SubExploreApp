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
using SubExplore.Services.Implementations;
using SubExplore.ViewModels.Base;
using SubExplore.ViewModels.Profile;
using SubExplore.ViewModels.Spots;
using SubExplore.Models.Menu;
using MenuItemModel = SubExplore.Models.Menu.MenuItem;

namespace SubExplore.ViewModels.Map
{
    public partial class MapViewModel : ViewModelBase
    {
        private readonly ISpotRepository _spotRepository;
        private readonly ILocationService _locationService;
        private readonly ISpotTypeRepository _spotTypeRepository;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IPlatformMapService _platformMapService;
        private readonly IApplicationPerformanceService _performanceService;

        // Map Configuration Constants
        private const double DEFAULT_SEARCH_RADIUS_KM = 10.0;
        private const int MAX_SPOTS_LIMIT = 100;
        private const int MIN_SPOTS_FOR_AUTO_ZOOM = 1;
        private const int MAX_SPOTS_FOR_AUTO_ZOOM = 5;
        private const double MIN_ZOOM_LEVEL = 1.0;
        private const double MAX_ZOOM_LEVEL = 18.0;
        private const int SPOTS_BATCH_SIZE = 20;
        private const int MAP_UPDATE_DELAY_MS = 500;
        private const int CACHE_EXPIRY_MINUTES = 5;

        // Performance: Cache frequently accessed data
        private DateTime _lastSpotTypesLoad = DateTime.MinValue;
        private DateTime _lastSpotsLoad = DateTime.MinValue;

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

        // Initialization flag to prevent multiple initializations
        private bool _isInitialized = false;
        private bool _isInitializing = false;

        // Menu-related properties
        [ObservableProperty]
        private bool _isMenuOpen;

        [ObservableProperty]
        private ObservableCollection<MenuSection> _menuSections;

        [ObservableProperty]
        private string _userDisplayName;

        [ObservableProperty]
        private string _userEmail;

        [ObservableProperty]
        private string _userAvatarUrl;

        // Spot mini window properties
        [ObservableProperty]
        private bool _isSpotMiniWindowVisible;

        [ObservableProperty]
        private Models.Domain.Spot _selectedSpot;

        private readonly IDatabaseService _databaseService;
        private readonly IUserRepository _userRepository;
        private readonly ISettingsService _settingsService;
        private readonly IAuthenticationService _authenticationService;

        public MapViewModel(
            ISpotRepository spotRepository,
            ILocationService locationService,
            ISpotTypeRepository spotTypeRepository,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            IPlatformMapService platformMapService,
            IDatabaseService databaseService,
            IUserRepository userRepository,
            IDialogService dialogService,
            INavigationService navigationService,
            ISettingsService settingsService,
            IAuthenticationService authenticationService,
            IApplicationPerformanceService performanceService)
            : base(dialogService, navigationService)
        {
            _spotRepository = spotRepository;
            _locationService = locationService;
            _spotTypeRepository = spotTypeRepository;
            _databaseService = databaseService;
            _userRepository = userRepository;
            _settingsService = settingsService;
            _authenticationService = authenticationService;
            _performanceService = performanceService;
            _configuration = configuration;
            _platformMapService = platformMapService;

            Spots = new ObservableCollection<Models.Domain.Spot>();
            Pins = new ObservableCollection<Pin>();
            SpotTypes = new ObservableCollection<SpotType>();
            MenuSections = new ObservableCollection<MenuSection>();

            // Valeurs par défaut, seront remplacées par la géolocalisation si disponible
            double defaultLat = _configuration.GetValue<double>("AppSettings:DefaultLatitude", 43.2965);
            double defaultLong = _configuration.GetValue<double>("AppSettings:DefaultLongitude", 5.3698);
            double defaultZoom = _configuration.GetValue<double>("AppSettings:DefaultZoomLevel", 12);

            MapLatitude = defaultLat;
            MapLongitude = defaultLong;
            MapZoomLevel = defaultZoom;
            
            System.Diagnostics.Debug.WriteLine($"[INFO] MapViewModel initialized with default coordinates: {MapLatitude}, {MapLongitude}, zoom: {MapZoomLevel}");

            Title = "Carte";
            
            // Initialize menu
            InitializeMenu();
            
            // Initialize empty and network error states
            UpdateEmptyState();
            CheckNetworkConnectivity();
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            try
            {
                // Prevent multiple simultaneous initializations
                if (_isInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("[INFO] MapViewModel already initialized, skipping");
                    return;
                }

                if (_isInitializing)
                {
                    System.Diagnostics.Debug.WriteLine("[INFO] MapViewModel initialization in progress, skipping duplicate call");
                    return;
                }

                _isInitializing = true;
                IsBusy = true;
                System.Diagnostics.Debug.WriteLine("[DEBUG] MapViewModel InitializeAsync started with enhanced error handling");
                
                try
                {
                    // Step 1: Initialize platform-specific map configuration
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Initializing platform map service");
                    var mapInitialized = await _platformMapService.InitializePlatformMapAsync();
                    if (!mapInitialized)
                    {
                        System.Diagnostics.Debug.WriteLine("[ERROR] Platform map initialization failed");
                        await DialogService.ShowAlertAsync("Erreur", "Impossible d'initialiser les cartes pour cette plateforme", "OK");
                        return;
                    }

                    // Step 2: Load spot types FIRST (required for filters to work)
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Loading spot types (required for filters)");
                    await LoadSpotTypesOptimized();
                    
                    if (SpotTypes?.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("[WARNING] No spot types loaded - filters will not work");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[SUCCESS] Loaded {SpotTypes.Count} spot types for filtering");
                    }

                    // Step 3: Load spots data
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Loading spots data");
                    await LoadSpotsOptimized();
                    
                    if (Spots?.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("[WARNING] No spots loaded - map will be empty");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[SUCCESS] Loaded {Spots.Count} spots");
                    }

                    // Step 4: Update pins on UI thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] Updating pins on UI thread");
                        UpdatePins();
                        System.Diagnostics.Debug.WriteLine($"[SUCCESS] Created {Pins?.Count ?? 0} pins for map");
                        
                        _isInitialized = true;
                        _isInitializing = false;
                        IsBusy = false;
                        
                        // Initialize menu and other UI elements  
                        InitializeMapPosition();
                        
                        System.Diagnostics.Debug.WriteLine("[SUCCESS] MapViewModel initialization completed successfully");
                    });
                }
                catch (Exception innerEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] MapViewModel initialization failed: {innerEx.Message}");
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        _isInitialized = false;
                        _isInitializing = false;
                        IsBusy = false;
                        await DialogService.ShowAlertAsync("Erreur", $"Erreur d'initialisation: {innerEx.Message}", "OK");
                    });
                }
            }
            catch (Exception ex)
            {
                _isInitializing = false;
                IsBusy = false;
                System.Diagnostics.Debug.WriteLine($"[ERROR] InitializeAsync failed: {ex.Message}");
                await DialogService.ShowAlertAsync("Erreur", $"Une erreur s'est produite lors de l'initialisation : {ex.Message}", "OK");
            }
        }

        private async Task LoadDataWithUIYields()
        {
            try
            {
                // Exécuter toutes les opérations de chargement en parallèle pour améliorer les performances
                System.Diagnostics.Debug.WriteLine("[DEBUG] Starting parallel data loading for better performance");
                
                var spotTypesTask = Task.Run(async () =>
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Loading spot types in background");
                    await LoadSpotTypesOptimized();
                });
                
                var userTask = Task.Run(async () =>
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Loading user for menu in background");
                    await LoadCurrentUser();
                });
                
                var locationTask = Task.Run(async () =>
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Checking location service availability in background");
                    var isAvailable = await _locationService.IsLocationServiceEnabledAsync();
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        IsLocationAvailable = isAvailable;
                    });
                });
                
                // Attendre que toutes les tâches parallèles se terminent
                await Task.WhenAll(spotTypesTask, userTask, locationTask);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Parallel loading completed - SpotTypes: {SpotTypes?.Count ?? 0}, Location available: {IsLocationAvailable}");
                
                // Yield minimal to UI thread
                await Task.Delay(1);
                
                System.Diagnostics.Debug.WriteLine("[DEBUG] Data loading with yields completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadDataWithUIYields failed: {ex.Message}");
                throw;
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

                // TEMPORAIRE : Force le chargement de tous les spots approuvés pour diagnostic
                System.Diagnostics.Debug.WriteLine("[DEBUG] FORCE: Loading all approved spots for debugging");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Repository instance: {_spotRepository?.GetType().Name ?? "null"}");
                
                spots = await _spotRepository.GetSpotsByValidationStatusAsync(SpotValidationStatus.Approved);
                
                // Log de diagnostic supplémentaire
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Query completed. Raw result count: {spots?.Count() ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] IsLocationAvailable: {IsLocationAvailable}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] UserLatitude: {UserLatitude}, UserLongitude: {UserLongitude}");
                
                // Si on obtient des spots, vérifions leur contenu
                if (spots != null && spots.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✓ Found {spots.Count()} spots from repository");
                    System.Diagnostics.Debug.WriteLine("[DEBUG] First 5 spots details:");
                    foreach (var spot in spots.Take(5))
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG]   - {spot.Name} (ID:{spot.Id})");
                        System.Diagnostics.Debug.WriteLine($"[DEBUG]     Status: {spot.ValidationStatus}, TypeId: {spot.TypeId}");
                        System.Diagnostics.Debug.WriteLine($"[DEBUG]     Position: {spot.Latitude}, {spot.Longitude}");
                        System.Diagnostics.Debug.WriteLine($"[DEBUG]     Type: {spot.Type?.Name ?? "null"}, Creator: {spot.Creator?.Id.ToString() ?? "null"}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[WARNING] ✗ No spots returned from repository");
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Checking database connection and data...");
                    
                    try
                    {
                        // Test database connectivity
                        await _databaseService.TestConnectionAsync();
                        System.Diagnostics.Debug.WriteLine("[DEBUG] ✓ Database connection test passed");
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] ✗ Database connection failed: {dbEx.Message}");
                    }
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

                System.Diagnostics.Debug.WriteLine($"[DEBUG] About to refresh spots list with {spotsCount} spots");
                RefreshSpotsList(spots);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Spots list refreshed, now contains {Spots?.Count ?? 0} spots");
                
                // Allow UI to update between operations
                await Task.Delay(50);
                
                System.Diagnostics.Debug.WriteLine("[DEBUG] About to update pins");
                UpdatePins();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Pins updated, now contains {Pins?.Count ?? 0} pins");
                
                // Allow UI to refresh after pins are updated
                await Task.Delay(50);
                
                UpdateEmptyState();
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
                UpdateEmptyState();
            }
        }

        [RelayCommand]
        private async Task LoadSpotTypes()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                // Le repository ne retourne maintenant que les 5 types autorisés
                var types = await _spotTypeRepository.GetActiveTypesAsync();

                RefreshSpotTypesList(types);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotTypes failed: {ex.Message}");
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
                // Check current permission status first
                var currentStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                
                if (currentStatus == PermissionStatus.Denied)
                {
                    // If permission was previously denied, inform user and suggest settings
                    IsLocationAvailable = false;
                    await DialogService.ShowAlertAsync("Permissions", 
                        "L'accès à la localisation a été refusé. Vous pouvez l'activer dans les paramètres de l'application.", 
                        "OK");
                    return;
                }
                
                // Request permission if not already granted
                bool hasPermission = await _locationService.RequestLocationPermissionAsync();

                if (!hasPermission)
                {
                    IsLocationAvailable = false;
                    await DialogService.ShowAlertAsync("Permissions", 
                        "L'accès à la localisation est nécessaire pour cette fonctionnalité. Vous pouvez l'activer dans les paramètres.", 
                        "OK");
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
                    await DialogService.ShowAlertAsync("Localisation", 
                        "Impossible d'obtenir votre position. Vérifiez que les services de localisation sont activés.", 
                        "OK");
                }
            }
            catch (Exception ex)
            {
                IsLocationAvailable = false;
                await DialogService.ShowAlertAsync("Localisation", 
                    "La géolocalisation n'est pas disponible.", 
                    "OK");
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
                    var filteredSpots = await _spotRepository.GetSpotsByTypeAsync(typeId); // ✅ FIXED: Removed ConfigureAwait(false) for ViewModel consistency

                    RefreshSpotsList(filteredSpots);
                    UpdatePins();
                }
                else
                {
                    // Si pas de filtre valide, charger tous les spots
                    await LoadSpotsCommand.ExecuteAsync(null); // ✅ FIXED: Removed ConfigureAwait(false) for ViewModel consistency
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
            try
            {
                // Ensure we're on the UI thread
                if (Application.Current?.Dispatcher?.IsDispatchRequired == true)
                {
                    await Application.Current.Dispatcher.DispatchAsync(() => FilterSpotsByTypeCore(spotType));
                }
                else
                {
                    FilterSpotsByTypeCore(spotType);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] FilterSpotsByType failed: {ex.Message}");
            }
        }

        private void FilterSpotsByTypeCore(SpotType spotType)
        {
            SelectedSpotType = spotType;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Filtering spots by type: {spotType?.Name ?? "All"}");
            
            // Apply filter and update pins based on current spots in memory
            ApplySpotTypeFilterCore();
        }


        [RelayCommand]
        private async Task SpotSelected(Models.Domain.Spot spot)
        {
            if (spot == null) return;

            // Show mini window instead of direct navigation
            ShowSpotMiniWindow(spot);
        }

        [RelayCommand]
        private void ShowSpotMiniWindow(Models.Domain.Spot spot)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ShowSpotMiniWindow CALLED with spot: {spot?.Name ?? "null"}");
            
            if (spot == null) 
            {
                System.Diagnostics.Debug.WriteLine("[ERROR] ShowSpotMiniWindow called with null spot");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Before setting properties - IsSpotMiniWindowVisible: {IsSpotMiniWindowVisible}");
            
            SelectedSpot = spot;
            IsSpotMiniWindowVisible = true;
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ✓ Properties set - IsSpotMiniWindowVisible: {IsSpotMiniWindowVisible}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ✓ SelectedSpot: {SelectedSpot?.Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ✓ SelectedSpot.Type: {SelectedSpot?.Type?.Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ✓ SelectedSpot.DifficultyLevel: {SelectedSpot?.DifficultyLevel}");
            
            // Force property change notifications
            OnPropertyChanged(nameof(IsSpotMiniWindowVisible));
            OnPropertyChanged(nameof(SelectedSpot));
            
            System.Diagnostics.Debug.WriteLine("[DEBUG] ✓ Property change notifications sent");
        }

        [RelayCommand]
        private void CloseSpotMiniWindow()
        {
            IsSpotMiniWindowVisible = false;
            SelectedSpot = null;
            
            System.Diagnostics.Debug.WriteLine("[DEBUG] Closed spot mini window");
        }

        [RelayCommand]
        private async Task ViewSpotDetails()
        {
            if (SelectedSpot == null) 
            {
                System.Diagnostics.Debug.WriteLine("[ERROR] ViewSpotDetails: SelectedSpot is null");
                return;
            }

            try
            {
                // Capture spot data BEFORE closing mini window (which sets SelectedSpot to null)
                var spotId = SelectedSpot.Id;
                var spotName = SelectedSpot.Name;
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ViewSpotDetails: Starting navigation to details for spot {spotName} (ID: {spotId})");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ViewSpotDetails: Current MainPage type: {Application.Current?.MainPage?.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ViewSpotDetails: NavigationService null check: {NavigationService == null}");
                
                // Close mini window after capturing data
                CloseSpotMiniWindow();
                System.Diagnostics.Debug.WriteLine("[DEBUG] ViewSpotDetails: Mini window closed");
                
                // Check if NavigationService is available
                if (NavigationService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] NavigationService is null - cannot navigate");
                    await DialogService.ShowAlertAsync("Erreur", "Service de navigation non disponible", "OK");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ViewSpotDetails: About to call NavigateToAsync with SpotId: {spotId}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ViewSpotDetails: Spot name: {spotName}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ViewSpotDetails: Using captured data (SelectedSpot is now null after mini window closure)");
                
                // Final safety check before navigation
                if (NavigationService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] ViewSpotDetails: NavigationService became null before navigation call");
                    await DialogService.ShowAlertAsync("Erreur", "Service de navigation non disponible", "OK");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ViewSpotDetails: Final pre-navigation check - SpotId: {spotId}, NavigationService: {NavigationService.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ViewSpotDetails: About to navigate to {typeof(ViewModels.Spots.SpotDetailsViewModel).FullName}");
                
                // Navigate to full details with isolated try-catch
                try
                {
                    await NavigationService.NavigateToAsync<ViewModels.Spots.SpotDetailsViewModel>(spotId);
                    System.Diagnostics.Debug.WriteLine("[DEBUG] ViewSpotDetails: Navigation call completed successfully");
                }
                catch (Exception navEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] ViewSpotDetails: Navigation failed with exception: {navEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Navigation stack trace: {navEx.StackTrace}");
                    if (navEx.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Navigation inner exception: {navEx.InnerException.Message}");
                    }
                    
                    // Re-throw to be caught by outer catch block
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ViewSpotDetails failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
                }
                await DialogService.ShowAlertAsync("Erreur", $"Impossible d'ouvrir les détails du spot: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private void TestMiniWindow()
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] TestMiniWindow command executed");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Current binding context exists: {this != null}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Current IsSpotMiniWindowVisible before: {IsSpotMiniWindowVisible}");
            
            // Create a test spot to verify mini window functionality
            var testSpot = new Models.Domain.Spot
            {
                Id = 999,
                Name = "DEBUG TEST SPOT",
                DifficultyLevel = DifficultyLevel.Beginner,
                Latitude = 43.2965m,
                Longitude = 5.3698m,
                Type = new SpotType
                {
                    Id = 1,
                    Name = "Test Plongée",
                    ColorCode = "#FF0000"
                }
            };
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] About to call ShowSpotMiniWindow with test spot: {testSpot.Name}");
            ShowSpotMiniWindow(testSpot);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] After ShowSpotMiniWindow - IsSpotMiniWindowVisible: {IsSpotMiniWindowVisible}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] After ShowSpotMiniWindow - SelectedSpot: {SelectedSpot?.Name}");
            System.Diagnostics.Debug.WriteLine("[DEBUG] TestMiniWindow: Test spot mini window should now be visible with RED background");
        }


        [RelayCommand]
        private async Task NavigateToAddSpot()
        {
            try
            {
                // Create location parameter object with current user location if available
                object locationParameter = null;
                
                if (IsLocationAvailable)
                {
                    locationParameter = new
                    {
                        Latitude = (decimal)UserLatitude,
                        Longitude = (decimal)UserLongitude,
                        LocationParameter = $"Current Location ({UserLatitude:F6}, {UserLongitude:F6})"
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAddSpot with location: {UserLatitude}, {UserLongitude}");
                }
                else
                {
                    // Use map center as fallback
                    locationParameter = new
                    {
                        Latitude = (decimal)MapLatitude,
                        Longitude = (decimal)MapLongitude,
                        LocationParameter = $"Map Center ({MapLatitude:F6}, {MapLongitude:F6})"
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAddSpot with map center: {MapLatitude}, {MapLongitude}");
                }

                // Navigate to AddSpot with location parameters
                await NavigationService.NavigateToAsync<ViewModels.Spots.AddSpotViewModel>(locationParameter); // ✅ FIXED: Removed ConfigureAwait(false) for UI service
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] NavigateToAddSpot failed: {ex.Message}");
                await DialogService.ShowAlertAsync("Erreur", "Impossible de naviguer vers l'ajout de spot. Veuillez réessayer.", "OK");
            }
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
                await Task.Delay(500, _searchCancellationToken.Token).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
                {
                    await SearchSpots(); // ✅ FIXED: Removed ConfigureAwait(false) for ViewModel consistency
                }
                else if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadSpots(); // ✅ FIXED: Removed ConfigureAwait(false) for ViewModel consistency
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

                var searchResults = await _spotRepository.SearchSpotsAsync(SearchText); // ✅ FIXED: Removed ConfigureAwait(false) for ViewModel consistency

                RefreshSpotsList(searchResults);
                UpdatePins();
                UpdateEmptyState();

                // Zoom sur les résultats de recherche
                if (Spots.Count > 0)
                {
                    CenterMapOnSpots(Spots);
                }
                else
                {
                    await DialogService.ShowToastAsync("Aucun spot trouvé pour cette recherche"); // ✅ FIXED: Removed ConfigureAwait(false) for UI service
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'effectuer la recherche. Veuillez réessayer plus tard.", "OK"); // ✅ FIXED: Removed ConfigureAwait(false) for UI service
            }
            finally
            {
                IsBusy = false;
                IsSearching = false;
                UpdateEmptyState();
            }
        }

        [RelayCommand]
        private async Task ClearFilters()
        {
            try
            {
                SelectedSpotType = null;
                SearchText = string.Empty;
                IsFiltering = false;
                IsSearching = false;
                System.Diagnostics.Debug.WriteLine("[DEBUG] Clearing all filters");

                // Apply filter (null means show all) instead of reloading from database
                await ApplySpotTypeFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ClearFilters failed: {ex.Message}");
            }
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
                ShowSpotMiniWindow(spot);
            }
        }

        [RelayCommand]
        private async Task MapClicked(Microsoft.Maui.Controls.Maps.MapClickedEventArgs args)
        {
            try
            {
                // Handle map clicks for adding spots at specific location
                if (args?.Location != null)
                {
                    var clickedLocation = args.Location;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Map clicked at: {clickedLocation.Latitude}, {clickedLocation.Longitude}");
                    
                    // Show option to add spot at clicked location
                    var result = await DialogService.ShowConfirmationAsync(
                        "Ajouter un spot", 
                        "Voulez-vous ajouter un spot à cet endroit ?", 
                        "Oui", 
                        "Non");
                    
                    if (result)
                    {
                        var locationParameter = new
                        {
                            Latitude = (decimal)clickedLocation.Latitude,
                            Longitude = (decimal)clickedLocation.Longitude,
                            LocationParameter = $"Map Click ({clickedLocation.Latitude:F6}, {clickedLocation.Longitude:F6})"
                        };
                        
                        await NavigationService.NavigateToAsync<ViewModels.Spots.AddSpotViewModel>(locationParameter); // ✅ FIXED: Removed ConfigureAwait(false) for UI service
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] MapClicked failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private void VisibleRegionChanged(MapSpan mapSpan)
        {
            VisibleRegion = mapSpan;

            // Vous pourriez déclencher un chargement de spots dans la région visible
            // si l'utilisateur a déplacé la carte d'une distance significative
        }

        // Menu-related commands
        [RelayCommand]
        private void ToggleMenu()
        {
            IsMenuOpen = !IsMenuOpen;
        }

        [RelayCommand]
        private async Task NavigateToMySpots()
        {
            await NavigationService.NavigateToAsync<MySpotsViewModel>();
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task NavigateToProfile()
        {
            await NavigateToAsync<UserProfileViewModel>();
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task NavigateToFavorites()
        {
            // TODO: Implement Favorites page
            await DialogService.ShowToastAsync("Fonction à venir");
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task NavigateToHistory()
        {
            // TODO: Implement History page
            await DialogService.ShowToastAsync("Fonction à venir");
            IsMenuOpen = false;
        }


        [RelayCommand]
        private async Task NavigateToAbout()
        {
            // TODO: Implement About page
            await DialogService.ShowToastAsync("Fonction à venir");
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task Logout()
        {
            var confirmed = await DialogService.ShowConfirmationAsync(
                "Déconnexion",
                "Êtes-vous sûr de vouloir vous déconnecter ?",
                "Oui",
                "Annuler");

            if (confirmed)
            {
                try
                {
                    await _authenticationService.LogoutAsync();
                    await DialogService.ShowToastAsync("Déconnexion réussie");
                    
                    // Update UI to reflect logout
                    await LoadCurrentUser();
                    
                    IsMenuOpen = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Logout failed: {ex.Message}");
                    await DialogService.ShowAlertAsync("Erreur", "Erreur lors de la déconnexion", "OK");
                }
            }
        }

        // Menu helper methods
        private void InitializeMenu()
        {
            MenuSections.Clear();
            
            // Main Navigation Section
            var mainSection = new MenuSection
            {
                Title = "Navigation",
                Items = new ObservableCollection<MenuItemModel>
                {
                    new MenuItemModel
                    {
                        Title = "Mes Spots",
                        Icon = "📍",
                        Description = "Vos spots créés",
                        Command = NavigateToMySpotsCommand,
                        IsEnabled = true
                    },
                    new MenuItemModel
                    {
                        Title = "Ajouter un Spot",
                        Icon = "➕",
                        Description = "Créer un nouveau spot",
                        Command = NavigateToAddSpotCommand,
                        IsEnabled = true
                    }
                }
            };
            
            // User Section
            var userSection = new MenuSection
            {
                Title = "Utilisateur",
                Items = new ObservableCollection<MenuItemModel>
                {
                    new MenuItemModel
                    {
                        Title = "Profil",
                        Icon = "👤",
                        Description = "Gérer votre profil",
                        Command = NavigateToProfileCommand,
                        IsEnabled = true
                    },
                    new MenuItemModel
                    {
                        Title = "Favoris",
                        Icon = "❤️",
                        Description = "Vos spots favoris",
                        Command = NavigateToFavoritesCommand,
                        IsEnabled = true
                    },
                    new MenuItemModel
                    {
                        Title = "Historique",
                        Icon = "📋",
                        Description = "Vos plongées récentes",
                        Command = NavigateToHistoryCommand,
                        IsEnabled = true
                    }
                }
            };
            
            // Settings Section
            var settingsSection = new MenuSection
            {
                Title = "Paramètres",
                Items = new ObservableCollection<MenuItemModel>
                {
                    new MenuItemModel
                    {
                        Title = "À propos",
                        Icon = "ℹ️",
                        Description = "Informations sur l'app",
                        Command = NavigateToAboutCommand,
                        IsEnabled = true
                    }
                }
            };
            
            MenuSections.Add(mainSection);
            MenuSections.Add(userSection);
            MenuSections.Add(settingsSection);
        }

        private async Task LoadCurrentUser()
        {
            try
            {
                // Use authentication service to get current user
                if (_authenticationService.IsAuthenticated)
                {
                    var currentUser = _authenticationService.CurrentUser;
                    
                    if (currentUser != null)
                    {
                        UserDisplayName = $"{currentUser.FirstName} {currentUser.LastName}";
                        UserEmail = currentUser.Email;
                        UserAvatarUrl = currentUser.AvatarUrl ?? "default_avatar.png";
                        
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Loaded authenticated user: {currentUser.Id}");
                    }
                    else
                    {
                        // Should not happen if IsAuthenticated is true, but handle gracefully
                        await HandleUnauthenticatedUser();
                    }
                }
                else
                {
                    await HandleUnauthenticatedUser();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadCurrentUser failed: {ex.Message}");
                await HandleUnauthenticatedUser();
            }
        }
        
        private async Task HandleUnauthenticatedUser()
        {
            UserDisplayName = "Utilisateur Invité";
            UserEmail = "guest@subexplore.com";
            UserAvatarUrl = "default_avatar.png";
            
            System.Diagnostics.Debug.WriteLine("[DEBUG] User not authenticated - showing guest info");
            
            // Optional: Show login prompt or redirect to login
            // For now, just log the state
        }

        public void ForceMapRefresh()
        {
            // Force reset loading state to ensure UI updates properly
            IsBusy = false;
            IsFiltering = false;
            IsSearching = false;
            
            // Force UI to refresh map position
            OnPropertyChanged(nameof(MapLatitude));
            OnPropertyChanged(nameof(MapLongitude));
            OnPropertyChanged(nameof(MapZoomLevel));
            OnPropertyChanged(nameof(Pins));
            OnPropertyChanged(nameof(IsBusy));
            
            System.Diagnostics.Debug.WriteLine($"[INFO] ForceMapRefresh called: {MapLatitude}, {MapLongitude}, zoom: {MapZoomLevel}, pins: {Pins?.Count}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Forced IsBusy to false: {IsBusy}");
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

        /// <summary>
        /// Thread-safe pins update with atomic collection replacement
        /// </summary>
        public void UpdatePins()
        {
            Application.Current?.Dispatcher.Dispatch(() => {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] UpdatePins called with {Spots?.Count ?? 0} spots");
                    
                    if (Spots == null || !Spots.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] No spots available for pin creation");
                        Pins = new ObservableCollection<Pin>(); // ✅ FIXED: Atomic replacement
                        return;
                    }

                    // Debug first few spots for troubleshooting
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Processing spots for pin creation:");
                    foreach (var spot in Spots.Take(3))
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Spot: {spot.Name} at {spot.Latitude}, {spot.Longitude} - Status: {spot.ValidationStatus}");
                    }

                    var validPins = new List<Pin>();
                    int nullPinCount = 0;

                    foreach (var spot in Spots)
                    {
                        var pin = CreatePinFromSpot(spot);
                        if (pin != null)
                        {
                            validPins.Add(pin);
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] ✓ Created pin for {spot.Name}");
                        }
                        else
                        {
                            nullPinCount++;
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] ✗ Failed to create pin for {spot.Name} - likely invalid coordinates");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Pin creation summary: {validPins.Count} valid, {nullPinCount} failed");

                    // ✅ FIXED: Atomic collection replacement instead of Clear/Add pattern
                    Pins = new ObservableCollection<Pin>(validPins);
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] UpdatePins completed with atomic update. Total pins in collection: {Pins.Count}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] UpdatePins failed: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                    
                    // Ensure we always have a valid collection
                    if (Pins == null)
                    {
                        Pins = new ObservableCollection<Pin>();
                    }
                }
            });
        }

        private Pin CreatePinFromSpot(Models.Domain.Spot spot)
        {
            try
            {
                if (spot == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] Cannot create pin from null spot");
                    return null;
                }

                double lat = Convert.ToDouble(spot.Latitude);
                double lon = Convert.ToDouble(spot.Longitude);

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Creating pin for {spot.Name} at {lat}, {lon} (decimal: {spot.Latitude}, {spot.Longitude})");

                // Validate coordinates with detailed error reporting
                if (double.IsNaN(lat) || double.IsInfinity(lat))
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Invalid latitude for spot {spot.Name}: {lat} (NaN or Infinity)");
                    return null;
                }
                
                if (lat < -90 || lat > 90)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Latitude out of range for spot {spot.Name}: {lat} (must be -90 to 90)");
                    return null;
                }

                if (double.IsNaN(lon) || double.IsInfinity(lon))
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Invalid longitude for spot {spot.Name}: {lon} (NaN or Infinity)");
                    return null;
                }
                
                if (lon < -180 || lon > 180)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Longitude out of range for spot {spot.Name}: {lon} (must be -180 to 180)");
                    return null;
                }

                var pin = new Pin
                {
                    Label = "", // Empty label to prevent callout
                    Address = "", // Empty address to prevent callout
                    Location = new Location(lat, lon),
                    Type = PinType.Place,
                    BindingContext = spot
                };

                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✓ Successfully created pin for {spot.Name} with valid coordinates");
                return pin;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Exception creating pin for spot {spot?.Name ?? "unknown"}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
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

        /// <summary>
        /// Thread-safe atomic collection update to prevent UI flicker and race conditions
        /// </summary>
        private void RefreshSpotsList(IEnumerable<Models.Domain.Spot> spots)
        {
            Application.Current?.Dispatcher.Dispatch(() => {
                try
                {
                    var spotsList = spots?.ToList() ?? new List<Models.Domain.Spot>();
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] RefreshSpotsList: Processing {spotsList.Count} spots with atomic update");
                    
                    // ✅ FIXED: Atomic collection replacement instead of Clear/Add pattern
                    // This prevents race conditions and reduces PropertyChanged events from O(n) to O(1)
                    var newCollection = new ObservableCollection<Models.Domain.Spot>(spotsList);
                    Spots = newCollection;
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] RefreshSpotsList: Atomic update completed - {Spots.Count} spots");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] RefreshSpotsList failed: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                    
                    // Ensure we always have a valid collection
                    if (Spots == null)
                    {
                        Spots = new ObservableCollection<Models.Domain.Spot>();
                    }
                }
            });
        }

        /// <summary>
        /// Thread-safe atomic SpotTypes collection update
        /// </summary>
        private void RefreshSpotTypesList(IEnumerable<SpotType> types)
        {
            Application.Current?.Dispatcher.Dispatch(() => {
                try 
                {
                    var typesList = types?.ToList() ?? new List<SpotType>();
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] RefreshSpotTypesList: Atomic update with {typesList.Count} types");
                    
                    // ✅ FIXED: Atomic collection replacement
                    var newCollection = new ObservableCollection<SpotType>(typesList);
                    SpotTypes = newCollection;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] RefreshSpotTypesList failed: {ex.Message}");
                    
                    // Ensure we always have a valid collection
                    if (SpotTypes == null)
                    {
                        SpotTypes = new ObservableCollection<SpotType>();
                    }
                }
            });
        }

        private async Task ApplySpotTypeFilter()
        {
            try
            {
                // Ensure we're on the UI thread
                if (Application.Current?.Dispatcher?.IsDispatchRequired == true)
                {
                    await Application.Current.Dispatcher.DispatchAsync(() => ApplySpotTypeFilterCore());
                }
                else
                {
                    ApplySpotTypeFilterCore();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ApplySpotTypeFilter failed: {ex.Message}");
            }
        }

        private void ApplySpotTypeFilterCore()
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ApplySpotTypeFilter: SelectedSpotType = {SelectedSpotType?.Name ?? "null"}");
            
            IEnumerable<Models.Domain.Spot> filteredSpots;
            
            if (SelectedSpotType == null)
            {
                // Show all spots
                filteredSpots = Spots;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Showing all {Spots?.Count ?? 0} spots");
            }
            else
            {
                // Filter by selected type
                filteredSpots = Spots?.Where(s => s.TypeId == SelectedSpotType.Id) ?? new List<Models.Domain.Spot>();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Filtered to {filteredSpots.Count()} spots of type {SelectedSpotType.Name}");
            }
            
            // Update pins based on filtered spots
            UpdatePinsFromFilteredSpots(filteredSpots);
            
            // Update empty state
            UpdateEmptyState();
        }

        private void UpdatePinsFromFilteredSpots(IEnumerable<Models.Domain.Spot> filteredSpots)
        {
            try
            {
                // Ensure we're on UI thread when manipulating collections
                if (Application.Current?.Dispatcher?.IsDispatchRequired == true)
                {
                    Application.Current.Dispatcher.Dispatch(() => UpdatePinsFromFilteredSpotsCore(filteredSpots));
                }
                else
                {
                    UpdatePinsFromFilteredSpotsCore(filteredSpots);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] UpdatePinsFromFilteredSpots failed: {ex.Message}");
            }
        }

        private void UpdatePinsFromFilteredSpotsCore(IEnumerable<Models.Domain.Spot> filteredSpots)
        {
            var validPins = new List<Pin>();
            
            foreach (var spot in filteredSpots)
            {
                var pin = CreatePinFromSpot(spot);
                if (pin != null)
                {
                    validPins.Add(pin);
                }
            }
            
            // Clear and update pins collection
            Pins.Clear();
            foreach (var pin in validPins)
            {
                Pins.Add(pin);
            }
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Updated pins: {Pins.Count} pins from {filteredSpots.Count()} filtered spots");
            OnPropertyChanged(nameof(Pins));
        }
        
        private void UpdateEmptyState()
        {
            Application.Current?.Dispatcher.Dispatch(() => {
                // Only show empty state if:
                // 1. Not busy loading
                // 2. No spots are loaded
                // 3. No network error
                // 4. Not currently searching or filtering (to avoid showing empty state during search)
                IsEmptyState = !IsBusy && 
                              (Spots?.Count ?? 0) == 0 && 
                              !IsNetworkError && 
                              !IsSearching && 
                              !IsFiltering &&
                              string.IsNullOrEmpty(SearchText);
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

        // ===================== MÉTHODES D'OPTIMISATION PERFORMANCE =====================

        /// <summary>
        /// Version optimisée du chargement des SpotTypes avec cache
        /// </summary>
        private async Task LoadSpotTypesOptimized()
        {
            try
            {
                // Check cache first to avoid unnecessary DB hits
                if (_lastSpotTypesLoad.AddMinutes(CACHE_EXPIRY_MINUTES) > DateTime.UtcNow && SpotTypes?.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] SpotTypes loaded from cache");
                    return;
                }

                var spotTypes = await _spotTypeRepository.GetActiveSpotTypesAsync();
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    RefreshSpotTypesList(spotTypes);
                    _lastSpotTypesLoad = DateTime.UtcNow;
                });
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadSpotTypesOptimized completed - {spotTypes?.Count() ?? 0} types loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotTypesOptimized failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Version optimisée du chargement des Spots avec traitement par batch
        /// </summary>
        private async Task LoadSpotsOptimized()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] LoadSpotsOptimized started");

                // Use Task.Run for database operations to avoid blocking UI
                var spots = await Task.Run(async () =>
                {
                    return await _spotRepository.GetSpotsByValidationStatusAsync(SpotValidationStatus.Approved);
                });

                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadSpotsOptimized - Retrieved {spots?.Count() ?? 0} spots from repository");

                // Process spots in batches to maintain UI responsiveness
                await ProcessSpotsInBatches(spots);
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadSpotsOptimized completed. Final counts - Spots: {Spots?.Count ?? 0}, Pins: {Pins?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotsOptimized failed: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les spots. Veuillez réessayer.", "OK");
                });
            }
        }

        /// <summary>
        /// Optimized batch processing with atomic UI updates to eliminate flicker and race conditions
        /// </summary>
        private async Task ProcessSpotsInBatches(IEnumerable<Models.Domain.Spot> spots)
        {
            try
            {
                var spotsList = spots?.ToList() ?? new List<Models.Domain.Spot>();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ProcessSpotsInBatches: Processing {spotsList.Count} spots with optimized batching");
                
                // ✅ FIXED: Process all data off UI thread
                var (processedSpots, processedPins) = await Task.Run(() =>
                {
                    var pins = new List<Pin>();
                    
                    foreach (var spot in spotsList)
                    {
                        // Create pins off UI thread - IMPORTANT: Empty Label/Address to prevent callouts
                        if (IsValidSpotCoordinates(spot))
                        {
                            var pin = new Pin
                            {
                                Label = "", // Empty label to prevent callout
                                Address = "", // Empty address to prevent callout
                                Type = PinType.Place,
                                Location = new Location((double)spot.Latitude, (double)spot.Longitude),
                                BindingContext = spot // Store spot data for click detection
                            };
                            pins.Add(pin);
                        }
                    }
                    
                    return (spotsList, pins);
                });
                
                // ✅ FIXED: Single atomic UI update instead of multiple batch updates
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Spots = new ObservableCollection<Models.Domain.Spot>(processedSpots);
                    Pins = new ObservableCollection<Pin>(processedPins);
                    UpdateEmptyState();
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ProcessSpotsInBatches: Atomic update completed - Spots: {Spots.Count}, Pins: {Pins.Count}");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ProcessSpotsInBatches failed: {ex.Message}");
                
                // Ensure we have valid collections even on error
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (Spots == null) Spots = new ObservableCollection<Models.Domain.Spot>();
                    if (Pins == null) Pins = new ObservableCollection<Pin>();
                });
                
                throw;
            }
        }
        
        /// <summary>
        /// Validates spot coordinates for pin creation
        /// </summary>
        private bool IsValidSpotCoordinates(Models.Domain.Spot spot)
        {
            return spot?.Latitude != null && spot.Longitude != null &&
                   spot.Latitude != 0 && spot.Longitude != 0 &&
                   Math.Abs((double)spot.Latitude) <= 90 && Math.Abs((double)spot.Longitude) <= 180;
        }
        
        protected override void Dispose(bool disposing)
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