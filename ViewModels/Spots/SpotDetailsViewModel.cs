using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Caching;
using SubExplore.ViewModels.Base;
using Microsoft.Extensions.Logging;

// Importations très spécifiques sans ambiguïté
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;


namespace SubExplore.ViewModels.Spots
{
    [QueryProperty(nameof(SpotIdString), "id")]
    public partial class SpotDetailsViewModel : ViewModelBase
    {
        private readonly ISpotRepository _spotRepository;
        private readonly ISpotMediaRepository _spotMediaRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISpotService _spotService;
        private readonly ISpotCacheService _spotCacheService;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly IFavoriteSpotService _favoriteSpotService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IWeatherService _weatherService;
        private readonly IConnectivityService _connectivityService;
        private readonly ILogger<SpotDetailsViewModel> _logger;

        [ObservableProperty]
        private Models.Domain.Spot _spot;

        [ObservableProperty]
        private ObservableCollection<SpotMedia> _spotMedias;

        [ObservableProperty]
        private bool _isLoadingMedia;

        [ObservableProperty]
        private int _totalMediaCount;

        [ObservableProperty]
        private int _loadedMediaCount;

        // Lazy loading properties
        private const int MediaBatchSize = 5;
        private readonly Dictionary<int, SpotMedia> _mediaCache = new();
        private bool _allMediaLoaded = false;

        // Pas de stockage de Position/Location dans le ViewModel
        // Ces valeurs seront calculées à la demande

        [ObservableProperty]
        private string _creatorName;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private SpotStatistics _spotStatistics;

        [ObservableProperty] 
        private SpotSafetyReport _safetyReport;

        [ObservableProperty]
        private double _averageRating;

        [ObservableProperty]
        private int _reviewCount;

        [ObservableProperty]
        private bool _isFavorite;

        [ObservableProperty]
        private IEnumerable<Models.Domain.Spot> _similarSpots;

        [ObservableProperty]
        private bool _hasPhotos;

        [ObservableProperty]
        private bool _hasImageLoadError;


        [ObservableProperty]
        private bool _showToast;

        [ObservableProperty]
        private string _toastMessage = string.Empty;

        [ObservableProperty]
        private string _toastBackgroundColor = "Transparent";

        [ObservableProperty]
        private string _toastBorderColor = "Transparent";

        [ObservableProperty]
        private string _toastTextColor = "Black";

        [ObservableProperty]
        private int _favoritesCount;

        [ObservableProperty]
        private bool _isLoadingFavorite;

        // Weather-related properties
        [ObservableProperty]
        private WeatherInfo? _currentWeather;

        [ObservableProperty]
        private DivingWeatherConditions? _divingConditions;

        [ObservableProperty]
        private bool _isLoadingWeather;

        [ObservableProperty]
        private bool _hasWeatherData;

        [ObservableProperty]
        private string _weatherErrorMessage = string.Empty;

        [ObservableProperty]
        private bool _showWeatherError;

        // Enhanced loading indicators for better UX
        [ObservableProperty]
        private bool _isLoadingCreator;

        [ObservableProperty]
        private bool _isLoadingStatistics;

        [ObservableProperty]
        private bool _isLoadingSimilarSpots;

        [ObservableProperty]
        private string _loadingProgress = string.Empty;

        [ObservableProperty]
        private int _loadingPercentage;

        private int _spotId;

        public int SpotId
        {
            get => _spotId;
            set => SetProperty(ref _spotId, value);
        }

        private bool _isInitialized = false;
        
