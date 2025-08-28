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

namespace SubExplore.ViewModels.Spots
{
    public partial class MySpotsViewModel : ViewModelBase
    {
        private readonly ISpotService _spotService;
        private readonly ISimpleAuthenticationService _authenticationService;
        private readonly ILogger<MySpotsViewModel>? _logger;
        
        // Concurrency control
        private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);
        private bool _isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<Spot> _mySpots;

        [ObservableProperty]
        private ObservableCollection<Spot> _filteredSpots;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasSpots;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedFilterType = "Tous";

        [ObservableProperty]
        private int _totalSpotsCount;

        [ObservableProperty]
        private int _validatedSpotsCount;

        [ObservableProperty]
        private int _pendingSpotsCount;

        [ObservableProperty]
        private int _rejectedSpotsCount;

        [ObservableProperty]
        private bool _showEmptyState;

        public ObservableCollection<string> FilterTypes { get; } = new()
        {
            "Tous",
            "Validés", 
            "En attente",
            "Rejetés"
        };

        public MySpotsViewModel(
            ISpotService spotService,
            ISimpleAuthenticationService authenticationService,
            IDialogService dialogService,
            INavigationService navigationService,
            ILogger<MySpotsViewModel>? logger = null)
            : base(dialogService, navigationService)
        {
            _spotService = spotService ?? throw new ArgumentNullException(nameof(spotService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _logger = logger;

            Title = "Mes Spots";
            MySpots = new ObservableCollection<Spot>();
            FilteredSpots = new ObservableCollection<Spot>();
            
            // Property change handlers
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchText) || e.PropertyName == nameof(SelectedFilterType))
            {
                ApplyFilters();
            }
        }

        public override async Task InitializeAsync(object? parameter = null)
        {
            // Prevent duplicate initialization
            if (_isInitialized)
            {
                _logger?.LogInformation("MySpotsViewModel already initialized, skipping");
                return;
            }

            try
            {
                await _loadingSemaphore.WaitAsync();
                
                if (_isInitialized) return; // Double-check
                
                await LoadMySpots();
                _isInitialized = true;
                
                _logger?.LogInformation("MySpotsViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing MySpotsViewModel");
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger vos spots.", "OK");
            }
            finally
            {
                _loadingSemaphore.Release();
            }
        }

        [RelayCommand]
        private async Task LoadMySpots()
        {
            try
            {
                IsLoading = true;

                // Check if user is authenticated
                if (!_authenticationService.IsAuthenticated)
                {
                    _logger?.LogWarning("User not authenticated, cannot load spots");
                    UpdateEmptyState();
                    return;
                }

                var currentUser = _authenticationService.CurrentUser;
                if (currentUser?.Id == null)
                {
                    _logger?.LogWarning("No current user available");
                    UpdateEmptyState();
                    return;
                }

                // Load user favorite spots as placeholder
                // TODO: Implement GetSpotsByUserIdAsync method in ISpotService
                var spots = await _spotService.GetUserFavoriteSpotsAsync(currentUser.Id);
                
                // Update collections on UI thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MySpots.Clear();
                    foreach (var spot in spots)
                    {
                        MySpots.Add(spot);
                    }
                    
                    ApplyFilters();
                    UpdateStatistics();
                    UpdateEmptyState();
                });

                _logger?.LogInformation($"Loaded {spots.Count()} spots for user {currentUser.Id}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading user spots");
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger vos spots.", "OK");
                UpdateEmptyState();
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshSpots()
        {
            await LoadMySpots();
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedFilterType = "Tous";
        }

        [RelayCommand]
        private async Task AddNewSpot()
        {
            try
            {
                // Navigate to add spot page
                await Shell.Current.GoToAsync("///map");
                
                _logger?.LogInformation("Navigating to map to add new spot");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error navigating to add spot");
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'ouvrir la page d'ajout de spot.", "OK");
            }
        }

