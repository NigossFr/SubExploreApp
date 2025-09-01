using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Navigation;

namespace SubExplore.ViewModels.Favorites
{
    public partial class FavoriteSpotsViewModel : ViewModelBase
    {
        private readonly IFavoriteSpotService _favoriteSpotService;
        private readonly ISimpleAuthenticationService _authenticationService;
        private readonly IOfflineFavoriteService _offlineFavoriteService;
        private readonly IFavoriteExportImportService _exportImportService;
        private readonly ILogger<FavoriteSpotsViewModel>? _logger;

        // Concurrency control
        private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);
        private bool _isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<UserFavoriteSpot> _favoriteSpots;

        [ObservableProperty]
        private ObservableCollection<UserFavoriteSpot> _filteredFavoriteSpots;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private bool _isError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasFavorites;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _emptyStateMessage = "Vous n'avez pas encore ajouté de spots à vos favoris. Explorez les spots disponibles et marquez ceux que vous préférez !";

        [ObservableProperty]
        private FavoriteStats? _favoriteStats;

        public FavoriteSpotsViewModel(
            IFavoriteSpotService favoriteSpotService,
            ISimpleAuthenticationService authenticationService,
            IOfflineFavoriteService offlineFavoriteService,
            IFavoriteExportImportService exportImportService,
            IDialogService dialogService,
            INavigationService navigationService,
            ILogger<FavoriteSpotsViewModel>? logger = null)
            : base(dialogService, navigationService)
        {
            _favoriteSpotService = favoriteSpotService ?? throw new ArgumentNullException(nameof(favoriteSpotService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _offlineFavoriteService = offlineFavoriteService ?? throw new ArgumentNullException(nameof(offlineFavoriteService));
            _exportImportService = exportImportService ?? throw new ArgumentNullException(nameof(exportImportService));
            _logger = logger;

            Title = "Favoris";
            FavoriteSpots = new ObservableCollection<UserFavoriteSpot>();
            FilteredFavoriteSpots = new ObservableCollection<UserFavoriteSpot>();
            
            // Property change handlers
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchText))
            {
                ApplyFilters();
            }
        }

        public override async Task InitializeAsync(object? parameter = null)
        {
            // Prevent duplicate initialization
            if (_isInitialized)
            {
                _logger?.LogInformation("FavoriteSpotsViewModel already initialized, skipping");
                return;
            }

            try
            {
                await _loadingSemaphore.WaitAsync();
                
                if (_isInitialized) return; // Double-check
                
                await LoadFavoriteSpots();
                _isInitialized = true;
                
                _logger?.LogInformation("FavoriteSpotsViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing FavoriteSpotsViewModel");
                SetError("Impossible de charger vos favoris.");
            }
            finally
            {
                _loadingSemaphore.Release();
            }
        }

        [RelayCommand]
        private async Task LoadFavoriteSpots()
        {
            try
            {
                IsLoading = true;
                IsError = false;
                ErrorMessage = string.Empty;

                // Check if user is authenticated
                if (!_authenticationService.IsAuthenticated)
                {
                    _logger?.LogWarning("User not authenticated, cannot load favorites");
                    SetEmptyState();
                    return;
                }

                var currentUser = _authenticationService.CurrentUser;
                if (currentUser?.Id == null)
                {
                    _logger?.LogWarning("No current user available");
                    SetEmptyState();
                    return;
                }

                // Load user favorites - check offline mode first
                IEnumerable<UserFavoriteSpot> userFavorites;
                if (_offlineFavoriteService.IsOfflineModeActive)
                {
                    userFavorites = await _offlineFavoriteService.GetOfflineFavoritesAsync(currentUser.Id);
                    _logger?.LogInformation("Loaded favorites from offline cache");
                }
                else
                {
                    userFavorites = await _favoriteSpotService.GetUserFavoritesAsync(currentUser.Id);
                }
                
                // Update collections on UI thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    FavoriteSpots.Clear();
                    foreach (var favorite in userFavorites)
                    {
                        FavoriteSpots.Add(favorite);
                    }
                    
                    ApplyFilters();
                    UpdateStatistics();
                    UpdateEmptyState();
                });