        // Query property for Shell navigation
        public string SpotIdString
        {
            get => SpotId.ToString();
            set 
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: SpotIdString setter called with value: '{value}' at {DateTime.Now:HH:mm:ss.fff}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: Current thread: {Thread.CurrentThread.ManagedThreadId}");
                
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int spotId))
                {
                    var oldSpotId = SpotId;
                    SpotId = spotId;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: SpotIdString set to '{value}', SpotId changed from {oldSpotId} to {SpotId}");
                    
                    // If we haven't been initialized yet and we now have a valid SpotId, initialize automatically
                    if (!_isInitialized && SpotId > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: QueryProperty set before initialization, triggering automatic initialization");
                        Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(50); // Small delay to ensure UI is ready
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: Starting delayed initialization with SpotId {SpotId}");
                                await InitializeAsync().ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[ERROR] SpotDetailsViewModel: Delayed initialization failed: {ex.Message}");
                            }
                        });
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] SpotDetailsViewModel: Invalid SpotIdString value: '{value}'");
                }
            }
        }

        // Propriétés lisibles pour la vue mais pas liées directement à MapPosition
        public double? DisplayLatitude => Spot != null ? (double?)Spot.Latitude : null;
        public double? DisplayLongitude => Spot != null ? (double?)Spot.Longitude : null;

        public SpotDetailsViewModel(
            ISpotRepository spotRepository,
            ISpotMediaRepository spotMediaRepository,
            IUserRepository userRepository,
            ISpotService spotService,
            ISpotCacheService spotCacheService,
            IErrorHandlingService errorHandlingService,
            IFavoriteSpotService favoriteSpotService,
            IAuthenticationService authenticationService,
            IWeatherService weatherService,
            IConnectivityService connectivityService,
            ILogger<SpotDetailsViewModel> logger,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _spotRepository = spotRepository ?? throw new ArgumentNullException(nameof(spotRepository));
            _spotMediaRepository = spotMediaRepository ?? throw new ArgumentNullException(nameof(spotMediaRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));  
            _spotService = spotService ?? throw new ArgumentNullException(nameof(spotService));
            _spotCacheService = spotCacheService ?? throw new ArgumentNullException(nameof(spotCacheService));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _favoriteSpotService = favoriteSpotService ?? throw new ArgumentNullException(nameof(favoriteSpotService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
            _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SpotMedias = new ObservableCollection<SpotMedia>();
            SimilarSpots = new List<Models.Domain.Spot>();
            Title = "Détails du spot";
            
            // Initialize favorite-related properties with default values
            IsFavorite = false;
            IsLoadingFavorite = false;
            FavoritesCount = 0;
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel.InitializeAsync called with parameter: {parameter} (type: {parameter?.GetType()}), Current SpotId: {SpotId}, IsInitialized: {_isInitialized}");
            
            // Prevent double initialization
            if (_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: Already initialized, skipping");
                return;
            }
            
            try
            {
                // Priority: parameter > QueryProperty
                if (parameter is int spotId)
                {
                    SpotId = spotId;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: SpotId set to {SpotId} from int parameter");
                }
                else if (parameter is string spotIdString && int.TryParse(spotIdString, out int parsedSpotId))
                {
                    SpotId = parsedSpotId;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: SpotId set to {SpotId} from string parameter '{spotIdString}'");
                }
                else if (SpotId <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: No valid parameter, SpotId not set via QueryProperty either. SpotId: {SpotId}");
                    System.Diagnostics.Debug.WriteLine($"[WARNING] SpotDetailsViewModel: Cannot proceed with invalid SpotId. This is likely a QueryProperty timing issue.");
                    return; // Don't proceed if we don't have a valid SpotId
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: Using SpotId from QueryProperty: {SpotId}");
                }

                _isInitialized = true; // Mark as initialized before starting to prevent double calls
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: Marked as initialized, about to call LoadSpotAsync with SpotId: {SpotId}");
                await LoadSpotAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: LoadSpotAsync completed successfully");
            }
            catch (Exception ex)
            {
                _isInitialized = false; // Reset on error so initialization can be retried
                System.Diagnostics.Debug.WriteLine($"[ERROR] SpotDetailsViewModel.InitializeAsync failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
                }
                throw; // Re-throw to ensure the error bubbles up
            }
        }

        /// <summary>
        /// Loads spot data with comprehensive error handling and caching
        /// Optimized with parallel loading for better performance
        /// </summary>
        private async Task LoadSpotAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadSpotAsync called with SpotId: {SpotId}");
            
            if (SpotId <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotAsync: Invalid SpotId {SpotId}");
                await _errorHandlingService.HandleValidationErrorAsync(
                    "ID de spot invalide", 
                    nameof(LoadSpotAsync));
                return;
            }

            IsLoading = true;
            IsError = false;
            ErrorMessage = string.Empty;

            try
            {
                // Try to get spot from cache first
                Spot = await _spotCacheService.GetSpotAsync(SpotId).ConfigureAwait(false);
                
                if (Spot == null)
                {
                    // Load spot from service if not in cache
                    Spot = await _spotService.GetSpotWithFullDetailsAsync(SpotId).ConfigureAwait(false);
                    
                    if (Spot != null)
                    {
                        // Cache the loaded spot
                        await _spotCacheService.SetSpotAsync(Spot).ConfigureAwait(false);
                    }
                }

                if (Spot == null)
                {
                    IsError = true;
                    ErrorMessage = "Le spot demandé n'existe pas ou a été supprimé.";
                    await _errorHandlingService.LogExceptionAsync(
                        new InvalidOperationException($"Spot with ID {SpotId} not found"), 
                        nameof(LoadSpotAsync));
                    return;
                }

                Title = Spot.Name;

                // Initialize progress tracking
                LoadingProgress = "Chargement des données...";
                LoadingPercentage = 20; // Spot loaded (20%)

                // Load all secondary data in SEQUENTIAL order to avoid DbContext concurrency issues
                // Note: Changed from parallel to sequential to prevent "A second operation was started on this context instance" errors
                await TrackProgress(InitializeMediaCollectionAsync(), "médias", 30);
                await TrackProgress(LoadCreatorInformationAsync(), "créateur", 50);
                await TrackProgress(LoadEnhancedSpotDataAsync(), "statistiques", 70);
                await TrackProgress(LoadFavoriteInfoAsync(), "favoris", 85);

                // Only load weather if connectivity allows
                if (_connectivityService.IsConnected)
                {
                    await TrackProgress(LoadWeatherInfoAsync(), "météo", 95);
                }
                else
                {
                    // Set weather unavailable immediately
                    HasWeatherData = false;
                    WeatherErrorMessage = "Connectivité limitée - données météo indisponibles";
                    ShowWeatherError = true;
                    System.Diagnostics.Debug.WriteLine("[DEBUG] LoadSpotAsync: Skipping weather due to no connectivity");
                    LoadingPercentage = 95;
                }
                
                // Complete loading
                LoadingProgress = "Chargement terminé";
                LoadingPercentage = 100;
            }
            catch (TimeoutException ex)
            {
                await HandleLoadingErrorAsync(ex, "L'opération a pris trop de temps. Veuillez réessayer.");
            }
            catch (HttpRequestException ex)
            {
                await _errorHandlingService.HandleNetworkErrorAsync(ex, "LoadSpot");
                await HandleLoadingErrorAsync(ex, "Problème de connexion réseau. Vérifiez votre connexion et réessayez.");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("database") || ex.Message.Contains("connection"))
            {
                await _errorHandlingService.HandleDatabaseErrorAsync(ex, "LoadSpot");
                await HandleLoadingErrorAsync(ex, "Erreur de base de données. Veuillez réessayer plus tard.");
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, nameof(LoadSpotAsync), showToUser: false);
                await HandleLoadingErrorAsync(ex, "Une erreur inattendue s'est produite. Veuillez réessayer.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Tracks progress of a task with visual feedback
        /// </summary>
        private async Task TrackProgress(Task task, string taskName, int progressPercentage)
        {
            try
            {
                LoadingProgress = $"Chargement {taskName}...";
                await task.ConfigureAwait(false);
                LoadingPercentage = Math.Max(LoadingPercentage, progressPercentage);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] TrackProgress: Completed {taskName} ({progressPercentage}%)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] TrackProgress: {taskName} failed - {ex.Message}");
                // Continue with other tasks even if one fails
                LoadingPercentage = Math.Max(LoadingPercentage, progressPercentage);
            }
        }

        /// <summary>
        /// Loads creator information with error handling
        /// </summary>
        private async Task LoadCreatorInformationAsync()
        {
            try
            {
                if (Spot.CreatorId > 0)
                {
                    var creator = await _userRepository.GetByIdAsync(Spot.CreatorId).ConfigureAwait(false);
                    CreatorName = creator?.Username ?? "Utilisateur inconnu";
                }
                else
                {
                    CreatorName = "Utilisateur inconnu";
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the main loading
                await _errorHandlingService.LogExceptionAsync(ex, nameof(LoadCreatorInformationAsync));
                CreatorName = "Utilisateur inconnu";
            }
        }

        /// <summary>
        /// Handles loading errors with consistent error state management
        /// </summary>
        private async Task HandleLoadingErrorAsync(Exception ex, string userMessage)
        {
            IsError = true;
            ErrorMessage = userMessage;
            
            // Clear any partially loaded data
            Spot = null;
            SpotMedias?.Clear();
            
            await Task.CompletedTask; // For consistency with async pattern
        }

        /// <summary>
        /// Loads enhanced spot data (statistics, safety report, similar spots) with individual error handling
        /// </summary>
        private async Task LoadEnhancedSpotDataAsync()
        {
            // Load each enhanced data component individually to prevent total failure
            await LoadSpotStatisticsAsync().ConfigureAwait(false);
            await LoadSafetyReportAsync().ConfigureAwait(false);
            await LoadRatingDataAsync().ConfigureAwait(false);
            await LoadSimilarSpotsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Loads spot statistics with individual error handling
        /// </summary>
        private async Task LoadSpotStatisticsAsync()
        {
            try
            {
                SpotStatistics = await _spotService.GetSpotStatisticsAsync(SpotId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(LoadSpotStatisticsAsync));
                SpotStatistics = null; // Set to null to indicate unavailable data
            }
        }

        /// <summary>
        /// Loads safety report with individual error handling
        /// </summary>
        private async Task LoadSafetyReportAsync()
        {
            try
            {
                SafetyReport = await _spotService.GenerateSafetyReportAsync(SpotId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(LoadSafetyReportAsync));
                SafetyReport = null; // Set to null to indicate unavailable data
            }
        }

        /// <summary>
        /// Loads rating data with individual error handling
        /// </summary>
        private async Task LoadRatingDataAsync()
        {
            try
            {
                AverageRating = await _spotService.GetSpotAverageRatingAsync(SpotId).ConfigureAwait(false);
                ReviewCount = await _spotService.GetSpotReviewCountAsync(SpotId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(LoadRatingDataAsync));
                AverageRating = 0; // Set default values
                ReviewCount = 0;
            }
        }

        /// <summary>
        /// Loads similar spots with individual error handling
        /// </summary>
        private async Task LoadSimilarSpotsAsync()
        {
            try
            {
                SimilarSpots = await _spotService.GetSimilarSpotsAsync(SpotId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(LoadSimilarSpotsAsync));
                SimilarSpots = new List<Models.Domain.Spot>(); // Set empty list
            }
        }

        // Méthode utilitaire pour créer un Pin
        // À utiliser UNIQUEMENT depuis le code-behind
        public Pin CreatePin()
        {
            if (Spot == null) return null;

            try
            {
                // Conversion explicite de decimal à double
                double lat = (double)Spot.Latitude;
                double lon = (double)Spot.Longitude;

                // Utilisation explicite du bon type pour la propriété Location
                return new Pin
                {
                    Label = Spot.Name,
                    Address = $"Profondeur: {Spot.MaxDepth?.ToString() ?? "N/A"}m, Difficulté: {Spot.DifficultyLevel}",
                    Type = PinType.Place,
                    Location = new Location(lat, lon) // Type Location explicite
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la création du pin: {ex.Message}");
                return null;
            }
        }

        // Méthode utilitaire pour créer un MapSpan
        // À utiliser UNIQUEMENT depuis le code-behind
        public Microsoft.Maui.Maps.MapSpan GetMapSpan(double radiusKm = 1.0)
        {
            if (Spot == null) return null;

            try
            {
                double lat = (double)Spot.Latitude;
                double lon = (double)Spot.Longitude;

                // Utilisation très explicite de Distance et MapSpan
                var center = new Location(lat, lon);
                var distance = Microsoft.Maui.Maps.Distance.FromKilometers(radiusKm);
                return Microsoft.Maui.Maps.MapSpan.FromCenterAndRadius(center, distance);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la création du MapSpan: {ex.Message}");
                return null;
            }
        }

        // Getter pour les coordonnées sans conversion en objet Position
        public (double Latitude, double Longitude)? GetCoordinates()
        {
            if (Spot == null) return null;

            try
            {
                double lat = (double)Spot.Latitude;
                double lon = (double)Spot.Longitude;
                return (lat, lon);
            }
            catch
            {
                return null;
            }
        }

        [RelayCommand]
        private async Task ShareSpot()
        {
            if (Spot == null)
            {
                await _errorHandlingService.HandleValidationErrorAsync(
                    "Aucun spot à partager", 
                    nameof(ShareSpot));
                return;
            }

            try
            {
                string message = $"Découvre ce spot de {Spot.Type?.Name ?? "plongée"} : {Spot.Name}\n";
                message += $"Profondeur : {Spot.MaxDepth}m, Difficulté : {Spot.DifficultyLevel}\n";
                message += $"Localisation : {Spot.Latitude}, {Spot.Longitude}\n";
                message += "Partagé depuis l'application SubExplore";

                await Share.RequestAsync(new ShareTextRequest
                {
                    Title = $"Partager le spot {Spot.Name}",
                    Text = message
                }).ConfigureAwait(false);
            }
            catch (NotSupportedException ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, nameof(ShareSpot), 
                    userMessage: "Le partage n'est pas supporté sur cet appareil.");
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, nameof(ShareSpot), 
                    userMessage: "Impossible de partager le spot. Veuillez réessayer.");
            }
        }

        [RelayCommand]
        private async Task ReportSpot()
        {
            if (Spot == null)
            {
                await _errorHandlingService.HandleValidationErrorAsync(
                    "Aucun spot à signaler", 
                    nameof(ReportSpot));
                return;
            }

            try
            {
                bool confirm = await DialogService.ShowConfirmationAsync(
                    "Signalement",
                    "Souhaitez-vous signaler ce spot pour un problème ?",
                    "Oui",
                    "Non").ConfigureAwait(false);

                if (confirm)
                {
                    await DialogService.ShowAlertAsync(
                        "Signalement",
                        "Merci pour votre signalement. Un modérateur va examiner ce spot.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, nameof(ReportSpot),
                    userMessage: "Impossible de signaler le spot. Veuillez réessayer.");
            }
        }

        [RelayCommand]
        private async Task OpenInExternalMap()
        {
            if (Spot == null) return;
            try
            {
                double lat = (double)Spot.Latitude;
                double lon = (double)Spot.Longitude;

                // Location est de Microsoft.Maui.Devices.Sensors
                var location = new Location(lat, lon);
                var options = new MapLaunchOptions // MapLaunchOptions est dans Microsoft.Maui.ApplicationModel si MAUI < 8, sinon Microsoft.Maui.Devices.Sensors
                {
                    Name = Spot.Name,
                    NavigationMode = NavigationMode.None // NavigationMode est dans Microsoft.Maui.ApplicationModel
                };
                // Map.OpenAsync est dans Microsoft.Maui.Maps
                await Microsoft.Maui.ApplicationModel.Map.OpenAsync(location, options).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Impossible d'ouvrir la carte: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task Back()
        {
            await NavigationService.GoBackAsync();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadSpotAsync().ConfigureAwait(false);
        }

        private void DisplayToast(string message, bool isError = false)
        {
            ToastMessage = message;
            ShowToast = true;
            
            if (isError)
            {
                ToastBackgroundColor = "#FFEBEE";
                ToastBorderColor = "#F44336";
                ToastTextColor = "#D32F2F";
            }
            else
            {
                ToastBackgroundColor = "#E8F5E8";
                ToastBorderColor = "#4CAF50";
                ToastTextColor = "#2E7D32";
            }
            
            // Auto-hide after 3 seconds
            Task.Delay(3000).ContinueWith(_ => 
            {
                ShowToast = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Toggle favorite status for the current spot
        /// </summary>
        [RelayCommand]
        private async Task ToggleFavorite()
        {
            if (Spot == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] ToggleFavorite: Spot is null");
                return;
            }

            try
            {
                IsLoadingFavorite = true;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: Starting toggle for SpotId={SpotId}, CurrentState={IsFavorite}");

                var message = IsFavorite 
                    ? "Retiré des favoris" 
                    : "Ajouté aux favoris";
                    
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: Showing toast message: {message}");
                await DialogService.ShowToastAsync(message).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: Toast message shown successfully");

                // Get current user ID from authentication service
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: AuthenticationService.IsAuthenticated = {_authenticationService.IsAuthenticated}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: AuthenticationService.CurrentUser = {_authenticationService.CurrentUser?.Id.ToString() ?? "NULL"}");
                
                var currentUserId = _authenticationService.CurrentUserId;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: AuthenticationService.CurrentUserId = {currentUserId?.ToString() ?? "NULL"}");
                
                if (!currentUserId.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] ToggleFavorite: User not authenticated - attempting validation");
                    
                    // Try to validate authentication
                    var isAuthenticated = await _authenticationService.ValidateAuthenticationAsync();
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: ValidateAuthenticationAsync result = {isAuthenticated}");
                    
                    if (isAuthenticated && _authenticationService.CurrentUserId.HasValue)
                    {
                        currentUserId = _authenticationService.CurrentUserId;
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: After validation, CurrentUserId = {currentUserId}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[ERROR] ToggleFavorite: Still not authenticated after validation");
                        await DialogService.ShowAlertAsync("Erreur", "Vous devez être connecté pour utiliser les favoris", "OK");
                        return;
                    }
                }
                
                // Enhanced error handling and diagnostics
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: Calling FavoriteSpotService.ToggleFavoriteAsync(userId={currentUserId}, spotId={SpotId})");
                
                if (_favoriteSpotService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] ToggleFavorite: FavoriteSpotService is NULL!");
                    await DialogService.ShowAlertAsync("Erreur", "Service des favoris non disponible", "OK");
                    return;
                }
                
                var newFavoriteStatus = await _favoriteSpotService.ToggleFavoriteAsync(
                    currentUserId.Value, 
                    SpotId).ConfigureAwait(false);

                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: Service returned newFavoriteStatus={newFavoriteStatus}");
                IsFavorite = newFavoriteStatus;

                // Update favorites count
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: Getting favorites count for spot {SpotId}");
                FavoritesCount = await _favoriteSpotService.GetSpotFavoritesCountAsync(SpotId).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: Favorites count updated to {FavoritesCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ToggleFavorite: Exception occurred: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] ToggleFavorite: Stack trace: {ex.StackTrace}");
                await DialogService.ShowAlertAsync("Erreur", 
                    "Impossible de modifier les favoris. Veuillez réessayer.", "OK");
            }
            finally
            {
                IsLoadingFavorite = false;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: Finished. Final state IsFavorite={IsFavorite}");
            }
        }

        /// <summary>
        /// Load favorite information for the current spot
        /// </summary>
        private async Task LoadFavoriteInfoAsync()
        {
            try
            {
                // Get current user ID from authentication service
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadFavoriteInfoAsync: AuthenticationService.IsAuthenticated = {_authenticationService.IsAuthenticated}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadFavoriteInfoAsync: AuthenticationService.CurrentUser = {_authenticationService.CurrentUser?.Id.ToString() ?? "NULL"}");
                
                var currentUserId = _authenticationService.CurrentUserId;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadFavoriteInfoAsync: AuthenticationService.CurrentUserId = {currentUserId?.ToString() ?? "NULL"}");
                
                if (!currentUserId.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] LoadFavoriteInfoAsync: User not authenticated - attempting validation");
                    
                    // Try to validate authentication
                    var isAuthenticated = await _authenticationService.ValidateAuthenticationAsync();
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadFavoriteInfoAsync: ValidateAuthenticationAsync result = {isAuthenticated}");
                    
                    if (isAuthenticated && _authenticationService.CurrentUserId.HasValue)
                    {
                        currentUserId = _authenticationService.CurrentUserId;
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadFavoriteInfoAsync: After validation, CurrentUserId = {currentUserId}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] LoadFavoriteInfoAsync: Still not authenticated after validation, setting defaults");
                        IsFavorite = false;
                        FavoritesCount = 0;
                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadFavoriteInfoAsync: Loading favorite info for userId={currentUserId}, spotId={SpotId}");
                
                if (_favoriteSpotService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] LoadFavoriteInfoAsync: FavoriteSpotService is NULL!");
                    IsFavorite = false;
                    FavoritesCount = 0;
                    return;
                }

                IsFavorite = await _favoriteSpotService.IsSpotFavoritedAsync(
                    currentUserId.Value, 
                    SpotId).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadFavoriteInfoAsync: IsFavorite={IsFavorite}");

                FavoritesCount = await _favoriteSpotService.GetSpotFavoritesCountAsync(SpotId).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadFavoriteInfoAsync: FavoritesCount={FavoritesCount}");
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(LoadFavoriteInfoAsync));
                IsFavorite = false;
                FavoritesCount = 0;
            }
        }

        [RelayCommand]
        private async Task ViewSimilarSpots()
        {
            try
            {
                if (SimilarSpots?.Any() == true)
                {
                    // TODO: Navigate to similar spots view
                    await DialogService.ShowAlertAsync("Info", $"Spots similaires trouvés: {SimilarSpots.Count()}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Erreur lors de la navigation: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task RecordVisit()
        {
            try
            {
                if (Spot == null) return;

                // TODO: Get current user ID from authentication service
                // var currentUserId = await _authenticationService.GetCurrentUserIdAsync();
                // if (currentUserId.HasValue)
                // {
                //     await _spotService.RecordSpotVisitAsync(SpotId, currentUserId.Value);
                //     await DialogService.ShowToastAsync("Visite enregistrée!");
                //     
                //     // Refresh statistics
                //     SpotStatistics = await _spotService.GetSpotStatisticsAsync(SpotId);
                // }

                await DialogService.ShowAlertAsync("Info", "Visite enregistrée (fonctionnalité en développement)", "OK");
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Impossible d'enregistrer la visite: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ViewSafetyReport()
        {
            try
            {
                if (SafetyReport == null) return;

                var message = $"Score de sécurité: {SafetyReport.SafetyScore}/100\n\n";
                
                if (SafetyReport.SafetyWarnings.Any())
                {
                    message += "⚠️ Avertissements:\n";
                    message += string.Join("\n", SafetyReport.SafetyWarnings.Select(w => $"• {w}"));
                    message += "\n\n";
                }

                if (SafetyReport.RequiredEquipment.Any())
                {
                    message += "🎯 Équipement requis:\n";
                    message += string.Join("\n", SafetyReport.RequiredEquipment.Select(e => $"• {e}"));
                }

                await DialogService.ShowAlertAsync("Rapport de sécurité", message, "OK");
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Impossible d'afficher le rapport: {ex.Message}", "OK");
            }
        }

        // Media lazy loading implementation with caching
        private async Task InitializeMediaCollectionAsync()
        {
            try
            {
                SpotMedias.Clear();
                _mediaCache.Clear();
                _allMediaLoaded = false;
                
                // Try to get media from cache first
                var cachedMedia = await _spotCacheService.GetSpotMediaAsync(SpotId).ConfigureAwait(false);
                var mediaList = cachedMedia?.ToList();
                
                if (mediaList == null || !mediaList.Any())
                {
                    // Use spot's media or load from repository
                    if (Spot?.Media != null)
                    {
                        mediaList = Spot.Media.ToList();
                        // Cache the media for future use
                        await _spotCacheService.SetSpotMediaAsync(SpotId, mediaList).ConfigureAwait(false);
                    }
                    else
                    {
                        mediaList = new List<SpotMedia>();
                    }
                }
                
                TotalMediaCount = mediaList.Count;
                HasPhotos = TotalMediaCount > 0;
                
                if (TotalMediaCount > 0)
                {
                    // Load first batch immediately for initial display
                    await LoadMediaBatchFromList(mediaList, 0, Math.Min(MediaBatchSize, TotalMediaCount)).ConfigureAwait(false);
                }
                
                LoadedMediaCount = SpotMedias.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing media collection: {ex.Message}");
                HasPhotos = false;
                TotalMediaCount = 0;
                LoadedMediaCount = 0;
            }
        }
        
        private async Task LoadMediaBatchFromList(List<SpotMedia> mediaList, int startIndex, int count)
        {
            if (mediaList == null || IsLoadingMedia) return;
            
            try
            {
                IsLoadingMedia = true;
                
                var mediaToLoad = mediaList
                    .Skip(startIndex)
                    .Take(count)
                    .Where(m => !_mediaCache.ContainsKey(m.Id))
                    .ToList();
                
                foreach (var media in mediaToLoad)
                {
                    // Cache the media item
                    _mediaCache[media.Id] = media;
                    
                    // Add to observable collection
                    SpotMedias.Add(media);
                    
                    // Small delay for smooth loading experience
                    await Task.Delay(50).ConfigureAwait(false);
                }
                
                LoadedMediaCount = SpotMedias.Count;
                _allMediaLoaded = LoadedMediaCount >= TotalMediaCount;
                
                // Optimize memory usage if too many items loaded
                await OptimizeMemoryUsageAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading media batch: {ex.Message}");
            }
            finally
            {
                IsLoadingMedia = false;
            }
        }
        
        private async Task LoadMediaBatchAsync(int startIndex, int count)
        {
            // Get cached media list
            var cachedMedia = await _spotCacheService.GetSpotMediaAsync(SpotId).ConfigureAwait(false);
            var mediaList = cachedMedia?.ToList() ?? Spot?.Media?.ToList();
            
            if (mediaList != null)
            {
                await LoadMediaBatchFromList(mediaList, startIndex, count).ConfigureAwait(false);
            }
        }
        
        [RelayCommand]
        private async Task LoadMoreMedia()
        {
            if (_allMediaLoaded || IsLoadingMedia) return;
            
            var nextBatchStart = LoadedMediaCount;
            var remainingCount = TotalMediaCount - LoadedMediaCount;
            var batchSize = Math.Min(MediaBatchSize, remainingCount);
            
            await LoadMediaBatchAsync(nextBatchStart, batchSize).ConfigureAwait(false);
        }
        
        // Optimize memory by disposing unused media from cache
        private void OptimizeMediaCache()
        {
            if (_mediaCache.Count > MediaBatchSize * 2)
            {
                var itemsToRemove = _mediaCache.Keys
                    .Take(_mediaCache.Count - MediaBatchSize)
                    .ToList();
                
                foreach (var key in itemsToRemove)
                {
                    _mediaCache.Remove(key);
                }
                
                // Force garbage collection after cleanup
                System.Diagnostics.Debug.WriteLine($"[DEBUG] OptimizeMediaCache: Removed {itemsToRemove.Count} items from cache");
                GC.Collect(0, GCCollectionMode.Optimized);
            }
        }

        /// <summary>
        /// Enhanced memory management for media loading
        /// Automatically triggered when loading large amounts of media
        /// </summary>
        private async Task OptimizeMemoryUsageAsync()
        {
            try
            {
                // Check if we have too many items in memory
                if (SpotMedias.Count > MediaBatchSize * 3)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] OptimizeMemoryUsageAsync: High media count detected, optimizing...");
                    
                    // Keep only the most recent items visible
                    var itemsToKeep = SpotMedias.Take(MediaBatchSize * 2).ToList();
                    SpotMedias.Clear();
                    
                    foreach (var item in itemsToKeep)
                    {
                        SpotMedias.Add(item);
                    }
                    
                    // Optimize cache
                    OptimizeMediaCache();
                    
                    // Small delay to let UI update
                    await Task.Delay(50).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] OptimizeMemoryUsageAsync: {ex.Message}");
            }
        }

        /// <summary>
        /// Load weather information for the current spot
        /// Enhanced with better connectivity handling and fallbacks
        /// </summary>
        private async Task LoadWeatherInfoAsync()
        {
            if (Spot == null)
            {
                HasWeatherData = false;
                return;
            }

            try
            {
                IsLoadingWeather = true;
                ShowWeatherError = false;
                WeatherErrorMessage = string.Empty;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadWeatherInfoAsync: Loading weather for spot at {Spot.Latitude}, {Spot.Longitude}");

                // Quick connectivity check first
                if (!_connectivityService.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] LoadWeatherInfoAsync: No internet connectivity");
                    HasWeatherData = false;
                    WeatherErrorMessage = "Pas de connexion internet - données météo indisponibles";
                    ShowWeatherError = true;
                    return;
                }

                // Check if weather service is available with timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var isServiceAvailable = await _weatherService.IsServiceAvailableAsync().ConfigureAwait(false);
                
                if (!isServiceAvailable)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] LoadWeatherInfoAsync: Weather service not available");
                    _logger.LogWarning("Weather service is not available");
                    HasWeatherData = false;
                    WeatherErrorMessage = "Service météo temporairement indisponible";
                    ShowWeatherError = true;
                    return;
                }

                // Load current weather and diving conditions in parallel with timeout
                var weatherTask = _weatherService.GetCurrentWeatherAsync(Spot.Latitude, Spot.Longitude, cts.Token);
                var divingConditionsTask = _weatherService.GetDivingConditionsAsync(Spot.Latitude, Spot.Longitude, cts.Token);

                await Task.WhenAll(weatherTask, divingConditionsTask).ConfigureAwait(false);

                CurrentWeather = await weatherTask;
                DivingConditions = await divingConditionsTask;

                HasWeatherData = CurrentWeather != null;

                if (HasWeatherData)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadWeatherInfoAsync: Successfully loaded weather - Temp: {CurrentWeather?.Temperature}°C, Conditions: {CurrentWeather?.Condition}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] LoadWeatherInfoAsync: No weather data received");
                    WeatherErrorMessage = "Données météo temporairement indisponibles";
                    ShowWeatherError = true;
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] LoadWeatherInfoAsync: Operation timed out");
                HasWeatherData = false;
                WeatherErrorMessage = "Délai d'attente dépassé - données météo indisponibles";
                ShowWeatherError = true;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadWeatherInfoAsync: Network error - {ex.Message}");
                await _errorHandlingService.LogExceptionAsync(ex, nameof(LoadWeatherInfoAsync));
                
                HasWeatherData = false;
                WeatherErrorMessage = "Problème de réseau - données météo indisponibles";
                ShowWeatherError = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadWeatherInfoAsync: {ex.Message}");
                await _errorHandlingService.LogExceptionAsync(ex, nameof(LoadWeatherInfoAsync));
                
                HasWeatherData = false;
                WeatherErrorMessage = "Erreur lors du chargement des données météo";
                ShowWeatherError = true;
            }
            finally
            {
                IsLoadingWeather = false;
            }
        }

        /// <summary>
        /// Refresh weather information
        /// </summary>
        [RelayCommand]
        private async Task RefreshWeather()
        {
            await LoadWeatherInfoAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Show detailed weather information
        /// </summary>
        [RelayCommand]
        private async Task ShowWeatherDetails()
        {
            if (CurrentWeather == null)
            {
                await DialogService.ShowAlertAsync("Info", "Aucune donnée météo disponible", "OK");
                return;
            }

            try
            {
                var message = $"🌡️ Température: {CurrentWeather.Temperature:F1}°C (ressenti {CurrentWeather.FeelsLike:F1}°C)\n";
                message += $"💨 Vent: {CurrentWeather.WindSpeed:F1} km/h {CurrentWeather.GetWindDirectionText()}\n";
                message += $"💧 Humidité: {CurrentWeather.Humidity}%\n";
                message += $"👁️ Visibilité: {CurrentWeather.Visibility:F1} km\n";
                message += $"☁️ Couverture nuageuse: {CurrentWeather.CloudCover}%\n";

                if (CurrentWeather.ChanceOfRain > 0)
                {
                    message += $"🌧️ Probabilité de pluie: {CurrentWeather.ChanceOfRain}%\n";
                }

                if (DivingConditions != null)
                {
                    message += $"\n🤿 Conditions de plongée: {DivingConditions.OverallCondition}\n";
                    message += $"⚡ Score de sécurité: {DivingConditions.SafetyScore}/100\n";
                    
                    if (DivingConditions.Warnings.Any())
                    {
                        message += $"\n⚠️ Avertissements:\n{string.Join("\n", DivingConditions.Warnings.Select(w => $"• {w}"))}";
                    }

                    if (DivingConditions.Recommendations.Any())
                    {
                        message += $"\n💡 Recommandations:\n{string.Join("\n", DivingConditions.Recommendations.Select(r => $"• {r}"))}";
                    }
                }

                message += $"\n\n📅 Dernière mise à jour: {CurrentWeather.LastUpdated:HH:mm}";

                await DialogService.ShowAlertAsync("Conditions météo détaillées", message, "OK");
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Impossible d'afficher les détails météo: {ex.Message}", "OK");
            }
        }

        // Back command for header navigation is auto-generated as BackCommand
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clear media cache
                _mediaCache?.Clear();
                
                // Clear collections
                SpotMedias?.Clear();
                
                // Clear references
                Spot = null;
                SimilarSpots = null;
            }
            
            base.Dispose(disposing);
        }
    }
}