        [RelayCommand]
        private async Task EditSpot(Spot spot)
        {
            if (spot == null) return;

            try
            {
                _logger?.LogInformation($"Editing spot {spot.Id}");
                
                // TODO: Navigate to edit spot page when available
                await DialogService.ShowAlertAsync("Information", "Fonctionnalité d'édition bientôt disponible.", "OK");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error editing spot {spot?.Id}");
                await DialogService.ShowAlertAsync("Erreur", "Impossible de modifier ce spot.", "OK");
            }
        }

        [RelayCommand]
        private async Task DeleteSpot(Spot spot)
        {
            if (spot == null) return;

            try
            {
                var confirm = await DialogService.ShowConfirmationAsync(
                    "Confirmer la suppression", 
                    $"Voulez-vous vraiment supprimer le spot '{spot.Name}' ?", 
                    "Supprimer", 
                    "Annuler");

                if (!confirm) return;

                IsLoading = true;
                
                // TODO: Implement DeleteSpotAsync method in ISpotService
                // For now, just remove from collection
                var success = true;
                if (success)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        MySpots.Remove(spot);
                        ApplyFilters();
                        UpdateStatistics();
                        UpdateEmptyState();
                    });

                    await DialogService.ShowAlertAsync("Succès", "Spot supprimé avec succès.", "OK");
                    _logger?.LogInformation($"Spot {spot.Id} deleted successfully");
                }
                else
                {
                    await DialogService.ShowAlertAsync("Erreur", "Impossible de supprimer ce spot.", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error deleting spot {spot?.Id}");
                await DialogService.ShowAlertAsync("Erreur", "Erreur lors de la suppression du spot.", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ViewSpotDetails(Spot spot)
        {
            if (spot == null) return;

            try
            {
                _logger?.LogInformation($"Viewing details for spot {spot.Id}");
                
                // TODO: Navigate to spot details page when available
                await DialogService.ShowAlertAsync("Information", $"Détails du spot '{spot.Name}' bientôt disponibles.", "OK");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error viewing spot details {spot?.Id}");
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'afficher les détails de ce spot.", "OK");
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = MySpots.AsEnumerable();

                // Apply text search
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLowerInvariant();
                    filtered = filtered.Where(s => 
                        s.Name.ToLowerInvariant().Contains(searchLower) ||
                        (s.Description?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (s.Type?.Name?.ToLowerInvariant().Contains(searchLower) ?? false));
                }

                // Apply status filter
                if (SelectedFilterType != "Tous")
                {
                    filtered = SelectedFilterType switch
                    {
                        "Validés" => filtered.Where(s => s.ValidationStatus == SpotValidationStatus.Approved),
                        "En attente" => filtered.Where(s => s.ValidationStatus == SpotValidationStatus.Pending),
                        "Rejetés" => filtered.Where(s => s.ValidationStatus == SpotValidationStatus.Rejected),
                        _ => filtered
                    };
                }

                // Update filtered collection on UI thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FilteredSpots.Clear();
                    foreach (var spot in filtered)
                    {
                        FilteredSpots.Add(spot);
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
                TotalSpotsCount = MySpots.Count;
                ValidatedSpotsCount = MySpots.Count(s => s.ValidationStatus == SpotValidationStatus.Approved);
                PendingSpotsCount = MySpots.Count(s => s.ValidationStatus == SpotValidationStatus.Pending);
                RejectedSpotsCount = MySpots.Count(s => s.ValidationStatus == SpotValidationStatus.Rejected);

                _logger?.LogDebug($"Statistics updated - Total: {TotalSpotsCount}, Validated: {ValidatedSpotsCount}, Pending: {PendingSpotsCount}, Rejected: {RejectedSpotsCount}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating statistics");
            }
        }

        private void UpdateEmptyState()
        {
            HasSpots = MySpots.Any();
            ShowEmptyState = !HasSpots && !IsLoading;
            
            _logger?.LogDebug($"Empty state updated - HasSpots: {HasSpots}, ShowEmptyState: {ShowEmptyState}");
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
}