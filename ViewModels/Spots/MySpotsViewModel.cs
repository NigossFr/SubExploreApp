using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace SubExplore.ViewModels.Spots
{
    public partial class MySpotsViewModel : ViewModelBase
    {
        private readonly ISpotRepository _spotRepository;
        private readonly ISpotService _spotService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly ILogger<MySpotsViewModel> _logger;

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
            ISpotRepository spotRepository,
            ISpotService spotService,
            IAuthenticationService authenticationService,
            IErrorHandlingService errorHandlingService,
            ILogger<MySpotsViewModel> logger,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _spotRepository = spotRepository ?? throw new ArgumentNullException(nameof(spotRepository));
            _spotService = spotService ?? throw new ArgumentNullException(nameof(spotService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Title = "Mes Spots";
            MySpots = new ObservableCollection<Spot>();
            FilteredSpots = new ObservableCollection<Spot>();
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            // Prevent duplicate initialization
            if (_isInitialized)
            {
                _logger.LogInformation("MySpotsViewModel already initialized, skipping");
                return;
            }

            try
            {
                _logger.LogInformation("MySpotsViewModel.InitializeAsync starting");
                await LoadMySpots();
                _isInitialized = true;
                _logger.LogInformation("MySpotsViewModel.InitializeAsync completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing MySpotsViewModel");
                // Don't show error here if LoadMySpots already handled it
                // Only show error if it's a different exception
                if (MySpots.Count == 0)
                {
                    await HandleErrorAsync("Erreur lors du chargement", "Une erreur est survenue lors du chargement de vos spots.");
                }
                else
                {
                    _logger.LogWarning("Exception in InitializeAsync but {Count} spots were loaded", MySpots.Count);
                }
            }
        }

        /// <summary>
        /// Load user's spots from repository
        /// </summary>
        [RelayCommand]
        private async Task LoadMySpots()
        {
            // Prevent concurrent database operations
            if (!await _loadingSemaphore.WaitAsync(100))
            {
                _logger.LogWarning("LoadMySpots already in progress, skipping duplicate call");
                return;
            }

            try
            {
                IsLoading = true;
                _logger.LogInformation("Starting LoadMySpots, IsAuthenticated: {IsAuthenticated}", _authenticationService.IsAuthenticated);
                
                // Enhanced authentication check with retry
                if (!_authenticationService.IsAuthenticated || _authenticationService.CurrentUser == null)
                {
                    _logger.LogWarning("User not authenticated when loading spots. Attempting to initialize authentication...");
                    
                    // Try to initialize authentication if not already done
                    try
                    {
                        await _authenticationService.InitializeAsync();
                        _logger.LogInformation("Authentication re-initialized. IsAuthenticated: {IsAuthenticated}", _authenticationService.IsAuthenticated);
                    }
                    catch (Exception authEx)
                    {
                        _logger.LogWarning(authEx, "Failed to re-initialize authentication");
                    }
                    
                    // Check again after initialization attempt
                    if (!_authenticationService.IsAuthenticated || _authenticationService.CurrentUser == null)
                    {
                        await ShowAlertAsync("Authentification requise", 
                            "Vous devez être connecté pour voir vos spots. Veuillez vous connecter d'abord.", "OK");
                        return;
                    }
                }
                
                var userId = _authenticationService.CurrentUser.Id;
                _logger.LogInformation("Loading spots for user {UserId} (Username: {Username})", 
                    userId, _authenticationService.CurrentUser.Username);

                var userSpots = await _spotRepository.GetSpotsByUserAsync(userId);
                _logger.LogInformation("Repository returned {Count} spots for user {UserId}", userSpots?.Count() ?? 0, userId);

                // Ensure UI updates happen on the main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MySpots.Clear();
                    foreach (var spot in userSpots.OrderByDescending(s => s.CreatedAt))
                    {
                        MySpots.Add(spot);
                    }

                    UpdateStatistics();
                    ApplyFilters();
                });

                _logger.LogInformation("Successfully loaded {Count} spots for user {Username}", 
                    MySpots.Count, _authenticationService.CurrentUser.Username);
                    
                // If we have no spots, this might be a new user
                if (MySpots.Count == 0)
                {
                    _logger.LogInformation("No spots found for user {UserId}. This might be a new user.", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user spots for user {UserId}", 
                    _authenticationService.CurrentUser?.Id);
                
                // Log exception details for debugging
                await _errorHandlingService.LogExceptionAsync(ex, nameof(LoadMySpots));
                
                // Show error dialog - this is likely a real error
                await HandleErrorAsync("Erreur de chargement", 
                    $"Impossible de charger vos spots. Détails: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                _loadingSemaphore.Release();
            }
        }

        /// <summary>
        /// Apply search and filter criteria
        /// </summary>
        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnSelectedFilterTypeChanged(string value)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (MySpots == null)
                return;

            var filtered = MySpots.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                filtered = filtered.Where(s => 
                    s.Name.ToLowerInvariant().Contains(searchLower) ||
                    s.Description.ToLowerInvariant().Contains(searchLower) ||
                    s.Type?.Name?.ToLowerInvariant().Contains(searchLower) == true);
            }

            // Apply type filter
            filtered = SelectedFilterType switch
            {
                "Validés" => filtered.Where(s => s.ValidationStatus == SpotValidationStatus.Approved),
                "En attente" => filtered.Where(s => s.ValidationStatus == SpotValidationStatus.Pending),
                "Rejetés" => filtered.Where(s => s.ValidationStatus == SpotValidationStatus.Rejected),
                _ => filtered // "Tous"
            };

            FilteredSpots.Clear();
            foreach (var spot in filtered)
            {
                FilteredSpots.Add(spot);
            }

            HasSpots = FilteredSpots.Any();
            ShowEmptyState = !HasSpots && !IsLoading;
        }

        /// <summary>
        /// Update statistics counters
        /// </summary>
        private void UpdateStatistics()
        {
            TotalSpotsCount = MySpots.Count;
            ValidatedSpotsCount = MySpots.Count(s => s.ValidationStatus == SpotValidationStatus.Approved);
            PendingSpotsCount = MySpots.Count(s => s.ValidationStatus == SpotValidationStatus.Pending);
            RejectedSpotsCount = MySpots.Count(s => s.ValidationStatus == SpotValidationStatus.Rejected);
        }

        /// <summary>
        /// Navigate to spot details
        /// </summary>
        [RelayCommand]
        private async Task ViewSpotDetails(Spot spot)
        {
            if (spot == null)
            {
                _logger.LogWarning("ViewSpotDetails called with null spot");
                return;
            }

            try
            {
                _logger.LogInformation("Navigating to spot details for spot ID: {SpotId}", spot.Id);
                await NavigateToAsync<SpotDetailsViewModel>(spot.Id.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to spot details for spot {SpotId}", spot.Id);
                await HandleErrorAsync("Erreur de navigation", "Impossible d'ouvrir les détails du spot.");
            }
        }

        /// <summary>
        /// Edit spot information
        /// </summary>
        [RelayCommand]
        private async Task EditSpot(Spot spot)
        {
            if (spot == null)
                return;

            try
            {
                _logger.LogInformation("Navigating to edit spot {SpotId}: {SpotName}", spot.Id, spot.Name);
                
                // Create navigation parameter for editing
                var editParameter = new SubExplore.Models.Navigation.SpotNavigationParameter(
                    spot.Id, 
                    spot.Name, 
                    spot.Latitude, 
                    spot.Longitude);
                
                // Navigate to AddSpotPage in edit mode
                await NavigateToAsync<AddSpotViewModel>(editParameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to edit spot {SpotId}", spot.Id);
                await HandleErrorAsync("Erreur", "Impossible d'ouvrir l'édition du spot.");
            }
        }

        /// <summary>
        /// Delete a spot with confirmation
        /// </summary>
        [RelayCommand]
        private async Task DeleteSpot(Spot spot)
        {
            if (spot == null)
                return;

            try
            {
                var confirmed = await ShowConfirmationAsync(
                    "Supprimer le spot",
                    $"Êtes-vous sûr de vouloir supprimer le spot '{spot.Name}' ? Cette action est irréversible.",
                    "Supprimer",
                    "Annuler");

                if (confirmed)
                {
                    IsLoading = true;
                    
                    await _spotRepository.DeleteAsync(spot);
                    
                    MySpots.Remove(spot);
                    UpdateStatistics();
                    ApplyFilters();
                    
                    await ShowToastAsync($"Spot '{spot.Name}' supprimé avec succès");
                    
                    _logger.LogInformation("User deleted spot {SpotId}", spot.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting spot {SpotId}", spot.Id);
                await _errorHandlingService.LogExceptionAsync(ex, nameof(DeleteSpot));
                await HandleErrorAsync("Erreur de suppression", $"Impossible de supprimer le spot '{spot.Name}'.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Change spot validation status
        /// </summary>
        [RelayCommand]
        private async Task ChangeSpotStatus(Spot spot)
        {
            if (spot == null)
                return;

            try
            {
                // For now, just show a message - this would typically be an admin function
                await ShowToastAsync($"Statut du spot: {spot.ValidationStatus}");
                
                _logger.LogInformation("User viewed status for spot {SpotId}: {Status}", spot.Id, spot.ValidationStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing status for spot {SpotId}", spot.Id);
                await HandleErrorAsync("Erreur", "Impossible d'afficher le statut du spot.");
            }
        }

        /// <summary>
        /// Navigate to add new spot
        /// </summary>
        [RelayCommand]
        private async Task AddNewSpot()
        {
            try
            {
                await NavigateToAsync<AddSpotViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to add spot");
                await HandleErrorAsync("Erreur de navigation", "Impossible d'ouvrir la page d'ajout de spot.");
            }
        }

        /// <summary>
        /// Refresh the spots list
        /// </summary>
        [RelayCommand]
        private async Task RefreshSpots()
        {
            await LoadMySpots();
        }

        /// <summary>
        /// Clear search and filters
        /// </summary>
        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedFilterType = "Tous";
        }

        /// <summary>
        /// Handle error with user-friendly messaging
        /// </summary>
        private async Task HandleErrorAsync(string title, string message)
        {
            IsError = true;
            ErrorMessage = message;
            await ShowAlertAsync(title, message, "OK");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                MySpots?.Clear();
                FilteredSpots?.Clear();
                _loadingSemaphore?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}