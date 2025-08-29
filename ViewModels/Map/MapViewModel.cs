using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
// 🚫 Repositories supprimés - Version temporaire sans accès base de données
// using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;
using SubExplore.ViewModels.Base;
using SubExplore.ViewModels.Profile;
using SubExplore.ViewModels.Spots;
// 🚫 ViewModels.Favorites supprimé
// using SubExplore.ViewModels.Favorites;
using SubExplore.Models.Menu;
using SubExplore.Helpers.Extensions;
using MenuItemModel = SubExplore.Models.Menu.MenuItem;
using SubExplore.Models.Supabase;

namespace SubExplore.ViewModels.Map
{
    public partial class MapViewModel : ViewModelBase, IDisposable
    {
        // 🚫 Repository temporairement désactivé
        // private readonly ISpotRepository _spotRepository;
        private readonly ILocationService _locationService;
        // 🚫 Repository temporairement désactivé
        // private readonly ISpotTypeRepository _spotTypeRepository;
        private readonly ISupabaseApiService _supabaseApiService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IPlatformMapService _platformMapService;
        private readonly IApplicationPerformanceService _performanceService;
        private readonly IPinSelectionService _pinSelectionService;

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

        [ObservableProperty]
        private ObservableCollection<string> _searchSuggestions;

        [ObservableProperty]
        private bool _areSuggestionsVisible;

        // Nouvelles propriétés pour l'organisation hiérarchique des filtres
        [ObservableProperty]
        private string? _selectedCategory;

        [ObservableProperty]
        private bool _isSubFiltersVisible;

        [ObservableProperty]
        private ObservableCollection<SpotType> _currentSubFilters;

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

        // 🚫 Service temporairement désactivé
        // private readonly IDatabaseService _databaseService;
        // 🚫 Repository temporairement désactivé
        // private readonly IUserRepository _userRepository;
        private readonly ISettingsService _settingsService;
        private readonly ISimpleAuthenticationService _authenticationService;

        // Public property to expose PinSelectionService to the View for integration
        public IPinSelectionService PinSelectionService => _pinSelectionService;
        
        // Additional properties for UI feedback
        public bool IsAnyFilterActive => !string.IsNullOrEmpty(SelectedCategory) || SelectedSpotType != null || !string.IsNullOrEmpty(SearchText);
        
        [ObservableProperty]
        private int _filteredSpotsCount;
        
        // Property change notifications
        partial void OnSelectedCategoryChanged(string? value)
        {
            OnPropertyChanged(nameof(IsAnyFilterActive));
        }
        
        partial void OnSelectedSpotTypeChanged(SpotType? value) 
        {
            OnPropertyChanged(nameof(IsAnyFilterActive));
        }
        
        partial void OnSearchTextChanged(string? value)
        {
            OnPropertyChanged(nameof(IsAnyFilterActive));
        }
        
        partial void OnPinsChanged(ObservableCollection<Pin>? value)
        {
            FilteredSpotsCount = value?.Count ?? 0;
        }

        public MapViewModel(
            // 🚫 Repositories supprimés temporairement
            // ISpotRepository spotRepository,
            ILocationService locationService,
            // ISpotTypeRepository spotTypeRepository,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            IPlatformMapService platformMapService,
            // IDatabaseService databaseService,
            // IUserRepository userRepository,
            IDialogService dialogService,
            INavigationService navigationService,
            ISettingsService settingsService,
            ISimpleAuthenticationService authenticationService,
            IApplicationPerformanceService performanceService,
            IPinSelectionService pinSelectionService,
            ISupabaseApiService supabaseApiService)
            : base(dialogService, navigationService)
        {
            // 🚫 Repositories temporairement désactivés
            // _spotRepository = spotRepository;
            _locationService = locationService;
            // _spotTypeRepository = spotTypeRepository;
            _supabaseApiService = supabaseApiService;
            // _databaseService = databaseService;
            // _userRepository = userRepository;
            _settingsService = settingsService;
            _authenticationService = authenticationService;
            _performanceService = performanceService;
            _pinSelectionService = pinSelectionService;
            _configuration = configuration;
            _platformMapService = platformMapService;

            Spots = new ObservableCollection<Models.Domain.Spot>();
            Pins = new ObservableCollection<Pin>();
            SpotTypes = new ObservableCollection<SpotType>();
            MenuSections = new ObservableCollection<MenuSection>();
            SearchSuggestions = new ObservableCollection<string>();
            CurrentSubFilters = new ObservableCollection<SpotType>();
            
            // Subscribe to authentication state changes to update menu dynamically
            // 🚫 StateChanged event temporairement désactivé
            // _authenticationService.StateChanged += OnAuthenticationStateChanged;

            // Valeurs par défaut pour Paris, seront remplacées par la géolocalisation si disponible
            double defaultLat = _configuration.GetValue<double>("AppSettings:DefaultLatitude", 48.8566);
            double defaultLong = _configuration.GetValue<double>("AppSettings:DefaultLongitude", 2.3522);
            double defaultZoom = _configuration.GetValue<double>("AppSettings:DefaultZoomLevel", 12);

            MapLatitude = defaultLat;
            MapLongitude = defaultLong;
            MapZoomLevel = defaultZoom;
            
            System.Diagnostics.Debug.WriteLine($"[INFO] MapViewModel initialized with default coordinates (Paris): {MapLatitude}, {MapLongitude}, zoom: {MapZoomLevel}");

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
                
                try
                {
                    // Step 1: Initialize platform-specific map configuration
                    var mapInitialized = await _platformMapService.InitializePlatformMapAsync();
                    if (!mapInitialized)
                    {
                        System.Diagnostics.Debug.WriteLine("[ERROR] Platform map initialization failed");
                        await DialogService.ShowAlertAsync("Erreur", "Impossible d'initialiser les cartes pour cette plateforme", "D'accord");
                        return;
                    }

                    // Step 2: Load spot types FIRST (required for filters and conversion to work)
                    await LoadSpotTypesOptimized();
                    
                    if (SpotTypes?.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("[WARNING] ⚠️ No spot types loaded - filters and spot conversion will not work properly!");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[SUCCESS] ✅ Loaded {SpotTypes.Count} spot types for filtering and conversion");
                        
                        // ✅ AMÉLIORATION: Log SpotType details for debugging
                        foreach (var spotType in SpotTypes.Take(3))
                        {
                        }
                    }

                    // Step 3: ✅ CORRECTION: Load spots synchronously to ensure proper initialization order
                    await LoadSpotsOptimized(); // Changed from background loading to synchronous

                    // Step 4: Finalize initialization on UI thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        
                        _isInitialized = true;
                        _isInitializing = false;
                        IsBusy = false;
                        
                        // Initialize menu and other UI elements  
                        InitializeMapPosition();
                        
                        System.Diagnostics.Debug.WriteLine("[SUCCESS] ✅ MapViewModel initialization completed successfully");
                    });

