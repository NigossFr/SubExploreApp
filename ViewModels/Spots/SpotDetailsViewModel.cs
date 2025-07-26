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
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _spotRepository = spotRepository;
            _spotMediaRepository = spotMediaRepository;
            _userRepository = userRepository;  
            _spotService = spotService;
            _spotCacheService = spotCacheService;

            SpotMedias = new ObservableCollection<SpotMedia>();
            SimilarSpots = new List<Models.Domain.Spot>();
            Title = "Détails du spot";
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            if (parameter is int spotId)
            {
                SpotId = spotId;
            }

            await LoadSpotAsync();
        }

        private async Task LoadSpotAsync()
        {
            if (SpotId <= 0)
                return;

            // Add null safety guards
            if (_spotService == null)
            {
                System.Diagnostics.Debug.WriteLine("[ERROR] LoadSpotAsync: _spotService is null");
                return;
            }

            IsLoading = true;
            IsError = false;
            ErrorMessage = string.Empty;

            try
            {
                // Try to get spot from cache first
                Spot = await _spotCacheService.GetSpotAsync(SpotId);
                
                if (Spot == null)
                {
                    // Load spot from service if not in cache
                    Spot = await _spotService.GetSpotWithFullDetailsAsync(SpotId);
                    
                    if (Spot != null)
                    {
                        // Cache the loaded spot
                        await _spotCacheService.SetSpotAsync(Spot);
                    }
                }

                if (Spot == null)
                {
                    IsError = true;
                    ErrorMessage = "Le spot demandé n'existe pas ou a été supprimé.";
                    return;
                }

                Title = Spot.Name;

                // Initialize media collections with lazy loading
                await InitializeMediaCollectionAsync();

                // Load creator name
                if (Spot.CreatorId > 0 && _userRepository != null)
                {
                    var creator = await _userRepository.GetByIdAsync(Spot.CreatorId);
                    CreatorName = creator?.Username ?? "Utilisateur inconnu";
                }
                else
                {
                    CreatorName = "Utilisateur inconnu";
                }

                // Load enhanced spot data
                await LoadEnhancedSpotDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotAsync failed: {ex.Message}");
                IsError = true;
                ErrorMessage = "Une erreur est survenue lors du chargement du spot. Vérifiez votre connexion internet et réessayez.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadEnhancedSpotDataAsync()
        {
            try
            {
                // Add null safety guard for enhanced data loading
                if (_spotService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[WARNING] LoadEnhancedSpotDataAsync: _spotService is null, skipping enhanced data");
                    return;
                }

                // Load statistics
                SpotStatistics = await _spotService.GetSpotStatisticsAsync(SpotId);

                // Load safety report
                SafetyReport = await _spotService.GenerateSafetyReportAsync(SpotId);

                // Load rating data
                AverageRating = await _spotService.GetSpotAverageRatingAsync(SpotId);
                ReviewCount = await _spotService.GetSpotReviewCountAsync(SpotId);

                // Load similar spots
                SimilarSpots = await _spotService.GetSimilarSpotsAsync(SpotId);

                // Check if spot is favorite (if user is logged in)
                // TODO: Get current user ID from authentication service
                // IsFavorite = await _spotService.IsSpotFavoriteAsync(SpotId, currentUserId);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the main loading
                System.Diagnostics.Debug.WriteLine($"Error loading enhanced spot data: {ex.Message}");
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
                });
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Impossible de partager le spot : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ReportSpot()
        {
            bool confirm = await DialogService.ShowConfirmationAsync(
                "Signalement",
                "Souhaitez-vous signaler ce spot pour un problème ?",
                "Oui",
                "Non");

            if (confirm)
            {
                await DialogService.ShowAlertAsync(
                    "Signalement",
                    "Merci pour votre signalement. Un modérateur va examiner ce spot.",
                    "OK");
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
                await Microsoft.Maui.ApplicationModel.Map.OpenAsync(location, options);

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
            await LoadSpotAsync();
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

        [RelayCommand]
        private async Task ToggleFavorite()
        {
            try
            {
                if (Spot == null) return;

                // TODO: Get current user ID from authentication service
                // var currentUserId = await _authenticationService.GetCurrentUserIdAsync();
                // if (currentUserId.HasValue)
                // {
                //     IsFavorite = await _spotService.ToggleFavoriteSpotAsync(SpotId, currentUserId.Value);
                // }

                await DialogService.ShowAlertAsync("Info", "Fonctionnalité en cours de développement", "OK");
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Impossible de modifier les favoris: {ex.Message}", "OK");
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
                var cachedMedia = await _spotCacheService.GetSpotMediaAsync(SpotId);
                var mediaList = cachedMedia?.ToList();
                
                if (mediaList == null || !mediaList.Any())
                {
                    // Use spot's media or load from repository
                    if (Spot?.Media != null)
                    {
                        mediaList = Spot.Media.ToList();
                        // Cache the media for future use
                        await _spotCacheService.SetSpotMediaAsync(SpotId, mediaList);
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
                    await LoadMediaBatchFromList(mediaList, 0, Math.Min(MediaBatchSize, TotalMediaCount));
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
                    await Task.Delay(50);
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
            var cachedMedia = await _spotCacheService.GetSpotMediaAsync(SpotId);
            var mediaList = cachedMedia?.ToList() ?? Spot?.Media?.ToList();
            
            if (mediaList != null)
            {
                await LoadMediaBatchFromList(mediaList, startIndex, count);
            }
        }
        
        [RelayCommand]
        private async Task LoadMoreMedia()
        {
            if (_allMediaLoaded || IsLoadingMedia) return;
            
            var nextBatchStart = LoadedMediaCount;
            var remainingCount = TotalMediaCount - LoadedMediaCount;
            var batchSize = Math.Min(MediaBatchSize, remainingCount);
            
            await LoadMediaBatchAsync(nextBatchStart, batchSize);
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