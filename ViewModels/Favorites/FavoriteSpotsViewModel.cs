using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Favorites
{
    /// <summary>
    /// ViewModel for managing and displaying user's favorite spots
    /// </summary>
    public partial class FavoriteSpotsViewModel : ViewModelBase
    {
        private readonly IFavoriteSpotService _favoriteSpotService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<FavoriteSpotsViewModel> _logger;

        [ObservableProperty]
        private ObservableCollection<UserFavoriteSpot> _favoriteSpots;

        [ObservableProperty]
        private FavoriteSpotStats? _favoriteStats;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private bool _hasFavorites;

        [ObservableProperty]
        private string _emptyStateMessage;

        [ObservableProperty]
        private bool _showByPriority;

        [ObservableProperty]
        private UserFavoriteSpot? _selectedFavorite;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _hasSearchText;

        [ObservableProperty]
        private int? _selectedPriorityFilter;

        [ObservableProperty]
        private string _notificationFilter = "Tous"; // "Tous", "Activées", "Désactivées"

        private ObservableCollection<UserFavoriteSpot> _allFavorites;
        private System.Timers.Timer? _searchTimer;

        /// <summary>
        /// Initializes a new instance of the FavoriteSpotsViewModel
        /// </summary>
        public FavoriteSpotsViewModel(
            IFavoriteSpotService favoriteSpotService,
            IAuthenticationService authenticationService,
            ILogger<FavoriteSpotsViewModel> logger,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _favoriteSpotService = favoriteSpotService ?? throw new ArgumentNullException(nameof(favoriteSpotService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            FavoriteSpots = new ObservableCollection<UserFavoriteSpot>();
            _allFavorites = new ObservableCollection<UserFavoriteSpot>();
            EmptyStateMessage = "Aucun spot favori pour le moment.\nExplorez la carte pour découvrir et ajouter des spots à vos favoris !";
            Title = "Mes Favoris";
            ShowByPriority = false;
            
            // Setup search timer for real-time search
            _searchTimer = new System.Timers.Timer(300); // 300ms delay
            _searchTimer.Elapsed += OnSearchTimerElapsed;
            _searchTimer.AutoReset = false;
        }

        /// <summary>
        /// Initialize the ViewModel with favorite spots data
        /// </summary>
        public override async Task InitializeAsync(object parameter = null)
        {
            await LoadFavoritesAsync().ConfigureAwait(false);
            await LoadFavoriteStatsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Load user's favorite spots with optimized performance
        /// </summary>
        [RelayCommand]
        private async Task LoadFavoritesAsync()
        {
            try
            {
                IsLoading = true;
                IsError = false;
                ErrorMessage = string.Empty;

                var currentUserId = await GetCurrentUserIdAsync().ConfigureAwait(false);
                if (currentUserId == null)
                {
                    await HandleNotAuthenticatedAsync().ConfigureAwait(false);
                    return;
                }

                // Load favorites and stats sequentially to prevent DbContext concurrency issues
                // "A second operation was started on this context instance before a previous operation completed"
                var favorites = ShowByPriority 
                    ? await _favoriteSpotService.GetUserFavoritesByPriorityAsync(currentUserId.Value).ConfigureAwait(false)
                    : await _favoriteSpotService.GetUserFavoritesAsync(currentUserId.Value).ConfigureAwait(false);
                
                var stats = await _favoriteSpotService.GetUserFavoriteStatsAsync(currentUserId.Value).ConfigureAwait(false);

                // Update collections efficiently
                await UpdateFavoritesCollectionAsync(favorites).ConfigureAwait(false);
                FavoriteStats = stats;
                HasFavorites = FavoriteSpots.Any();

                _logger.LogInformation("Loaded {Count} favorite spots for user {UserId}", 
                    FavoriteSpots.Count, currentUserId.Value);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters for loading favorites");
                IsError = true;
                ErrorMessage = "Paramètres invalides. Veuillez vous reconnecter.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorite spots");
                IsError = true;
                ErrorMessage = "Erreur lors du chargement des favoris. Veuillez réessayer.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Efficiently update the favorites collection on the main thread
        /// </summary>
        private async Task UpdateFavoritesCollectionAsync(IEnumerable<UserFavoriteSpot> newFavorites)
        {
            // Store all favorites for filtering
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _allFavorites.Clear();
                foreach (var favorite in newFavorites)
                {
                    _allFavorites.Add(favorite);
                }
            });
            
            // Apply current filters
            await ApplyFiltersAsync();
        }

        /// <summary>
        /// Load favorite spots statistics
        /// </summary>
        private async Task LoadFavoriteStatsAsync()
        {
            try
            {
                var currentUserId = await GetCurrentUserIdAsync().ConfigureAwait(false);
                if (currentUserId == null)
                    return;

                FavoriteStats = await _favoriteSpotService.GetUserFavoriteStatsAsync(currentUserId.Value).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorite stats");
            }
        }

        /// <summary>
        /// Refresh the favorites list
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            // Load favorites (which now includes stats loading) sequentially
            await LoadFavoritesAsync().ConfigureAwait(false);
            IsRefreshing = false;
        }

        /// <summary>
        /// Remove a spot from favorites with optimized UX
        /// </summary>
        /// <param name="favorite">Favorite spot to remove</param>
        [RelayCommand]
        private async Task RemoveFromFavoritesAsync(UserFavoriteSpot favorite)
        {
            if (favorite?.Spot == null)
            {
                _logger.LogWarning("Attempted to remove null favorite");
                return;
            }

            try
            {
                var confirmed = await DialogService.ShowConfirmationAsync(
                    "Retirer des favoris",
                    $"Voulez-vous retirer \"{favorite.Spot.Name}\" de vos favoris ?",
                    "Retirer",
                    "Annuler").ConfigureAwait(false);

                if (!confirmed)
                    return;

                var currentUserId = await GetCurrentUserIdAsync().ConfigureAwait(false);
                if (currentUserId == null)
                {
                    await HandleNotAuthenticatedAsync().ConfigureAwait(false);
                    return;
                }

                // Optimistic UI update - remove from collection immediately
                var originalIndex = FavoriteSpots.IndexOf(favorite);
                FavoriteSpots.Remove(favorite);
                HasFavorites = FavoriteSpots.Any();

                try
                {
                    var removed = await _favoriteSpotService.RemoveFromFavoritesAsync(
                        currentUserId.Value, 
                        favorite.SpotId).ConfigureAwait(false);

                    if (removed)
                    {
                        // Update stats and show success message
                        await LoadFavoriteStatsAsync().ConfigureAwait(false);
                        await DialogService.ShowToastAsync($"Spot retiré des favoris").ConfigureAwait(false);
                        _logger.LogInformation("Removed spot {SpotId} from favorites for user {UserId}", 
                            favorite.SpotId, currentUserId.Value);
                    }
                    else
                    {
                        // Rollback optimistic update
                        FavoriteSpots.Insert(originalIndex, favorite);
                        HasFavorites = FavoriteSpots.Any();
                        
                        await DialogService.ShowAlertAsync(
                            "Erreur", 
                            "Impossible de retirer le spot des favoris.", 
                            "OK").ConfigureAwait(false);
                    }
                }
                catch (Exception serviceEx)
                {
                    // Rollback optimistic update on service error
                    FavoriteSpots.Insert(originalIndex, favorite);
                    HasFavorites = FavoriteSpots.Any();
                    throw serviceEx;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing spot {SpotId} from favorites", favorite.SpotId);
                await DialogService.ShowAlertAsync(
                    "Erreur", 
                    "Une erreur est survenue lors de la suppression.", 
                    "OK").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Navigate to spot details with enhanced parameter passing
        /// </summary>
        /// <param name="favorite">Selected favorite spot</param>
        [RelayCommand]
        private async Task ViewSpotDetailsAsync(UserFavoriteSpot favorite)
        {
            if (favorite?.Spot == null)
                return;

            try
            {
                // Enhanced navigation with source context for better back navigation
                var navigationParameter = new Dictionary<string, object>
                {
                    ["SpotId"] = favorite.SpotId,
                    ["Source"] = "Favorites",
                    ["IsFavorite"] = true,
                    ["FavoriteId"] = favorite.Id
                };

                await NavigationService.NavigateToAsync<ViewModels.Spots.SpotDetailsViewModel>(
                    navigationParameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to spot details for spot {SpotId}", favorite.SpotId);
                await DialogService.ShowAlertAsync(
                    "Erreur", 
                    "Impossible d'afficher les détails du spot.", 
                    "OK");
            }
        }

        /// <summary>
        /// Update favorite priority
        /// </summary>
        /// <param name="favorite">Favorite to update</param>
        [RelayCommand]
        private async Task UpdatePriorityAsync(UserFavoriteSpot favorite)
        {
            if (favorite == null)
                return;

            try
            {
                // TODO: Implement priority picker dialog or slider
                var newPriority = await ShowPriorityPickerAsync(favorite.Priority).ConfigureAwait(false);
                if (newPriority == null || newPriority == favorite.Priority)
                    return;

                var currentUserId = await GetCurrentUserIdAsync().ConfigureAwait(false);
                if (currentUserId == null)
                {
                    await HandleNotAuthenticatedAsync().ConfigureAwait(false);
                    return;
                }

                var updated = await _favoriteSpotService.UpdateFavoritePriorityAsync(
                    currentUserId.Value, 
                    favorite.SpotId, 
                    newPriority.Value).ConfigureAwait(false);

                if (updated)
                {
                    favorite.Priority = newPriority.Value;
                    
                    // Refresh if showing by priority to maintain correct order
                    if (ShowByPriority)
                    {
                        await LoadFavoritesAsync().ConfigureAwait(false);
                    }

                    await DialogService.ShowToastAsync("Priorité mise à jour").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating favorite priority");
                await DialogService.ShowAlertAsync(
                    "Erreur", 
                    "Impossible de mettre à jour la priorité.", 
                    "OK").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Toggle notification for a favorite spot
        /// </summary>
        /// <param name="favorite">Favorite to update</param>
        [RelayCommand]
        private async Task ToggleNotificationAsync(UserFavoriteSpot favorite)
        {
            if (favorite == null)
                return;

            try
            {
                var currentUserId = await GetCurrentUserIdAsync().ConfigureAwait(false);
                if (currentUserId == null)
                {
                    await HandleNotAuthenticatedAsync().ConfigureAwait(false);
                    return;
                }

                var newState = !favorite.NotificationEnabled;
                var updated = await _favoriteSpotService.UpdateFavoriteNotificationAsync(
                    currentUserId.Value, 
                    favorite.SpotId, 
                    newState).ConfigureAwait(false);

                if (updated)
                {
                    favorite.NotificationEnabled = newState;
                    await LoadFavoriteStatsAsync().ConfigureAwait(false);

                    var message = newState 
                        ? "Notifications activées" 
                        : "Notifications désactivées";
                    await DialogService.ShowToastAsync(message).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite notification");
            }
        }

        /// <summary>
        /// Toggle sorting between priority and date
        /// </summary>
        [RelayCommand]
        private async Task ToggleSortingAsync()
        {
            ShowByPriority = !ShowByPriority;
            await ApplyFiltersAsync();
        }

        /// <summary>
        /// Navigate to map to explore and discover new spots
        /// </summary>
        [RelayCommand]
        private async Task ExploreMapAsync()
        {
            try
            {
                await NavigationService.NavigateToAsync<ViewModels.Map.MapViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to map for exploring spots");
            }
        }

        /// <summary>
        /// Navigate to user's own spots
        /// </summary>
        [RelayCommand]
        private async Task ViewMySpotsAsync()
        {
            try
            {
                await NavigationService.NavigateToAsync<ViewModels.Spots.MySpotsViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to my spots");
            }
        }

        /// <summary>
        /// Navigate to add new spot
        /// </summary>
        [RelayCommand]
        private async Task AddNewSpotAsync()
        {
            try
            {
                await NavigationService.NavigateToAsync<ViewModels.Spots.AddSpotViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to add new spot");
            }
        }

        /// <summary>
        /// Get current authenticated user ID
        /// </summary>
        private async Task<int?> GetCurrentUserIdAsync()
        {
            try
            {
                _logger.LogDebug("🔍 GetCurrentUserIdAsync: Starting authentication check");
                
                // Use the actual authentication service to get current user ID
                var currentUser = _authenticationService.CurrentUser;
                _logger.LogDebug("🔍 GetCurrentUserIdAsync: CurrentUser = {User}", currentUser?.Id.ToString() ?? "NULL");
                
                if (currentUser != null)
                {
                    _logger.LogDebug("✅ GetCurrentUserIdAsync: Found current user with ID {UserId}", currentUser.Id);
                    return currentUser.Id;
                }
                
                // Check IsAuthenticated property
                var isAuthenticatedProperty = _authenticationService.IsAuthenticated;
                _logger.LogDebug("🔍 GetCurrentUserIdAsync: IsAuthenticated property = {IsAuthenticated}", isAuthenticatedProperty);
                
                // Try to validate authentication if user is null
                _logger.LogDebug("🔍 GetCurrentUserIdAsync: Attempting to validate authentication...");
                var isAuthenticated = await _authenticationService.ValidateAuthenticationAsync().ConfigureAwait(false);
                _logger.LogDebug("🔍 GetCurrentUserIdAsync: ValidateAuthenticationAsync result = {IsAuthenticated}", isAuthenticated);
                
                if (isAuthenticated)
                {
                    var userAfterValidation = _authenticationService.CurrentUser;
                    _logger.LogDebug("🔍 GetCurrentUserIdAsync: CurrentUser after validation = {User}", userAfterValidation?.Id.ToString() ?? "NULL");
                    return userAfterValidation?.Id;
                }
                
                _logger.LogWarning("❌ GetCurrentUserIdAsync: Authentication validation failed");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting current user ID");
                return null;
            }
        }

        /// <summary>
        /// Handle not authenticated scenario
        /// </summary>
        private async Task HandleNotAuthenticatedAsync()
        {
            await DialogService.ShowAlertAsync(
                "Authentification requise",
                "Vous devez être connecté pour accéder à vos favoris.",
                "OK").ConfigureAwait(false);
            
            // TODO: Navigate to login page
        }

        /// <summary>
        /// Show priority picker dialog
        /// </summary>
        private async Task<int?> ShowPriorityPickerAsync(int currentPriority)
        {
            try
            {
                // TODO: Implement custom priority picker dialog
                // For now, show a simple prompt
                var priorities = new[] { "1 - Très haute", "2 - Haute", "3 - Moyenne-haute", "4 - Moyenne", "5 - Normale", 
                                       "6 - Moyenne-basse", "7 - Basse", "8 - Très basse", "9 - Faible", "10 - Très faible" };
                
                var selected = await DialogService.ShowActionSheetAsync(
                    "Sélectionner la priorité",
                    "Annuler",
                    null,
                    priorities).ConfigureAwait(false);

                if (string.IsNullOrEmpty(selected) || selected == "Annuler")
                    return null;

                // Extract priority number from selection
                var priorityChar = selected[0];
                if (char.IsDigit(priorityChar))
                {
                    return int.Parse(priorityChar.ToString());
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing priority picker");
                return null;
            }
        }

        /// <summary>
        /// Apply search and filters to the favorites list
        /// </summary>
        private async Task ApplyFiltersAsync()
        {
            try
            {
                var filteredFavorites = _allFavorites.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLowerInvariant();
                    filteredFavorites = filteredFavorites.Where(f => 
                        (f.Spot?.Name?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (f.Spot?.Description?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (f.Notes?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (f.Spot?.Type?.Name?.ToLowerInvariant().Contains(searchLower) ?? false));
                }

                // Apply priority filter
                if (SelectedPriorityFilter.HasValue)
                {
                    filteredFavorites = filteredFavorites.Where(f => f.Priority == SelectedPriorityFilter.Value);
                }

                // Apply notification filter
                if (NotificationFilter == "Activées")
                {
                    filteredFavorites = filteredFavorites.Where(f => f.NotificationEnabled);
                }
                else if (NotificationFilter == "Désactivées")
                {
                    filteredFavorites = filteredFavorites.Where(f => !f.NotificationEnabled);
                }

                // Apply sorting
                if (ShowByPriority)
                {
                    filteredFavorites = filteredFavorites.OrderBy(f => f.Priority).ThenByDescending(f => f.CreatedAt);
                }
                else
                {
                    filteredFavorites = filteredFavorites.OrderByDescending(f => f.CreatedAt);
                }

                // Update the displayed collection
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    FavoriteSpots.Clear();
                    foreach (var favorite in filteredFavorites)
                    {
                        FavoriteSpots.Add(favorite);
                    }
                    HasFavorites = FavoriteSpots.Any();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying filters to favorites");
            }
        }

        /// <summary>
        /// Handle search text changes with debouncing
        /// </summary>
        partial void OnSearchTextChanged(string value)
        {
            HasSearchText = !string.IsNullOrWhiteSpace(value);
            
            _searchTimer?.Stop();
            _searchTimer?.Start();
        }

        /// <summary>
        /// Search timer elapsed - apply search filter
        /// </summary>
        private async void OnSearchTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            await ApplyFiltersAsync();
        }

        /// <summary>
        /// Clear search text
        /// </summary>
        [RelayCommand]
        private async Task ClearSearchAsync()
        {
            SearchText = string.Empty;
            await ApplyFiltersAsync();
        }

        /// <summary>
        /// Show priority filter options
        /// </summary>
        [RelayCommand]
        private async Task ShowPriorityFilterAsync()
        {
            try
            {
                var options = new[] 
                { 
                    "Toutes les priorités",
                    "1 - Très haute", 
                    "2 - Haute", 
                    "3 - Moyenne-haute", 
                    "4 - Moyenne", 
                    "5 - Normale",
                    "6 - Moyenne-basse", 
                    "7 - Basse", 
                    "8 - Très basse", 
                    "9 - Faible", 
                    "10 - Très faible" 
                };
                
                var selected = await DialogService.ShowActionSheetAsync(
                    "Filtrer par priorité",
                    "Annuler",
                    null,
                    options);

                if (string.IsNullOrEmpty(selected) || selected == "Annuler")
                    return;

                if (selected == "Toutes les priorités")
                {
                    SelectedPriorityFilter = null;
                }
                else
                {
                    var priorityChar = selected[0];
                    if (char.IsDigit(priorityChar))
                    {
                        SelectedPriorityFilter = int.Parse(priorityChar.ToString());
                    }
                }

                await ApplyFiltersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing priority filter");
            }
        }

        /// <summary>
        /// Toggle notification filter
        /// </summary>
        [RelayCommand]
        private async Task ToggleNotificationFilterAsync()
        {
            NotificationFilter = NotificationFilter switch
            {
                "Tous" => "Activées",
                "Activées" => "Désactivées",
                "Désactivées" => "Tous",
                _ => "Tous"
            };
            
            await ApplyFiltersAsync();
        }

        /// <summary>
        /// View weather for a specific spot
        /// </summary>
        /// <param name="favorite">Favorite spot</param>
        [RelayCommand]
        private async Task ViewWeatherAsync(UserFavoriteSpot favorite)
        {
            if (favorite?.Spot == null)
                return;

            try
            {
                // Navigate to weather details or show weather popup
                var weatherInfo = $"Météo pour {favorite.Spot.Name}:\nLatitude: {favorite.Spot.Latitude}\nLongitude: {favorite.Spot.Longitude}";
                await DialogService.ShowAlertAsync(
                    "Météo du spot",
                    weatherInfo,
                    "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing weather for spot {SpotId}", favorite.SpotId);
            }
        }

        /// <summary>
        /// Navigate to spot using GPS
        /// </summary>
        /// <param name="favorite">Favorite spot</param>
        [RelayCommand]
        private async Task NavigateToSpotAsync(UserFavoriteSpot favorite)
        {
            if (favorite?.Spot == null)
                return;

            try
            {
                // Use platform-specific navigation
                var latitude = (double)favorite.Spot.Latitude;
                var longitude = (double)favorite.Spot.Longitude;
                var spotName = Uri.EscapeDataString(favorite.Spot.Name ?? "Spot de plongée");
                
                var uri = DeviceInfo.Platform == DevicePlatform.iOS
                    ? $"maps://?q={latitude},{longitude}&ll={latitude},{longitude}"
                    : $"geo:{latitude},{longitude}?q={latitude},{longitude}({spotName})";

                if (await Launcher.CanOpenAsync(uri))
                {
                    await Launcher.OpenAsync(uri);
                }
                else
                {
                    await DialogService.ShowAlertAsync(
                        "Navigation indisponible",
                        "Impossible d'ouvrir l'application de navigation.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to spot {SpotId}", favorite.SpotId);
                await DialogService.ShowAlertAsync(
                    "Erreur",
                    "Impossible de lancer la navigation.",
                    "OK");
            }
        }

        /// <summary>
        /// Share spot information
        /// </summary>
        /// <param name="favorite">Favorite spot</param>
        [RelayCommand]
        private async Task ShareSpotAsync(UserFavoriteSpot favorite)
        {
            if (favorite?.Spot == null)
                return;

            try
            {
                var shareText = $"🌊 {favorite.Spot.Name}\n\n" +
                               $"📍 {favorite.Spot.Description}\n\n" +
                               $"🎯 Difficulté: {favorite.Spot.DifficultyLevel}\n" +
                               $"🌊 Profondeur max: {favorite.Spot.MaxDepth}m\n\n" +
                               $"📍 Coordonnées: {favorite.Spot.Latitude}, {favorite.Spot.Longitude}\n\n" +
                               $"Partagé via SubExplore 🤿";

                await Share.RequestAsync(new ShareTextRequest
                {
                    Text = shareText,
                    Title = $"Spot de plongée: {favorite.Spot.Name}"
                });

                _logger.LogInformation("Shared spot {SpotId} via share sheet", favorite.SpotId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing spot {SpotId}", favorite.SpotId);
                await DialogService.ShowAlertAsync(
                    "Erreur",
                    "Impossible de partager le spot.",
                    "OK");
            }
        }

        /// <summary>
        /// Export favorites to file
        /// </summary>
        [RelayCommand]
        private async Task ExportFavoritesAsync()
        {
            try
            {
                if (!FavoriteSpots.Any())
                {
                    await DialogService.ShowAlertAsync(
                        "Aucun favori",
                        "Vous n'avez aucun favori à exporter.",
                        "OK");
                    return;
                }

                var exportData = string.Join("\n", FavoriteSpots.Select(f => 
                    $"{f.Spot?.Name},{f.Spot?.Latitude},{f.Spot?.Longitude},{f.Priority},{f.Notes}"));
                
                var header = "Nom,Latitude,Longitude,Priorité,Notes\n";
                var csvContent = header + exportData;

                // Save to file or share
                await Share.RequestAsync(new ShareTextRequest
                {
                    Text = csvContent,
                    Title = "Export des favoris SubExplore"
                });

                await DialogService.ShowToastAsync("Favoris exportés avec succès!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting favorites");
                await DialogService.ShowAlertAsync(
                    "Erreur",
                    "Impossible d'exporter les favoris.",
                    "OK");
            }
        }

        /// <summary>
        /// Import favorites from file
        /// </summary>
        [RelayCommand]
        private async Task ImportFavoritesAsync()
        {
            try
            {
                await DialogService.ShowAlertAsync(
                    "Import de favoris",
                    "Fonctionnalité d'import en cours de développement.\n\nVous pourrez bientôt importer vos favoris depuis un fichier CSV ou depuis d'autres applications de plongée.",
                    "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing favorites");
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            _searchTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}