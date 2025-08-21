using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.Constants;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Map
{
    /// <summary>
    /// ViewModel responsible for spot data management, filtering, and search operations
    /// Extracted from MapViewModel to improve Single Responsibility Principle
    /// </summary>
    public partial class SpotManagementViewModel : ViewModelBase
    {
        private readonly ISpotRepository _spotRepository;
        private readonly ISpotTypeRepository _spotTypeRepository;
        private readonly ILogger<SpotManagementViewModel> _logger;

        // Caching for performance
        private DateTime _lastSpotTypesLoad = DateTime.MinValue;
        private DateTime _lastSpotsLoad = DateTime.MinValue;

        [ObservableProperty]
        private ObservableCollection<Spot> _spots;

        [ObservableProperty]
        private ObservableCollection<Pin> _pins;

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
        private bool _isEmptyState;

        [ObservableProperty]
        private bool _isNetworkError;

        [ObservableProperty]
        private System.Threading.CancellationTokenSource _searchCancellationToken;

        public SpotManagementViewModel(
            ISpotRepository spotRepository,
            ISpotTypeRepository spotTypeRepository,
            ILogger<SpotManagementViewModel> logger,
            IDialogService dialogService = null,
            INavigationService navigationService = null)
            : base(dialogService, navigationService)
        {
            _spotRepository = spotRepository ?? throw new ArgumentNullException(nameof(spotRepository));
            _spotTypeRepository = spotTypeRepository ?? throw new ArgumentNullException(nameof(spotTypeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Spots = new ObservableCollection<Spot>();
            Pins = new ObservableCollection<Pin>();
            SpotTypes = new ObservableCollection<SpotType>();

            UpdateEmptyState();
            CheckNetworkConnectivity();
        }

        /// <summary>
        /// Initialize spot data and types
        /// </summary>
        public override async Task InitializeAsync(object parameter = null)
        {
            try
            {
                _logger.LogInformation("Initializing spot management");

                await LoadSpotTypesAsync();
                await LoadSpotsAsync();

                _logger.LogInformation("Spot management initialized successfully. Spots: {SpotCount}, Types: {TypeCount}",
                    Spots?.Count ?? 0, SpotTypes?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize spot management");
                await ShowAlertAsync("Erreur", $"Impossible d'initialiser les données : {ex.Message}", "D'accord");
            }
        }

        /// <summary>
        /// Load all approved spots from the repository
        /// </summary>
        [RelayCommand]
        public async Task LoadSpotsAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                _logger.LogDebug("Loading spots from repository");

                var spots = await _spotRepository.GetSpotsByValidationStatusAsync(SpotValidationStatus.Approved);
                
                var spotsCount = spots?.Count() ?? 0;
                _logger.LogInformation("Retrieved {SpotCount} spots from repository", spotsCount);

                if (spotsCount == 0)
                {
                    _logger.LogWarning("No spots found in repository");
                    await ShowToastAsync("Aucun spot trouvé dans la région");
                }
                else
                {
                    _logger.LogDebug("Sample spots loaded: {SampleSpots}",
                        string.Join(", ", spots.Take(3).Select(s => $"{s.Name} ({s.Latitude}, {s.Longitude})")));
                }

                RefreshSpotsList(spots);
                UpdatePins();
                UpdateEmptyState();
                _lastSpotsLoad = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load spots");
                await ShowAlertAsync("Erreur", "Impossible de charger les spots. Veuillez réessayer plus tard.", "D'accord");
            }
            finally
            {
                IsLoading = false;
                UpdateEmptyState();
            }
        }

        /// <summary>
        /// Load available spot types for filtering
        /// </summary>
        [RelayCommand]
        public async Task LoadSpotTypesAsync()
        {
            if (IsLoading) return;

            // Use cache if recent
            if ((DateTime.Now - _lastSpotTypesLoad).TotalMinutes < AppConstants.Map.CACHE_EXPIRY_MINUTES)
            {
                _logger.LogDebug("Using cached spot types");
                return;
            }

            try
            {
                IsLoading = true;
                _logger.LogDebug("Loading spot types from repository");

                var types = await _spotTypeRepository.GetActiveTypesAsync();
                RefreshSpotTypesList(types);
                _lastSpotTypesLoad = DateTime.Now;

                _logger.LogInformation("Loaded {TypeCount} spot types", types?.Count() ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load spot types");
                await ShowAlertAsync("Erreur", "Impossible de charger les types de spots.", "D'accord");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Filter spots by selected type
        /// </summary>
        [RelayCommand]
        public async Task FilterSpotsByTypeAsync(SpotType spotType)
        {
            if (spotType == null || IsLoading) return;

            try
            {
                IsLoading = true;
                IsFiltering = true;
                SelectedSpotType = spotType;

                _logger.LogDebug("Filtering spots by type: {SpotType}", spotType.Name);

                var filteredSpots = await _spotRepository.GetSpotsByTypeAsync(spotType.Id);
                RefreshSpotsList(filteredSpots);
                UpdatePins();
                UpdateEmptyState();

                _logger.LogInformation("Filtered to {SpotCount} spots for type {SpotType}",
                    filteredSpots?.Count() ?? 0, spotType.Name);

                // Auto-zoom if appropriate number of spots
                var spotCount = Spots.Count;
                if (spotCount >= AppConstants.Map.MIN_SPOTS_FOR_AUTO_ZOOM && 
                    spotCount <= AppConstants.Map.MAX_SPOTS_FOR_AUTO_ZOOM)
                {
                    OnRequestCenterMapOnSpots?.Invoke(Spots);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to filter spots by type {SpotType}", spotType?.Name);
                await ShowAlertAsync("Erreur", "Impossible de filtrer les spots. Veuillez réessayer plus tard.", "D'accord");
            }
            finally
            {
                IsLoading = false;
                IsFiltering = false;
                UpdateEmptyState();
            }
        }

        /// <summary>
        /// Search spots by name or description
        /// </summary>
        [RelayCommand]
        public async Task SearchTextChangedAsync()
        {
            // Cancel previous search
            _searchCancellationToken?.Cancel();
            _searchCancellationToken = new System.Threading.CancellationTokenSource();

            // Debounce search - wait before executing
            try
            {
                await Task.Delay(AppConstants.UI.SEARCH_DEBOUNCE_DELAY_MS, _searchCancellationToken.Token);
                
                if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= AppConstants.UI.MIN_SEARCH_LENGTH)
                {
                    await SearchSpotsAsync();
                }
                else if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadSpotsAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Search was cancelled - this is expected
                _logger.LogDebug("Search operation was cancelled");
            }
        }

        /// <summary>
        /// Execute spot search with current search text
        /// </summary>
        [RelayCommand]
        public async Task SearchSpotsAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || IsLoading) return;

            try
            {
                IsLoading = true;
                IsSearching = true;

                _logger.LogDebug("Searching spots for: {SearchText}", SearchText);

                var searchResults = await _spotRepository.SearchSpotsAsync(SearchText);
                RefreshSpotsList(searchResults);
                UpdatePins();
                UpdateEmptyState();

                var resultCount = searchResults?.Count() ?? 0;
                _logger.LogInformation("Search for '{SearchText}' returned {ResultCount} results", SearchText, resultCount);

                // Center map on search results
                if (Spots.Count > 0)
                {
                    OnRequestCenterMapOnSpots?.Invoke(Spots);
                }
                else
                {
                    await ShowToastAsync("Aucun spot trouvé pour cette recherche");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search spots for: {SearchText}", SearchText);
                await ShowAlertAsync("Erreur", "Impossible d'effectuer la recherche. Veuillez réessayer plus tard.", "D'accord");
            }
            finally
            {
                IsLoading = false;
                IsSearching = false;
                UpdateEmptyState();
            }
        }

        /// <summary>
        /// Clear all active filters and search
        /// </summary>
        [RelayCommand]
        public void ClearFilters()
        {
            SelectedSpotType = null;
            SearchText = string.Empty;
            IsFiltering = false;
            IsSearching = false;

            _logger.LogDebug("Cleared all filters and search");

            // Reload all spots
            LoadSpotsCommand.Execute(null);
        }

        /// <summary>
        /// Create map pins from current spots
        /// </summary>
        public void UpdatePins()
        {
            Application.Current?.Dispatcher.Dispatch(() =>
            {
                try
                {
                    _logger.LogDebug("Updating pins for {SpotCount} spots", Spots?.Count ?? 0);

                    if (Spots == null || !Spots.Any())
                    {
                        Pins.Clear();
                        return;
                    }

                    var validPins = Spots
                        .Select(CreatePinFromSpot)
                        .Where(pin => pin != null)
                        .ToList();

                    _logger.LogDebug("Created {ValidPins} valid pins from {TotalSpots} spots",
                        validPins.Count, Spots.Count);

                    // Efficient batch update
                    Pins.Clear();
                    foreach (var pin in validPins)
                    {
                        Pins.Add(pin!);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update pins");
                }
            });
        }

        /// <summary>
        /// Create a map pin from a spot
        /// </summary>
        private Pin CreatePinFromSpot(Spot spot)
        {
            try
            {
                double lat = Convert.ToDouble(spot.Latitude);
                double lon = Convert.ToDouble(spot.Longitude);

                // Validate coordinates
                if (double.IsNaN(lat) || double.IsInfinity(lat) || lat < -90 || lat > 90 ||
                    double.IsNaN(lon) || double.IsInfinity(lon) || lon < -180 || lon > 180)
                {
                    _logger.LogWarning("Invalid coordinates for spot {SpotName}: Lat={Latitude}, Lng={Longitude}",
                        spot.Name, spot.Latitude, spot.Longitude);
                    return null;
                }

                var pin = new Pin
                {
                    Label = spot.Name,
                    Address = $"{spot.Type?.Name ?? "Spot"} - {spot.DifficultyLevel}",
                    Location = new Location(lat, lon),
                    Type = PinType.Place,
                    BindingContext = spot
                };

                return pin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create pin for spot {SpotName}", spot.Name);
                return null;
            }
        }

        /// <summary>
        /// Refresh the spots collection with new data
        /// </summary>
        private void RefreshSpotsList(IEnumerable<Spot> spots)
        {
            Application.Current?.Dispatcher.Dispatch(() =>
            {
                try
                {
                    var spotsList = spots?.ToList() ?? new List<Spot>();
                    Spots.Clear();

                    // Efficient batch processing
                    for (int i = 0; i < spotsList.Count; i += AppConstants.Map.SPOTS_BATCH_SIZE)
                    {
                        var batch = spotsList.Skip(i).Take(AppConstants.Map.SPOTS_BATCH_SIZE);
                        foreach (var spot in batch)
                        {
                            Spots.Add(spot);
                        }
                    }

                    _logger.LogDebug("Refreshed spots list with {SpotCount} spots", spotsList.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh spots list");
                }
            });
        }

        /// <summary>
        /// Refresh the spot types collection
        /// </summary>
        private void RefreshSpotTypesList(IEnumerable<SpotType> types)
        {
            Application.Current?.Dispatcher.Dispatch(() =>
            {
                SpotTypes.Clear();
                if (types != null)
                {
                    foreach (var type in types)
                    {
                        SpotTypes.Add(type);
                    }
                }
            });
        }

        /// <summary>
        /// Update the empty state based on current data
        /// </summary>
        private void UpdateEmptyState()
        {
            Application.Current?.Dispatcher.Dispatch(() =>
            {
                // Show empty state only when appropriate
                IsEmptyState = !IsLoading &&
                              (Spots?.Count ?? 0) == 0 &&
                              !IsNetworkError &&
                              !IsSearching &&
                              !IsFiltering &&
                              string.IsNullOrEmpty(SearchText);
            });
        }

        /// <summary>
        /// Check network connectivity status
        /// </summary>
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
                _logger.LogError(ex, "Failed to check network connectivity");
                IsNetworkError = false;
            }
        }

        /// <summary>
        /// Handle connectivity changes
        /// </summary>
        private void OnConnectivityChanged(object sender, Microsoft.Maui.Networking.ConnectivityChangedEventArgs e)
        {
            Application.Current?.Dispatcher.Dispatch(() =>
            {
                var wasNetworkError = IsNetworkError;
                IsNetworkError = e.NetworkAccess != NetworkAccess.Internet;

                // Reload spots when connectivity is restored
                if (wasNetworkError && !IsNetworkError)
                {
                    _logger.LogInformation("Network connectivity restored, reloading spots");
                    LoadSpotsCommand.Execute(null);
                }

                UpdateEmptyState();
            });
        }

        /// <summary>
        /// Event to request map centering on spots
        /// </summary>
        public event Action<IEnumerable<Spot>> OnRequestCenterMapOnSpots;

        /// <summary>
        /// Dispose resources
        /// </summary>
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
                    _logger.LogWarning(ex, "Failed to unsubscribe from connectivity events");
                }
            }

            base.Dispose(disposing);
        }
    }
}