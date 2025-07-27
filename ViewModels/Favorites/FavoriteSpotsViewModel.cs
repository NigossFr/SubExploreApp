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
            EmptyStateMessage = "Aucun spot favori pour le moment.\nAjoutez des spots à vos favoris pour les retrouver facilement !";
            Title = "Mes Favoris";
            ShowByPriority = false;
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

                // Load favorites and stats in parallel for better performance
                var loadFavoritesTask = ShowByPriority 
                    ? _favoriteSpotService.GetUserFavoritesByPriorityAsync(currentUserId.Value)
                    : _favoriteSpotService.GetUserFavoritesAsync(currentUserId.Value);
                
                var loadStatsTask = _favoriteSpotService.GetUserFavoriteStatsAsync(currentUserId.Value);

                await Task.WhenAll(loadFavoritesTask, loadStatsTask).ConfigureAwait(false);
                var favorites = await loadFavoritesTask.ConfigureAwait(false);
                var stats = await loadStatsTask.ConfigureAwait(false);

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
            // ObservableCollection must be updated on the main UI thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                FavoriteSpots.Clear();
                foreach (var favorite in newFavorites)
                {
                    FavoriteSpots.Add(favorite);
                }
            });
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
            await LoadFavoritesAsync().ConfigureAwait(false);
            await LoadFavoriteStatsAsync().ConfigureAwait(false);
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
        /// Navigate to spot details
        /// </summary>
        /// <param name="favorite">Selected favorite spot</param>
        [RelayCommand]
        private async Task ViewSpotDetailsAsync(UserFavoriteSpot favorite)
        {
            if (favorite?.Spot == null)
                return;

            try
            {
                await NavigationService.NavigateToAsync<ViewModels.Spots.SpotDetailsViewModel>(
                    favorite.SpotId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to spot details for spot {SpotId}", favorite.SpotId);
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
            await LoadFavoritesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Navigate to add new favorite spot
        /// </summary>
        [RelayCommand]
        private async Task AddFavoriteAsync()
        {
            try
            {
                await NavigationService.NavigateToAsync<ViewModels.Map.MapViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to map for adding favorites");
            }
        }

        /// <summary>
        /// Get current authenticated user ID
        /// </summary>
        private async Task<int?> GetCurrentUserIdAsync()
        {
            try
            {
                // TODO: Implement authentication service method to get current user ID
                // This is a placeholder implementation
                return 1; // Admin user for testing
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user ID");
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
    }
}