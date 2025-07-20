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
using SubExplore.ViewModels.Profile;
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
            IAuthenticationService authenticationService)
            : base(dialogService, navigationService)
        {
            _spotRepository = spotRepository;
            _locationService = locationService;
            _spotTypeRepository = spotTypeRepository;
            _databaseService = databaseService;
            _userRepository = userRepository;
            _settingsService = settingsService;
            _authenticationService = authenticationService;
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
                
                // Initialize database and seed data
                System.Diagnostics.Debug.WriteLine("[DEBUG] Initializing database");
                var databaseCreated = await _databaseService.EnsureDatabaseCreatedAsync();
                if (databaseCreated)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Database created successfully");
                    
                    // Seed database with initial data
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Seeding database");
                    var dataSeeded = await _databaseService.SeedDatabaseAsync();
                    if (dataSeeded)
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] Database seeded successfully");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[WARNING] Database seeding failed or was not needed");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] Database initialization failed");
                }
                
                // Récupération des types de spots pour les filtres
                System.Diagnostics.Debug.WriteLine("[DEBUG] Loading spot types");
                await LoadSpotTypesCommand.ExecuteAsync(null);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Loaded {SpotTypes?.Count ?? 0} spot types");

                // Load user information for menu
                System.Diagnostics.Debug.WriteLine("[DEBUG] Loading user for menu");
                await LoadCurrentUser();

                // Check if location services are available (without requesting permission)
                System.Diagnostics.Debug.WriteLine("[DEBUG] Checking location service availability");
                IsLocationAvailable = await _locationService.IsLocationServiceEnabledAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Location service available: {IsLocationAvailable}");

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
                UpdateEmptyState();

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
                UpdateEmptyState();
            }
        }

        [RelayCommand]
        private async Task SpotSelected(Models.Domain.Spot spot)
        {
            if (spot == null) return;

            // Pour naviguer vers les détails du spot
            await NavigationService.NavigateToAsync<ViewModels.Spots.SpotDetailsViewModel>(spot.Id).ConfigureAwait(false);
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
                await NavigationService.NavigateToAsync<ViewModels.Spots.AddSpotViewModel>(locationParameter).ConfigureAwait(false);
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
                    await SearchSpots().ConfigureAwait(false);
                }
                else if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadSpots().ConfigureAwait(false);
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
                UpdateEmptyState();

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
                UpdateEmptyState();
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
                        
                        await NavigationService.NavigateToAsync<ViewModels.Spots.AddSpotViewModel>(locationParameter).ConfigureAwait(false);
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
            // TODO: Implement MySpots page
            await DialogService.ShowToastAsync("Fonction à venir");
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
        private async Task NavigateToSettings()
        {
            try
            {
                await NavigationService.NavigateToAsync<ViewModels.Settings.DatabaseTestViewModel>();
                IsMenuOpen = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] NavigateToSettings failed: {ex.Message}");
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'accéder aux paramètres", "OK");
                IsMenuOpen = false;
            }
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
                        Title = "Base de données",
                        Icon = "🗄️",
                        Description = "Import des spots et configuration",
                        Command = NavigateToSettingsCommand,
                        IsEnabled = true
                    },
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