                    // Step 4.5: Force load current user and update menu AFTER map is initialized
                    try
                    {
                        await LoadCurrentUser();
                    }
                    catch (Exception userEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Force LoadCurrentUser failed: {userEx.Message}");
                    }

                    // Step 5: Try to get user's location after map is initialized
                    try
                    {
                        // Check if location services are available
                        var isLocationAvailable = await _locationService.IsLocationServiceEnabledAsync();
                        if (isLocationAvailable)
                        {
                            // Request permission and get location
                            var hasPermission = await _locationService.RequestLocationPermissionAsync();
                            if (hasPermission)
                            {
                                var location = await _locationService.GetCurrentLocationAsync();
                                if (location != null)
                                {
                                    await MainThread.InvokeOnMainThreadAsync(() =>
                                    {
                                        UserLatitude = Convert.ToDouble(location.Latitude);
                                        UserLongitude = Convert.ToDouble(location.Longitude);
                                        MapLatitude = UserLatitude;
                                        MapLongitude = UserLongitude;
                                        IsLocationAvailable = true;
                                        
                                        System.Diagnostics.Debug.WriteLine($"[SUCCESS] User location obtained on startup: {UserLatitude}, {UserLongitude}");
                                        
                                        // Force map refresh to center on user location
                                        ForceMapRefresh();
                                    });
                                    
                                    // Optionally reload spots based on user location
                                    await LoadSpotsOptimized();
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("[INFO] Location service returned null, using default location (Paris)");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[INFO] Location permission denied, using default location (Paris)");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[INFO] Location services not available, using default location (Paris)");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WARNING] Could not get user location on startup: {ex.Message}");
                        // Continue with default location - this is not a critical error
                    }
                }
                catch (Exception innerEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] MapViewModel initialization failed: {innerEx.Message}");
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        _isInitialized = false;
                        _isInitializing = false;
                        IsBusy = false;
                        await DialogService.ShowAlertAsync("Erreur", $"Erreur d'initialisation: {innerEx.Message}", "D'accord");
                    });
                }
            }
            catch (Exception ex)
            {
                _isInitializing = false;
                IsBusy = false;
                System.Diagnostics.Debug.WriteLine($"[ERROR] InitializeAsync failed: {ex.Message}");
                await DialogService.ShowAlertAsync("Erreur", $"Une erreur s'est produite lors de l'initialisation : {ex.Message}", "D'accord");
            }
        }

        private async Task LoadDataWithUIYields()
        {
            try
            {
                // Exécuter toutes les opérations de chargement en parallèle pour améliorer les performances
                
                var spotTypesTask = Task.Run(async () =>
                {
                    await LoadSpotTypesOptimized();
                });
                
                var userTask = Task.Run(async () =>
                {
                    try
                    {
                        await LoadCurrentUser();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] LoadCurrentUser failed: {ex.Message}");
                    }
                });
                
                var locationTask = Task.Run(async () =>
                {
                    var isAvailable = await _locationService.IsLocationServiceEnabledAsync();
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        IsLocationAvailable = isAvailable;
                    });
                });
                
                // Attendre que toutes les tâches parallèles se terminent
                await Task.WhenAll(spotTypesTask, userTask, locationTask);
                
                // Yield minimal to UI thread
                await Task.Delay(1);
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadDataWithUIYields failed: {ex.Message}");
                throw;
            }
        }


        [RelayCommand]
        private async Task InitializeMap()
        {
            await InitializeAsync();
        }

        [RelayCommand]
        private async Task LoadSpots()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                IEnumerable<Models.Domain.Spot> spots;

                // TEMPORAIRE : Force le chargement de tous les spots approuvés pour diagnostic
                
                // 🚫 Repository temporairement désactivé
                // spots = await _spotRepository.GetSpotsByValidationStatusAsync(SpotValidationStatus.Approved);
                spots = new List<Spot>(); // Liste vide temporaire
                
                // Log de diagnostic supplémentaire
                
                // Si on obtient des spots, vérifions leur contenu
                if (spots != null && spots.Any())
                {
                    foreach (var spot in spots.Take(5))
                    {
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[WARNING] ✗ No spots returned from repository");
                    
                    try
                    {
                        // Test database connectivity
                        // 🚫 DatabaseService temporairement désactivé
                        // await _databaseService.TestConnectionAsync();
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] ✗ Database connection failed: {dbEx.Message}");
                    }
                }

                var spotsCount = spots?.Count() ?? 0;

                if (spotsCount == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[WARNING] No spots found in repository");
                    await DialogService.ShowToastAsync("Aucun spot trouvé dans la région");
                }
                else
                {
                    foreach (var spot in spots.Take(3)) // Log first 3 spots for debugging
                    {
                    }
                }

                RefreshSpotsList(spots);
                
                // Allow UI to update between operations
                await Task.Delay(50);
                
                UpdatePins();
                
                // Allow UI to refresh after pins are updated
                await Task.Delay(50);
                
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpots failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les spots. Veuillez réessayer plus tard.", "D'accord");
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
                // 🚫 Repository temporairement désactivé
                // var types = await _spotTypeRepository.GetActiveTypesAsync();
                var types = new List<SpotType>(); // Liste vide temporaire

                RefreshSpotTypesList(types);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotTypes failed: {ex.Message}");
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les types de spots.", "D'accord");
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
                        "D'accord");
                    return;
                }
                
                // Request permission if not already granted
                bool hasPermission = await _locationService.RequestLocationPermissionAsync();

                if (!hasPermission)
                {
                    IsLocationAvailable = false;
                    await DialogService.ShowAlertAsync("Permissions", 
                        "L'accès à la localisation est nécessaire pour cette fonctionnalité. Vous pouvez l'activer dans les paramètres.", 
                        "D'accord");
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
                        "D'accord");
                }
            }
            catch (Exception ex)
            {
                IsLocationAvailable = false;
                await DialogService.ShowAlertAsync("Localisation", 
                    "La géolocalisation n'est pas disponible.", 
                    "D'accord");
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


                // Use the new category-based filtering system
                string categoryName = filterType.ToLower() switch
                {
                    "activities" => "Activités",
                    "activités" => "Activités",
                    "structures" => "Structures", 
                    "boutiques" => "Boutiques",
                    "shops" => "Boutiques",
                    _ => null // Show all
                };

                if (!string.IsNullOrEmpty(categoryName))
                {
                    await ApplyCategoryFilter(categoryName);
                }
                else
                {
                    await ClearFilters();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] FilterSpots failed: {ex.Message}");
                await DialogService.ShowAlertAsync("Erreur", "Impossible de filtrer les spots. Veuillez réessayer plus tard.", "D'accord");
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
            
            if (spot == null) 
            {
                System.Diagnostics.Debug.WriteLine("[ERROR] ShowSpotMiniWindow called with null spot");
                return;
            }

            
            SelectedSpot = spot;
            IsSpotMiniWindowVisible = true;
            
            
            // Force property change notifications
            OnPropertyChanged(nameof(IsSpotMiniWindowVisible));
            OnPropertyChanged(nameof(SelectedSpot));
            
        }

        [RelayCommand]
        private void CloseSpotMiniWindow()
        {
            IsSpotMiniWindowVisible = false;
            SelectedSpot = null;
            
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
                
                
                // Close mini window after capturing data
                CloseSpotMiniWindow();
                
                // Check if NavigationService is available
                if (NavigationService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] NavigationService is null - cannot navigate");
                    await DialogService.ShowAlertAsync("Erreur", "Service de navigation non disponible", "D'accord");
                    return;
                }
                
                
                // Final safety check before navigation
                if (NavigationService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] ViewSpotDetails: NavigationService became null before navigation call");
                    await DialogService.ShowAlertAsync("Erreur", "Service de navigation non disponible", "D'accord");
                    return;
                }
                
                
                // Navigate to full details with isolated try-catch
                try
                {
                    // ✅ CORRECTION: Réactiver la navigation vers SpotDetailsViewModel
                    await NavigationService.NavigateToAsync<ViewModels.Spots.SpotDetailsViewModel>(spotId);
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
                await DialogService.ShowAlertAsync("Erreur", $"Impossible d'ouvrir les détails du spot: {ex.Message}", "D'accord");
            }
        }

        [RelayCommand]
        private void TestMiniWindow()
        {
            
            // Create a test spot to verify mini window functionality
            var testSpot = new Models.Domain.Spot
            {
                Id = Guid.NewGuid(),
                Name = "DEBUG TEST SPOT",
                DifficultyLevel = DifficultyLevel.Beginner,
                Latitude = 43.2965m,
                Longitude = 5.3698m,
                Type = new SpotType
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Plongée",
                    ColorCode = "#FF0000"
                }
            };
            
            ShowSpotMiniWindow(testSpot);
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
                    
                }

                // Navigate to AddSpot with location parameters
                // 🚫 AddSpotViewModel temporairement désactivé
                // await NavigationService.NavigateToAsync<ViewModels.Spots.AddSpotViewModel>(locationParameter);
                await DialogService.ShowToastAsync("🚧 AddSpot temporairement désactivé");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] NavigateToAddSpot failed: {ex.Message}");
                await DialogService.ShowAlertAsync("Erreur", "Impossible de naviguer vers l'ajout de spot. Veuillez réessayer.", "D'accord");
            }
        }

        [RelayCommand]
        private async Task SearchTextChanged()
        {
            // Cancel previous search
            _searchCancellationToken?.Cancel();
            _searchCancellationToken = new System.Threading.CancellationTokenSource();
            
            try
            {
                // Immediate suggestions for responsive UI
                if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 1)
                {
                    _ = GenerateSearchSuggestionsAsync(_searchCancellationToken.Token);
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        AreSuggestionsVisible = false;
                        SearchSuggestions.Clear();
                    });
                }

                // Debounce actual search - wait 300ms after user stops typing (reduced for better UX)
                await Task.Delay(300, _searchCancellationToken.Token).ConfigureAwait(false);
                
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

                // Hide suggestions when performing actual search
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AreSuggestionsVisible = false;
                    SearchSuggestions.Clear();
                });

                // Add a small delay to ensure suggestion operations complete
                await Task.Delay(100);

                // Use standard search without geographic limitations to find spots anywhere
                // 🚫 Repository temporairement désactivé
                // var searchResults = await _spotRepository.SearchSpotsAsync(SearchText);
                var searchResults = new List<Spot>(); // Liste vide temporaire

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
                    await DialogService.ShowToastAsync("Aucun spot trouvé pour cette recherche");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Search failed: {ex.Message}");
                await DialogService.ShowAlertAsync("Erreur", "recherche impossible", "D'accord");
            }
            finally
            {
                IsBusy = false;
                IsSearching = false;
                UpdateEmptyState();
            }
        }

        /// <summary>
        /// Generate search suggestions based on current search text with cancellation support
        /// </summary>
        private async Task GenerateSearchSuggestionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (string.IsNullOrWhiteSpace(SearchText) || SearchText.Length < 1)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        AreSuggestionsVisible = false;
                        SearchSuggestions.Clear();
                    });
                    return;
                }

                // Get optimized suggestions from repository with cancellation support
                // 🚫 Repository temporairement désactivé
                // var databaseSuggestions = await _spotRepository.GetSearchSuggestionsAsync(SearchText, 3).ConfigureAwait(false);
                var databaseSuggestions = new List<string>(); // Liste vide temporaire
                
                // Check for cancellation again after database call
                if (cancellationToken.IsCancellationRequested)
                    return;

                var suggestions = new List<string>(databaseSuggestions);

                // Add contextual suggestions based on search terms
                string searchLower = SearchText.ToLower();
                if (searchLower.Contains("mer") || searchLower.Contains("ocean"))
                    suggestions.Add("Spots en mer");
                if (searchLower.Contains("lac"))
                    suggestions.Add("Spots de lac");
                if (searchLower.Contains("profond"))
                    suggestions.Add("Plongée profonde");
                if (searchLower.Contains("debutant") || searchLower.Contains("facile"))
                    suggestions.Add("Spots débutant");
                if (searchLower.Contains("avance") || searchLower.Contains("expert"))
                    suggestions.Add("Spots avancés");

                // Final cancellation check before UI update
                if (cancellationToken.IsCancellationRequested)
                    return;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SearchSuggestions.Clear();
                    foreach (var suggestion in suggestions.Take(5))
                    {
                        SearchSuggestions.Add(suggestion);
                    }
                    AreSuggestionsVisible = SearchSuggestions.Count > 0;
                });
            }
            catch (TaskCanceledException)
            {
                // Expected when search is cancelled
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GenerateSearchSuggestions failed: {ex.Message}");
                // Don't throw - this is a background operation
            }
        }

        /// <summary>
        /// Select a search suggestion and perform search
        /// </summary>
        [RelayCommand]
        private async Task SelectSuggestion(string suggestion)
        {
            if (string.IsNullOrEmpty(suggestion))
                return;

            try
            {
                SearchText = suggestion;
                AreSuggestionsVisible = false;
                await SearchSpots();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SelectSuggestion failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ClearFilters()
        {
            try
            {
                SelectedSpotType = null;
                SelectedCategory = null;
                SearchText = string.Empty;
                AreSuggestionsVisible = false;
                SearchSuggestions.Clear();
                IsSubFiltersVisible = false;
                CurrentSubFilters.Clear();
                IsFiltering = false;
                IsSearching = false;

                // Apply filter (null means show all) instead of reloading from database
                await ApplySpotTypeFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ClearFilters failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Commande pour filtrer par catégorie principale
        /// </summary>
        [RelayCommand]
        private async Task FilterByCategory(string categoryName)
        {
            try
            {

                // Si on reclique sur la même catégorie, fermer le menu
                if (SelectedCategory == categoryName)
                {
                    IsSubFiltersVisible = false;
                    CurrentSubFilters.Clear();
                    SelectedCategory = null;
                    SelectedSpotType = null;
                    
                    // Remettre tous les spots visibles
                    UpdatePinsFromFilteredSpots(Spots ?? Enumerable.Empty<Models.Domain.Spot>());
                    return;
                }

                SelectedCategory = categoryName;
                SelectedSpotType = null;

                // Remplir les sous-filtres pour cette catégorie
                CurrentSubFilters.Clear();
                var categorySpotTypes = SpotTypes.FilterByMainCategory(categoryName);
                
                foreach (var spotType in categorySpotTypes)
                {
                    CurrentSubFilters.Add(spotType);
                }

                // Afficher le menu des sous-filtres si on a des éléments
                IsSubFiltersVisible = CurrentSubFilters.Count > 0;

                // Appliquer le filtre de catégorie (afficher tous les spots de cette catégorie)
                await ApplyCategoryFilter(categoryName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] FilterByCategory failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Applique le filtre de catégorie aux spots
        /// </summary>
        private async Task ApplyCategoryFilter(string categoryName)
        {
            try
            {
                IsFiltering = true;

                // Debug log all spots and their types
                if (Spots != null)
                {
                    foreach (var spot in Spots)
                    {
                        var typeName = spot.Type?.Name ?? "NULL_TYPE";
                        var belongsToCategory = spot.Type?.BelongsToCategory(categoryName) ?? false;
                    }
                }
                
                var filteredSpots = Spots?.Where(s => s.Type != null && s.Type.BelongsToCategory(categoryName)) ?? Enumerable.Empty<Models.Domain.Spot>();
                var filteredSpotsList = filteredSpots.ToList(); // Materialize to avoid multiple enumeration
                
                
                // Update the UI properties that show filter counts
                SelectedCategory = categoryName;
                
                UpdatePinsFromFilteredSpots(filteredSpotsList);
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ApplyCategoryFilter failed: {ex.Message}");
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'appliquer le filtre par catégorie", "OK");
            }
            finally
            {
                IsFiltering = false;
            }
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
                        
                        // 🚫 AddSpotViewModel temporairement désactivé
                        // await NavigationService.NavigateToAsync<ViewModels.Spots.AddSpotViewModel>(locationParameter);
                        await DialogService.ShowToastAsync("🚧 AddSpot temporairement désactivé");
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
            // 🚫 MySpotsViewModel temporairement désactivé
            // await NavigationService.NavigateToAsync<MySpotsViewModel>();
            await DialogService.ShowToastAsync("🚧 MySpots temporairement désactivé");
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
            // 🚫 FavoriteSpotsViewModel temporairement désactivé
            // await NavigationService.NavigateToAsync<FavoriteSpotsViewModel>();
            await DialogService.ShowToastAsync("🚧 Favorites temporairement désactivé");
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
        private async Task NavigateToSpotValidation()
        {
            // 🚫 SpotValidationViewModel temporairement désactivé
            // await NavigateToAsync<ViewModels.Admin.SpotValidationViewModel>();
            await DialogService.ShowToastAsync("🚧 SpotValidation temporairement désactivé");
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
                    await DialogService.ShowAlertAsync("Erreur", "Erreur lors de la déconnexion", "D'accord");
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
            
            // Admin Section (only for moderators and administrators)
            MenuSection? adminSection = null;
            
            // Check admin permissions
            var currentUser = _authenticationService.CurrentUser;
            
            if (currentUser?.AccountType == Models.Enums.AccountType.ExpertModerator ||
                currentUser?.AccountType == Models.Enums.AccountType.Administrator)
            {
                adminSection = new MenuSection
                {
                    Title = "Administration",
                    Items = new ObservableCollection<MenuItemModel>
                    {
                        new MenuItemModel
                        {
                            Title = "Validation des Spots",
                            Icon = "✅",
                            Description = "Gérer la validation des spots",
                            Command = NavigateToSpotValidationCommand,
                            IsEnabled = true
                        }
                    }
                };
            }
            else
            {
            }
            
            MenuSections.Add(mainSection);
            MenuSections.Add(userSection);
            MenuSections.Add(settingsSection);
            
            // Add admin section if user has permissions
            if (adminSection != null)
            {
                MenuSections.Add(adminSection);
            }
            
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
                        
                        
                        // Re-initialize menu to show/hide admin options based on user role
                        InitializeMenu();
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
            
            
            // Re-initialize menu to hide admin options for unauthenticated users
            InitializeMenu();
            
            // Optional: Show login prompt or redirect to login
            // For now, just log the state
        }

        /// <summary>
        /// Handle authentication state changes to update menu dynamically
        /// </summary>
        private void OnAuthenticationStateChanged(object? sender, AuthenticationStateChangedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Authentication state changed: {e.IsAuthenticated}, User: {e.User?.DisplayName ?? "NULL"}, Reason: {e.Reason}");
                
                if (e.IsAuthenticated && e.User != null)
                {
                    UserDisplayName = $"{e.User.FirstName} {e.User.LastName}";
                    UserEmail = e.User.Email;
                    UserAvatarUrl = e.User.AvatarUrl ?? "default_avatar.png";
                    
                    System.Diagnostics.Debug.WriteLine($"[MapViewModel] Updated user info for: {e.User.Id} with account type: {e.User.AccountType}");
                }
                else
                {
                    UserDisplayName = "Utilisateur Invité";
                    UserEmail = "guest@subexplore.com";
                    UserAvatarUrl = "default_avatar.png";
                    
                    System.Diagnostics.Debug.WriteLine("[MapViewModel] User logged out - reverting to guest info");
                }
                
                // Re-initialize menu with new user context
                System.Diagnostics.Debug.WriteLine("[MapViewModel] Re-initializing menu due to authentication state change");
                InitializeMenu();
                
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Menu refreshed - sections count: {MenuSections.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Error handling authentication state change: {ex.Message}");
            }
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
            
            // Additional map debugging
        }
        
        public void InitializeMapPosition()
        {
            // Ensure we have valid coordinates
            if (MapLatitude == 0 && MapLongitude == 0)
            {
                // Use default coordinates from configuration (Paris)
                double defaultLat = _configuration.GetValue<double>("AppSettings:DefaultLatitude", 48.8566);
                double defaultLong = _configuration.GetValue<double>("AppSettings:DefaultLongitude", 2.3522);
                double defaultZoom = _configuration.GetValue<double>("AppSettings:DefaultZoomLevel", 12);
                
                MapLatitude = defaultLat;
                MapLongitude = defaultLong;
                MapZoomLevel = defaultZoom;
                
                System.Diagnostics.Debug.WriteLine($"[INFO] Map initialized with default coordinates (Paris): {MapLatitude}, {MapLongitude}, zoom: {MapZoomLevel}");
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
                    
                    if (Spots == null || !Spots.Any())
                    {
                        Pins = new ObservableCollection<Pin>(); // ✅ FIXED: Atomic replacement
                        return;
                    }

                    // Debug first few spots for troubleshooting
                    foreach (var spot in Spots.Take(3))
                    {
                    }

                    var validPins = new List<Pin>();
                    int nullPinCount = 0;

                    foreach (var spot in Spots)
                    {
                        var pin = CreatePinFromSpot(spot);
                        if (pin != null)
                        {
                            validPins.Add(pin);
                        }
                        else
                        {
                            nullPinCount++;
                        }
                    }


                    // ✅ FIXED: Atomic collection replacement instead of Clear/Add pattern
                    Pins = new ObservableCollection<Pin>(validPins);
                    
                    // Mettre à jour le compteur FilteredSpotsCount
                    FilteredSpotsCount = Pins.Count;
                    
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
                    Label = "", // Empty label to prevent Google InfoWindow
                    Address = "", // Empty address to prevent Google InfoWindow
                    Type = PinType.Place,
                    Location = new Location(lat, lon),
                    BindingContext = spot
                };

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
                    
                    // ✅ FIXED: Atomic collection replacement instead of Clear/Add pattern
                    // This prevents race conditions and reduces PropertyChanged events from O(n) to O(1)
                    var newCollection = new ObservableCollection<Models.Domain.Spot>(spotsList);
                    Spots = newCollection;
                    
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
            
            IEnumerable<Models.Domain.Spot> filteredSpots;
            
            if (SelectedSpotType == null)
            {
                // Show all spots
                filteredSpots = Spots;
            }
            else
            {
                // Filter by selected type
                filteredSpots = Spots?.Where(s => s.TypeId == SelectedSpotType.Id) ?? new List<Models.Domain.Spot>();
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
            
            
            // Replace the entire collection to trigger PropertyChanged
            Pins = new ObservableCollection<Pin>(validPins);
            
            
            // Mettre à jour le compteur FilteredSpotsCount
            FilteredSpotsCount = Pins.Count;
            
            // Force property changed notification (should be automatic with [ObservableProperty] but let's be sure)
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

        /// <summary>
        /// Handles the case when no spots are found - diagnoses the issue and provides appropriate feedback
        /// </summary>
        private async Task HandleEmptySpotState()
        {
            try
            {
                
                // Check if this is a data integrity issue or a filtering issue
                // ✅ Utilisation du service Supabase API
                var supabaseSpots = await _supabaseApiService.GetSpotsAsync();
                var totalSpots = supabaseSpots?.Select(ConvertToDomainSpot).Where(s => s != null).ToList() ?? new List<Spot>();
                var totalCount = totalSpots.Count();
                
                
                if (totalCount == 0)
                {
                    // No spots in database at all
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        IsEmptyState = true;
                        await DialogService.ShowAlertAsync(
                            "Base de données vide", 
                            "Aucun spot n'a été trouvé dans la base de données. Cela peut être dû à une migration récente. Veuillez contacter le support.", 
                            "Compris");
                    });
                }
                else
                {
                    // Spots exist but none are visible - check validation status and SpotType issues
                    var approvedSpots = totalSpots.Where(s => s.ValidationStatus == SpotValidationStatus.Approved).Count();
                    var spotsWithActiveTypes = totalSpots.Where(s => s.Type != null && s.Type.IsActive).Count();
                    
                    
                    if (approvedSpots == 0)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            IsEmptyState = true;
                            await DialogService.ShowAlertAsync(
                                "Spots en attente", 
                                $"Il y a {totalCount} spots dans la base de données mais aucun n'est encore approuvé. Ils sont en cours de validation.", 
                                "Compris");
                        });
                    }
                    else if (spotsWithActiveTypes == 0)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            IsEmptyState = true;
                            await DialogService.ShowAlertAsync(
                                "Problème de configuration", 
                                "Des spots existent mais il y a un problème avec les types de spots. L'équipe technique a été notifiée.", 
                                "Compris");
                        });
                    }
                    else
                    {
                        // Some other filtering issue
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            IsEmptyState = true;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] HandleEmptySpotState failed: {ex.Message}");
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsEmptyState = true;
                });
            }
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
                    return;
                }

                // ✅ Utilisation du service Supabase API
                var supabaseSpotTypes = await _supabaseApiService.GetSpotTypesAsync();
                var spotTypes = supabaseSpotTypes?.Select(ConvertToDomainSpotType).Where(s => s != null).ToList() ?? new List<SpotType>();
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    RefreshSpotTypesList(spotTypes);
                    _lastSpotTypesLoad = DateTime.UtcNow;
                });
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotTypesOptimized failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Optimized background loading with proper cancellation support
        /// </summary>
        private async Task LoadSpotsInBackgroundAsync()
        {
            try
            {
                await Task.Delay(1000, CancellationToken.None).ConfigureAwait(false); // Allow UI to render first
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await LoadSpotsOptimized().ConfigureAwait(false);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Background spots loading failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Version optimisée du chargement des Spots avec traitement par batch
        /// </summary>
        private async Task LoadSpotsOptimized()
        {
            try
            {
                
                // Reset error states
                IsEmptyState = false;
                IsNetworkError = false;
                
                // ✅ CORRECTION RACE CONDITION: Ensure SpotTypes are loaded before converting spots
                if (SpotTypes?.Count == 0)
                {
                    await LoadSpotTypesOptimized();
                }
                
                
                // Use optimized method for better performance with ConfigureAwait
                // ✅ Utilisation du service Supabase API
                var supabaseSpots = await _supabaseApiService.GetSpotsAsync().ConfigureAwait(false);
                
                
                // ✅ AMÉLIORATION: Log conversion details to identify filtering issues
                var spotsList = new List<Models.Domain.Spot>();
                int nullConversions = 0;
                
                if (supabaseSpots != null)
                {
                    foreach (var supabaseSpot in supabaseSpots)
                    {
                        var convertedSpot = ConvertToDomainSpot(supabaseSpot);
                        if (convertedSpot != null)
                        {
                            spotsList.Add(convertedSpot);
                        }
                        else
                        {
                            nullConversions++;
                            System.Diagnostics.Debug.WriteLine($"[WARNING] 🚫 Failed to convert spot: {supabaseSpot?.Name ?? "NULL_SPOT"} (TypeId: {supabaseSpot?.TypeId})");
                        }
                    }
                }
                
                
                // Handle empty state
                if (!spotsList.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[WARNING] 📭 No spots found - checking if this is a data issue or filter issue");
                    await HandleEmptySpotState();
                    return;
                }
                
                // Analyser la répartition par catégorie pour debug
                if (spotsList.Any())
                {
                    var categoryDistribution = spotsList
                        .Where(s => s.Type != null)
                        .GroupBy(s => s.Type.Category)
                        .Select(g => new { Category = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .ToList();
                    
                    foreach (var cat in categoryDistribution)
                    {
                    }
                    
                    // Afficher quelques exemples de spots
                    foreach (var spot in spotsList.Take(5))
                    {
                        var typeInfo = spot.Type != null ? $"{spot.Type.Name} ({spot.Type.Category})" : "NO TYPE";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[WARNING] ⚠️ No spots retrieved from repository - this may indicate a data integrity issue!");
                    await HandleEmptySpotState();
                    return;
                }

                // Process spots in batches to maintain UI responsiveness
                await ProcessSpotsInBatches(spotsList);
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ❌ LoadSpotsOptimized failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                
                // Set error state
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsNetworkError = true;
                    IsEmptyState = false;
                });
                
                // Show user-friendly error message
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var retry = await DialogService.ShowConfirmationAsync(
                        "Erreur de chargement", 
                        "Impossible de charger les spots. Cela peut être dû à un problème de réseau ou de base de données. Voulez-vous réessayer?", 
                        "Réessayer", 
                        "Annuler");
                        
                    if (retry)
                    {
                        // Retry loading
                        await LoadSpotsOptimized();
                    }
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
                                Label = "", // Empty label to prevent InfoWindow
                                Address = "", // Empty address to prevent InfoWindow
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

        /// <summary>
        /// Convert SupabaseSpot to Domain Spot
        /// </summary>
        private Spot? ConvertToDomainSpot(SupabaseSpot supabaseSpot)
        {
            if (supabaseSpot == null) return null;

            try
            {
                // ✅ CORRECTION CRITIQUE: Lookup the spot type by TypeId
                SpotType? spotType = null;
                if (SpotTypes?.Any() == true)
                {
                    spotType = SpotTypes.FirstOrDefault(t => t.Id == supabaseSpot.TypeId);
                }
                
                // ✅ AMÉLIORATION: Don't fail conversion if SpotType not found, just log it
                var spotTypeName = spotType?.Name ?? "MISSING_TYPE";
                var spotTypeStatus = spotType != null ? "FOUND" : "MISSING";
                
                
                if (supabaseSpot.Name == "AquaTech Diving Store")
                {
                    System.Diagnostics.Debug.WriteLine($"[SPECIAL] MapViewModel: AquaTech Diving Store RAW data - Lat: {supabaseSpot.Latitude}, Lon: {supabaseSpot.Longitude}");
                }
                
                // ✅ AMÉLIORATION: Log when SpotType is missing but continue with conversion
                if (spotType == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[WARNING] 🔍 SpotType not found for '{supabaseSpot.Name}' (TypeId: {supabaseSpot.TypeId}) - Available Types: {SpotTypes?.Count ?? 0}");
                }

                return new Spot
                {
                    Id = supabaseSpot.Id,
                    Name = supabaseSpot.Name ?? string.Empty,
                    Description = supabaseSpot.Description ?? string.Empty,
                    Latitude = supabaseSpot.Latitude,
                    Longitude = supabaseSpot.Longitude,
                    CreatedAt = supabaseSpot.CreatedAt,
                    ValidationStatus = SpotValidationStatus.Approved, // Default for now
                    CreatorId = supabaseSpot.CreatorId,
                    TypeId = supabaseSpot.TypeId,
                    Type = spotType, // 🎯 CORRECTION: Assign the actual SpotType object
                    // Add more missing fields
                    DifficultyLevel = (DifficultyLevel)(supabaseSpot.DifficultyLevel ?? 0),
                    RequiredEquipment = supabaseSpot.RequiredEquipment ?? string.Empty,
                    SafetyNotes = supabaseSpot.SafetyNotes ?? string.Empty,
                    BestConditions = supabaseSpot.BestConditions ?? string.Empty,
                    MaxDepth = (int?)supabaseSpot.MaxDepth,
                    LastSafetyReview = supabaseSpot.LastSafetyReview,
                    SafetyFlags = supabaseSpot.SafetyFlags as Dictionary<string, object>,
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to convert SupabaseSpot to Spot: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert SupabaseSpotType to Domain SpotType
        /// </summary>
        private SpotType? ConvertToDomainSpotType(SupabaseSpotType supabaseSpotType)
        {
            if (supabaseSpotType == null) return null;

            try
            {
                // 🔧 CORRECTION: Parse category from Supabase string
                ActivityCategory category = ActivityCategory.Activity; // Default
                if (!string.IsNullOrEmpty(supabaseSpotType.Category))
                {
                    if (Enum.TryParse<ActivityCategory>(supabaseSpotType.Category, ignoreCase: true, out var parsedCategory))
                    {
                        category = parsedCategory;
                    }
                }
                

                return new SpotType
                {
                    Id = supabaseSpotType.Id,
                    Name = supabaseSpotType.Name ?? string.Empty,
                    Description = supabaseSpotType.Description ?? string.Empty,
                    Category = category, // 🎯 CORRECTION: Use parsed category
                    CreatedAt = supabaseSpotType.CreatedAt,
                    UpdatedAt = supabaseSpotType.UpdatedAt,
                    IsActive = supabaseSpotType.IsActive == true,
                    IconPath = supabaseSpotType.IconPath ?? string.Empty,
                    ColorCode = supabaseSpotType.ColorCode ?? string.Empty,
                    RequiresExpertValidation = supabaseSpotType.RequiresExpertValidation,
                    ValidationCriteria = supabaseSpotType.ValidationCriteria as Dictionary<string, object>
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to convert SupabaseSpotType to SpotType: {ex.Message}");
                return null;
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _searchCancellationToken?.Cancel();
                _searchCancellationToken?.Dispose();
                
                // Unsubscribe from authentication state changes
                try
                {
                    // 🚫 StateChanged event temporairement désactivé
                    // _authenticationService.StateChanged -= OnAuthenticationStateChanged;
                    System.Diagnostics.Debug.WriteLine("[MapViewModel] Disposed and unsubscribed from authentication events");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MapViewModel] Error during authentication event disposal: {ex.Message}");
                }
                
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