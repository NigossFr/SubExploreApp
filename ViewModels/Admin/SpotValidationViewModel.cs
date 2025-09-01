using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Models.Validation;
using SubExplore.Services.Implementations;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using SubExplore.ViewModels.Spots;
using System.Collections.ObjectModel;

namespace SubExplore.ViewModels.Admin
{
    /// <summary>
    /// ViewModel for spot validation administration - Supabase compatible
    /// Available to ExpertModerator and Administrator roles
    /// </summary>
    public partial class SpotValidationViewModel : AuthorizedViewModelBase
    {
        private readonly ISpotValidationService _spotValidationService;
        private readonly ISimpleAuthenticationService _authenticationService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;
        private readonly ILogger<SpotValidationViewModel>? _logger;

        // Concurrency control
        private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);
        private bool _isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<Spot> _pendingSpots = new();

        [ObservableProperty]
        private ObservableCollection<Spot> _spotsUnderReview = new();

        [ObservableProperty]
        private ObservableCollection<Spot> _safetyFlaggedSpots = new();

        [ObservableProperty]
        private Spot? _selectedSpot;

        [ObservableProperty]
        private SpotValidationStats? _validationStats;

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        [ObservableProperty]
        private string _validationNotes = string.Empty;

        [ObservableProperty]
        private bool _isValidationInProgress = false;

        [ObservableProperty]
        private bool _isRefreshing = false;

        public SpotValidationViewModel(
            ISpotValidationService spotValidationService,
            ISimpleAuthenticationService authenticationService,
            IDialogService dialogService,
            INavigationService navigationService,
            IAuthorizationService authorizationService,
            ILogger<SpotValidationViewModel>? logger = null)
            : base(authorizationService, authenticationService, dialogService, navigationService)
        {
            _spotValidationService = spotValidationService ?? throw new ArgumentNullException(nameof(spotValidationService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _logger = logger;

            Title = "Validation des Spots";
        }

        public override async Task InitializeAsync(object? parameter = null)
        {
            // Prevent duplicate initialization
            if (_isInitialized)
            {
                _logger?.LogInformation("SpotValidationViewModel already initialized, skipping");
                return;
            }

            try
            {
                await _loadingSemaphore.WaitAsync();
                
                if (_isInitialized) return; // Double-check
                
                await LoadValidationDataAsync();
                _isInitialized = true;
                
                _logger?.LogInformation("SpotValidationViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing SpotValidationViewModel");
                SetError("Impossible de charger les données de validation.");
            }
            finally
            {
                _loadingSemaphore.Release();
            }
        }

        [RelayCommand]
        private async Task LoadValidationDataAsync()
        {
            try
            {
                IsLoading = true;
                IsError = false;
                ErrorMessage = string.Empty;

                // Check authentication
                if (!_authenticationService.IsAuthenticated)
                {
                    _logger?.LogWarning("User not authenticated for validation access");
                    SetError("Vous devez être connecté pour accéder à la validation.");
                    return;
                }

                var currentUser = _authenticationService.CurrentUser;
                if (currentUser?.Id == null)
                {
                    _logger?.LogWarning("No current user available");
                    SetError("Utilisateur introuvable.");
                    return;
                }

                // Load all validation data in parallel
                var pendingTask = _spotValidationService.GetPendingValidationSpotsAsync();
                var reviewTask = _spotValidationService.GetSpotsUnderReviewAsync(currentUser.Id);
                var safetyTask = _spotValidationService.GetSpotsFlaggedForSafetyAsync();
                var statsTask = _spotValidationService.GetValidationStatsAsync();

                await Task.WhenAll(pendingTask, reviewTask, safetyTask, statsTask);

                // Update UI on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Update pending spots
                    if (pendingTask.Result.Success && pendingTask.Result.Data != null)
                    {
                        PendingSpots.Clear();
                        foreach (var spot in pendingTask.Result.Data)
                        {
                            PendingSpots.Add(spot);
                        }
                    }

                    // Update spots under review
                    if (reviewTask.Result.Success && reviewTask.Result.Data != null)
                    {
                        SpotsUnderReview.Clear();
                        foreach (var spot in reviewTask.Result.Data)
                        {
                            SpotsUnderReview.Add(spot);
                        }
                    }

                    // Update safety flagged spots
                    if (safetyTask.Result.Success && safetyTask.Result.Data != null)
                    {
                        SafetyFlaggedSpots.Clear();
                        foreach (var spot in safetyTask.Result.Data)
                        {
                            SafetyFlaggedSpots.Add(spot);
                        }
                    }

                    // Update statistics
                    if (statsTask.Result.Success && statsTask.Result.Data != null)
                    {
                        ValidationStats = statsTask.Result.Data;
                    }
                });

                _logger?.LogInformation($"Validation data loaded - Pending: {PendingSpots.Count}, Review: {SpotsUnderReview.Count}, Safety: {SafetyFlaggedSpots.Count}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading validation data");
                SetError("Erreur lors du chargement des données de validation.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshValidationData()
        {
            try
            {
                IsRefreshing = true;
                await LoadValidationDataAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task ApproveSpot(Spot spot)
        {
            if (spot == null) return;

            try
            {
                var confirm = await _dialogService.ShowConfirmationAsync(
                    "Approuver le Spot",
                    $"Voulez-vous approuver le spot '{spot.Name}' ?",
                    "Approuver",
                    "Annuler");

                if (!confirm) return;

                IsValidationInProgress = true;

                var currentUser = _authenticationService.CurrentUser;
                if (currentUser?.Id == null)
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Utilisateur introuvable.", "OK");
                    return;
                }

                // Use the service method if it exists, otherwise use simple validation service cast
                if (_spotValidationService is SupabaseSpotValidationService supabaseService)
                {
                    var result = await supabaseService.ApproveSpotAsync(spot.Id, currentUser.Id, ValidationNotes);
                    if (result.Success)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            PendingSpots.Remove(spot);
                            SpotsUnderReview.Remove(spot);
                        });

                        await _dialogService.ShowAlertAsync("Succès", "Spot approuvé avec succès.", "OK");
                        await RefreshValidationData();
                    }
                    else
                    {
                        await _dialogService.ShowAlertAsync("Erreur", result.ErrorMessage ?? "Erreur lors de l'approbation.", "OK");
                    }
                }
                
                _logger?.LogInformation($"Spot {spot.Id} approved by {currentUser.Id}");
                ValidationNotes = string.Empty;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error approving spot {spot?.Id}");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de l'approbation du spot.", "OK");
            }
            finally
            {
                IsValidationInProgress = false;
            }
        }