                _logger?.LogInformation($"Loaded {userFavorites.Count()} favorite spots for user {currentUser.Id}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading favorite spots");
                SetError("Erreur lors du chargement des favoris.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshFavorites()
        {
            try
            {
                IsRefreshing = true;
                await LoadFavoriteSpots();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await RefreshFavorites();
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        [RelayCommand]
        private async Task ShowActivityFilter()
        {
            try
            {
                // TODO: Implement activity filter when needed
                await DialogService.ShowAlertAsync("Information", "Filtre par activité bientôt disponible.", "OK");
                
                _logger?.LogInformation("Activity filter requested");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing activity filter");
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'afficher le filtre.", "OK");
            }
        }

        [RelayCommand]
        private async Task ExploreMap()
        {
            try
            {
                // Navigate to map page
                await Shell.Current.GoToAsync("///map");
                
                _logger?.LogInformation("Navigating to map to explore spots");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error navigating to map");
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'ouvrir la carte.", "OK");
            }
        }

        [RelayCommand]
        private async Task ViewMySpots()
        {
            try
            {
                // Navigate to my spots page
                await Shell.Current.GoToAsync("///myspots");
                
                _logger?.LogInformation("Navigating to my spots page");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error navigating to my spots");
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'ouvrir mes spots.", "OK");
            }
        }

        [RelayCommand]
        private async Task ImportFavorites()
        {
            try
            {
                var importOptions = await DialogService.ShowActionSheetAsync(
                    "Importer des favoris", 
                    "Annuler", 
                    null, 
                    "Fichier CSV", "Fichier JSON", "Fichier GPX");
                
                if (string.IsNullOrEmpty(importOptions) || importOptions == "Annuler")
                    return;

                _logger?.LogInformation($"Import favorites requested: {importOptions}");
                
                // In a real implementation, you would use FilePicker here
                await DialogService.ShowAlertAsync("Information", 
                    $"Sélectionnez un {importOptions} à importer.\n\nNote: La fonctionnalité de sélection de fichiers sera implémentée prochainement.", 
                    "OK");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error importing favorites");
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'importer les favoris.", "OK");
            }
        }

        [RelayCommand]
        private async Task ToggleNotification(UserFavoriteSpot favorite)
        {
            if (favorite == null) return;

            try
            {
                // Toggle notification status
                favorite.NotificationEnabled = !favorite.NotificationEnabled;
                
                // TODO: Update in database when UserFavoriteSpot service is implemented
                
                var status = favorite.NotificationEnabled ? "activées" : "désactivées";
                await DialogService.ShowAlertAsync("Notifications", $"Notifications {status} pour {favorite.Spot.Name}.", "OK");
                
                _logger?.LogInformation($"Notifications {(favorite.NotificationEnabled ? "enabled" : "disabled")} for spot {favorite.Spot.Id}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error toggling notification for spot {favorite?.Spot?.Id}");
                await DialogService.ShowAlertAsync("Erreur", "Impossible de modifier les notifications.", "OK");
            }
        }

        [RelayCommand]
        private async Task ViewSpotDetails(UserFavoriteSpot favorite)
        {
            if (favorite == null) return;

            try
            {
                _logger?.LogInformation($"Viewing details for favorite spot {favorite.Spot.Id}");
                
                // ✅ FIXED: Use absolute Shell navigation with /// prefix
                await Shell.Current.GoToAsync($"///spotdetails?spotId={favorite.SpotId}");
                
                _logger?.LogInformation("Navigated to spot details with favorite context");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error viewing spot details {favorite?.Spot?.Id}");
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'afficher les détails du spot.", "OK");
            }
        }

        [RelayCommand]
        private async Task RemoveFromFavorites(UserFavoriteSpot favorite)
        {
            if (favorite == null) return;

            try
            {
                var confirm = await DialogService.ShowConfirmationAsync(
                    "Retirer des favoris", 
                    $"Voulez-vous retirer '{favorite.Spot.Name}' de vos favoris ?", 
                    "Retirer", 
                    "Annuler");

                if (!confirm) return;

                // Remove from favorites using appropriate service
                bool success;
                if (_offlineFavoriteService.IsOfflineModeActive)
                {
                    success = await _offlineFavoriteService.RemoveOfflineFavoriteAsync(favorite.UserId, favorite.Spot.Id);
                    _logger?.LogInformation("Queued favorite removal for offline sync");
                }
                else
                {
                    success = await _favoriteSpotService.RemoveFromFavoritesAsync(favorite.UserId, favorite.Spot.Id);
                }
                if (success)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        FavoriteSpots.Remove(favorite);
                        ApplyFilters();
                        UpdateStatistics();
                        UpdateEmptyState();
                    });

                    await DialogService.ShowAlertAsync("Succès", "Spot retiré de vos favoris.", "OK");
                    _logger?.LogInformation($"Spot {favorite.Spot.Id} removed from favorites");
                }
                else
                {
                    await DialogService.ShowAlertAsync("Erreur", "Impossible de retirer ce spot des favoris.", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error removing spot from favorites {favorite?.Spot?.Id}");
                await DialogService.ShowAlertAsync("Erreur", "Erreur lors de la suppression du favori.", "OK");
            }
        }

        [RelayCommand]
        private async Task ExportFavorites()
        {
            try
            {
                if (!_authenticationService.IsAuthenticated || _authenticationService.CurrentUser?.Id == null)
                {
                    await DialogService.ShowAlertAsync("Erreur", "Vous devez être connecté pour exporter vos favoris.", "OK");
                    return;
                }

                var exportFormat = await DialogService.ShowActionSheetAsync(
                    "Exporter mes favoris", 
                    "Annuler", 
                    null, 
                    "CSV (Excel)", "JSON (Complet)", "GPX (GPS)");
                
                if (string.IsNullOrEmpty(exportFormat) || exportFormat == "Annuler")
                    return;

                _logger?.LogInformation($"Export favorites requested: {exportFormat}");
                
                // In a real implementation, you would use FilePicker/folder picker here
                var userId = _authenticationService.CurrentUser.Id;
                var timestamp = DateTime.UtcNow;
                var format = _exportImportService.GetSupportedExportFormats().First();
                var defaultFileName = _exportImportService.GetDefaultExportFileName(userId, format, timestamp);
                
                await DialogService.ShowAlertAsync("Export", 
                    $"Export en cours...\n\nFichier: {defaultFileName}\n\nNote: La sélection du dossier de destination sera implémentée prochainement.", 
                    "OK");

                _logger?.LogInformation("Export favorites functionality ready");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting favorites");
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'exporter les favoris.", "OK");
            }
        }

        [RelayCommand]
        private async Task ToggleOfflineMode()
        {
            try
            {
                if (_offlineFavoriteService.IsOfflineModeActive)
                {
                    var confirm = await DialogService.ShowConfirmationAsync(
                        "Mode hors ligne", 
                        "Désactiver le mode hors ligne ? Les modifications en attente seront synchronisées.", 
                        "Désactiver", 
                        "Annuler");

                    if (confirm)
                    {
                        var success = await _offlineFavoriteService.DisableOfflineModeAsync();
                        if (success)
                        {
                            await DialogService.ShowAlertAsync("Mode hors ligne", "Mode hors ligne désactivé.", "OK");
                            await LoadFavoriteSpots(); // Refresh from online
                        }
                    }
                }
                else
                {
                    var success = await _offlineFavoriteService.EnableOfflineModeAsync();
                    if (success)
                    {
                        await DialogService.ShowAlertAsync("Mode hors ligne", "Mode hors ligne activé. Vos favoris sont maintenant disponibles sans connexion.", "OK");
                    }
                }

                _logger?.LogInformation($"Offline mode toggled: {_offlineFavoriteService.IsOfflineModeActive}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error toggling offline mode");
                await DialogService.ShowAlertAsync("Erreur", "Impossible de modifier le mode hors ligne.", "OK");
            }
        }

        [RelayCommand]
        private async Task ShowSyncStatus()
        {
            try
            {
                var pendingCount = await _offlineFavoriteService.GetPendingSyncCountAsync();
                var storageInfo = await _offlineFavoriteService.GetStorageInfoAsync();

                var message = $"Mode hors ligne: {(_offlineFavoriteService.IsOfflineModeActive ? "Activé" : "Désactivé")}\n" +
                             $"Opérations en attente: {pendingCount}\n" +
                             $"Taille du cache: {storageInfo.GetFormattedSize()}\n" +
                             $"Dernière synchro: {(storageInfo.LastSyncDate == DateTime.MinValue ? "Jamais" : storageInfo.LastSyncDate.ToString("dd/MM/yyyy HH:mm"))}";

                if (pendingCount > 0)
                {
                    var sync = await DialogService.ShowConfirmationAsync(
                        "Statut de synchronisation", 
                        message + "\n\nVoulez-vous synchroniser maintenant ?", 
                        "Synchroniser", 
                        "Fermer");

                    if (sync)
                    {
                        await SyncPendingOperations();
                    }
                }
                else
                {
                    await DialogService.ShowAlertAsync("Statut de synchronisation", message, "OK");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing sync status");
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'afficher le statut de synchronisation.", "OK");
            }
        }

        [RelayCommand]
        private async Task SyncPendingOperations()
        {
            try
            {
                var pendingCount = await _offlineFavoriteService.GetPendingSyncCountAsync();
                if (pendingCount == 0)
                {
                    await DialogService.ShowAlertAsync("Synchronisation", "Aucune opération en attente de synchronisation.", "OK");
                    return;
                }

                IsLoading = true;
                _logger?.LogInformation($"Starting manual sync of {pendingCount} operations");

                var result = await _offlineFavoriteService.SyncPendingOperationsAsync();

                var message = $"Synchronisation terminée:\n" +
                             $"• {result.SuccessfulOperations} réussies\n" +
                             $"• {result.FailedOperations} échouées\n" +
                             $"• Durée: {result.SyncDuration.TotalSeconds:F1}s";

                if (result.IsSuccess)
                {
                    await DialogService.ShowAlertAsync("Synchronisation", message, "OK");
                    await LoadFavoriteSpots(); // Refresh after sync
                }
                else
                {
                    message += $"\n\nErreurs: {string.Join(", ", result.Errors.Take(3))}";
                    await DialogService.ShowAlertAsync("Synchronisation", message, "OK");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during manual sync");
                await DialogService.ShowAlertAsync("Erreur", "Erreur lors de la synchronisation.", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = FavoriteSpots.AsEnumerable();

                // Apply text search
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLowerInvariant();
                    filtered = filtered.Where(f => 
                        f.Spot.Name.ToLowerInvariant().Contains(searchLower) ||
                        (f.Spot.Description?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (f.Spot.Type?.Name?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (f.Notes?.ToLowerInvariant().Contains(searchLower) ?? false));
                }

                // Update filtered collection on UI thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FilteredFavoriteSpots.Clear();
                    foreach (var favorite in filtered)
                    {
                        FilteredFavoriteSpots.Add(favorite);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying filters");
            }
        }

        private void UpdateStatistics()
        {
            try
            {
                var stats = new FavoriteStats
                {
                    TotalFavorites = FavoriteSpots.Count,
                    NotificationEnabled = FavoriteSpots.Count(f => f.NotificationEnabled),
                    ActivityFavorites = FavoriteSpots.Select(f => f.Spot.Type?.Category).Distinct().Count()
                };

                FavoriteStats = stats;

                _logger?.LogDebug($"Statistics updated - Total: {stats.TotalFavorites}, Notifications: {stats.NotificationEnabled}, Activities: {stats.ActivityFavorites}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating statistics");
            }
        }

        private void UpdateEmptyState()
        {
            HasFavorites = FavoriteSpots.Any();
            
            _logger?.LogDebug($"Empty state updated - HasFavorites: {HasFavorites}");
        }

        private void SetEmptyState()
        {
            FavoriteSpots.Clear();
            FilteredFavoriteSpots.Clear();
            FavoriteStats = null;
            UpdateEmptyState();
        }

        private void SetError(string message)
        {
            IsError = true;
            ErrorMessage = message;
            SetEmptyState();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _loadingSemaphore?.Dispose();
                PropertyChanged -= OnPropertyChanged;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Statistics for favorite spots
    /// </summary>
    public class FavoriteStats
    {
        public int TotalFavorites { get; set; }
        public int NotificationEnabled { get; set; }
        public int ActivityFavorites { get; set; }
    }
}