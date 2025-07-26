using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Caching;
using SubExplore.ViewModels.Base;

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

        private int _spotId;

        public int SpotId
        {
            get => _spotId;
            set => SetProperty(ref _spotId, value);
        }

        // Query property for Shell navigation
        public string SpotIdString
        {
            get => SpotId.ToString();
            set 
            {
                if (int.TryParse(value, out int spotId))
                {
                    SpotId = spotId;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: SpotIdString set to {value}, parsed SpotId: {SpotId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] SpotDetailsViewModel: Invalid SpotIdString value: {value}");
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
            if (parameter is int spotId)
            {
                SpotId = spotId;
            }

            await LoadSpotAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Loads spot data with comprehensive error handling and caching
        /// </summary>
        private async Task LoadSpotAsync()
        {
            if (SpotId <= 0)
            {
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

                // Initialize media collections with lazy loading
                await InitializeMediaCollectionAsync().ConfigureAwait(false);

                // Load creator information safely
                await LoadCreatorInformationAsync().ConfigureAwait(false);

                // Load enhanced spot data
                await LoadEnhancedSpotDataAsync().ConfigureAwait(false);

                // Load favorite information
                await LoadFavoriteInfoAsync().ConfigureAwait(false);
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
            await NavigationService.GoBackAsync().ConfigureAwait(false);
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

                // Temporary simple toggle for testing UI
                var newFavoriteStatus = !IsFavorite;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: Simulated toggle to status={newFavoriteStatus}");

                IsFavorite = newFavoriteStatus;

                // Simulate favorites count update
                if (newFavoriteStatus)
                {
                    FavoritesCount++;
                }
                else
                {
                    FavoritesCount = Math.Max(0, FavoritesCount - 1);
                }
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: Updated FavoritesCount={FavoritesCount}");

                var message = newFavoriteStatus 
                    ? "Ajouté aux favoris" 
                    : "Retiré des favoris";
                    
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: Showing toast message: {message}");
                await DialogService.ShowToastAsync(message).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFavorite: Toast message shown successfully");

                // TODO: Uncomment this when database is properly set up
                /*
                // Get current user ID from authentication service
                var currentUserId = 1; // Using admin user for testing
                
                var newFavoriteStatus = await _favoriteSpotService.ToggleFavoriteAsync(
                    currentUserId, 
                    SpotId).ConfigureAwait(false);

                IsFavorite = newFavoriteStatus;

                // Update favorites count
                FavoritesCount = await _favoriteSpotService.GetSpotFavoritesCountAsync(SpotId).ConfigureAwait(false);
                */
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
                // Temporary: Set default values for testing
                IsFavorite = false;
                FavoritesCount = new Random().Next(0, 15); // Random count for testing
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadFavoriteInfoAsync: Set IsFavorite={IsFavorite}, FavoritesCount={FavoritesCount}");

                // TODO: Uncomment this when database is properly set up
                /*
                // Get current user ID from authentication service
                var currentUserId = 1; // Using admin user for testing

                IsFavorite = await _favoriteSpotService.IsSpotFavoritedAsync(
                    currentUserId, 
                    SpotId).ConfigureAwait(false);

                FavoritesCount = await _favoriteSpotService.GetSpotFavoritesCountAsync(SpotId).ConfigureAwait(false);
                */
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