        [RelayCommand]
        private async Task RejectSpot(Spot spot)
        {
            if (spot == null) return;

            try
            {
                var confirm = await _dialogService.ShowConfirmationAsync(
                    "Rejeter le Spot",
                    $"Voulez-vous rejeter le spot '{spot.Name}' ?",
                    "Rejeter",
                    "Annuler");

                if (!confirm) return;

                IsValidationInProgress = true;

                var currentUser = _authenticationService.CurrentUser;
                if (currentUser?.Id == null)
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Utilisateur introuvable.", "OK");
                    return;
                }

                // Use the service method if it exists
                if (_spotValidationService is SupabaseSpotValidationService supabaseService)
                {
                    var result = await supabaseService.RejectSpotAsync(spot.Id, currentUser.Id, ValidationNotes);
                    if (result.Success)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            PendingSpots.Remove(spot);
                            SpotsUnderReview.Remove(spot);
                        });

                        await _dialogService.ShowAlertAsync("Succès", "Spot rejeté avec succès.", "OK");
                        await RefreshValidationData();
                    }
                    else
                    {
                        await _dialogService.ShowAlertAsync("Erreur", result.ErrorMessage ?? "Erreur lors du rejet.", "OK");
                    }
                }
                
                _logger?.LogInformation($"Spot {spot.Id} rejected by {currentUser.Id}");
                ValidationNotes = string.Empty;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error rejecting spot {spot?.Id}");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors du rejet du spot.", "OK");
            }
            finally
            {
                IsValidationInProgress = false;
            }
        }

        [RelayCommand]
        private async Task AssignForReview(Spot spot)
        {
            if (spot == null) return;

            try
            {
                IsValidationInProgress = true;

                var currentUser = _authenticationService.CurrentUser;
                if (currentUser?.Id == null)
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Utilisateur introuvable.", "OK");
                    return;
                }

                var result = await _spotValidationService.AssignSpotForReviewAsync(spot.Id, currentUser.Id);
                if (result.Success)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        PendingSpots.Remove(spot);
                        SpotsUnderReview.Add(spot);
                    });

                    await _dialogService.ShowAlertAsync("Succès", "Spot assigné pour révision.", "OK");
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Erreur", result.ErrorMessage ?? "Erreur lors de l'assignation.", "OK");
                }
                
                _logger?.LogInformation($"Spot {spot.Id} assigned for review to {currentUser.Id}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error assigning spot for review {spot?.Id}");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de l'assignation du spot.", "OK");
            }
            finally
            {
                IsValidationInProgress = false;
            }
        }

        [RelayCommand]
        private async Task MarkSpotSafe(Spot spot)
        {
            await CompleteSafetyReviewForSpot(spot, true);
        }

        [RelayCommand]
        private async Task MarkSpotUnsafe(Spot spot)
        {
            await CompleteSafetyReviewForSpot(spot, false);
        }

        private async Task CompleteSafetyReviewForSpot(Spot spot, bool isSafe)
        {
            if (spot == null) return;

            try
            {
                var confirm = await _dialogService.ShowConfirmationAsync(
                    "Révision de Sécurité",
                    $"Confirmer que le spot '{spot.Name}' est {(isSafe ? "sûr" : "dangereux")} ?",
                    "Confirmer",
                    "Annuler");

                if (!confirm) return;

                IsValidationInProgress = true;

                var currentUser = _authenticationService.CurrentUser;
                if (currentUser?.Id == null)
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Utilisateur introuvable.", "OK");
                    return;
                }

                var reviewResult = new Models.Validation.SafetyReviewResult
                {
                    IsSafe = isSafe,
                    ReviewNotes = ValidationNotes,
                    RiskLevel = isSafe ? Models.Validation.SafetyRiskLevel.Low : Models.Validation.SafetyRiskLevel.High
                };

                var result = await _spotValidationService.CompleteSafetyReviewAsync(spot.Id, currentUser.Id, reviewResult);
                if (result.Success)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        SafetyFlaggedSpots.Remove(spot);
                    });

                    await _dialogService.ShowAlertAsync("Succès", $"Révision de sécurité terminée - spot marqué comme {(isSafe ? "sûr" : "dangereux")}.", "OK");
                    await RefreshValidationData();
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Erreur", result.ErrorMessage ?? "Erreur lors de la révision de sécurité.", "OK");
                }
                
                _logger?.LogInformation($"Safety review completed for spot {spot.Id} by {currentUser.Id} - Safe: {isSafe}");
                ValidationNotes = string.Empty;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error completing safety review for spot {spot?.Id}");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de la révision de sécurité.", "OK");
            }
            finally
            {
                IsValidationInProgress = false;
            }
        }

        [RelayCommand]
        private async Task ViewSpotDetails(Spot spot)
        {
            if (spot == null) return;

            try
            {
                _logger?.LogInformation($"Navigating to spot details for validation - Spot ID: {spot.Id}");
                
                // ✅ FIXED: Use NavigationService with validation parameters
                var validationParameter = new Dictionary<string, object>
                {
                    ["SpotId"] = spot.Id,
                    ["IsValidationMode"] = true,
                    ["ValidationMode"] = "Admin"
                };
                
                await _navigationService.NavigateToAsync<ViewModels.Spots.SpotDetailsViewModel>(validationParameter);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error navigating to spot details for validation: {spot?.Id}");
                await _dialogService.ShowAlertAsync("Erreur", "Impossible d'ouvrir les détails du spot.", "OK");
            }
        }

        [RelayCommand]
        private async Task CreateTestData()
        {
            try
            {
                var confirm = await _dialogService.ShowConfirmationAsync(
                    "Créer des données de test",
                    "Voulez-vous créer des spots de test pour la validation ?",
                    "Créer",
                    "Annuler");

                if (!confirm) return;

                IsLoading = true;

                // Create test spots through the Supabase service
                // For now, we'll create a simple test spot directly
                await _dialogService.ShowAlertAsync("Info", "🧪 Création de données de test...\n\nCette fonctionnalité créerait des spots de test avec différents statuts de validation pour tester l'interface admin.", "OK");
                
                // Refresh data to show any changes
                await RefreshValidationData();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating test data");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de la création des données de test.", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SetError(string message)
        {
            IsError = true;
            ErrorMessage = message;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _loadingSemaphore?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}