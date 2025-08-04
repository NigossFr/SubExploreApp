using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Models.Validation;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using System.Collections.ObjectModel;

namespace SubExplore.ViewModels.Admin
{
    /// <summary>
    /// ViewModel for spot validation administration
    /// Available to ExpertModerator and Administrator roles
    /// </summary>
    public partial class SpotValidationViewModel : AuthorizedViewModelBase
    {
        private readonly ISpotValidationService _spotValidationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;

        /// <summary>
        /// Navigation service for page navigation
        /// </summary>
        protected INavigationService NavigationService => _navigationService;

        /// <summary>
        /// Current authenticated user
        /// </summary>
        protected User? CurrentUser => _authenticationService.CurrentUser;

        [ObservableProperty]
        private ObservableCollection<Spot> pendingSpots = new();

        [ObservableProperty]
        private ObservableCollection<Spot> spotsUnderReview = new();

        [ObservableProperty]
        private ObservableCollection<Spot> safetyFlaggedSpots = new();

        [ObservableProperty]
        private Spot? selectedSpot;

        [ObservableProperty]
        private Models.Validation.SpotValidationStats? validationStats;

        [ObservableProperty]
        private int selectedTabIndex = 0;

        [ObservableProperty]
        private string validationNotes = string.Empty;

        [ObservableProperty]
        private bool isValidationInProgress = false;

        public SpotValidationViewModel(
            ISpotValidationService spotValidationService,
            IAuthorizationService authorizationService,
            IAuthenticationService authenticationService,
            IDialogService dialogService,
            INavigationService navigationService) 
            : base(authorizationService, authenticationService)
        {
            _spotValidationService = spotValidationService;
            _authenticationService = authenticationService;
            _dialogService = dialogService;
            _navigationService = navigationService;
            
            Title = "Validation des Spots";
        }

        public override async Task InitializeAsync(object? parameter = null)
        {
            try
            {
                await base.InitializeAsync(parameter);
                
                // Vérifier les permissions d'accès
                if (CurrentUser?.AccountType != AccountType.ExpertModerator && 
                    CurrentUser?.AccountType != AccountType.Administrator)
                {
                    await _dialogService.ShowAlertAsync("Accès refusé", 
                        "Vous n'avez pas les permissions nécessaires pour accéder à cette page.", "D'accord");
                    await NavigationService.GoBackAsync();
                    return;
                }

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] InitializeAsync error: {ex.Message}");
                IsError = true;
                ErrorMessage = "Erreur lors de l'initialisation de la page de validation.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ChangeTab(object tabIndex)
        {
            if (tabIndex is string tabIndexStr && int.TryParse(tabIndexStr, out int index))
            {
                SelectedTabIndex = index;
            }
            else if (tabIndex is int directIndex)
            {
                SelectedTabIndex = directIndex;
            }
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                IsError = false;

                // Charger les données en parallèle
                var pendingTask = _spotValidationService.GetPendingValidationSpotsAsync();
                var underReviewTask = _spotValidationService.GetSpotsUnderReviewAsync(CurrentUser?.Id ?? 0);
                var safetyTask = _spotValidationService.GetSpotsFlaggedForSafetyAsync();
                var statsTask = _spotValidationService.GetValidationStatsAsync();

                await Task.WhenAll(pendingTask, underReviewTask, safetyTask, statsTask);

                var pendingResult = await pendingTask;
                var underReviewResult = await underReviewTask;
                var safetyResult = await safetyTask;
                var statsResult = await statsTask;
                
                PendingSpots = new ObservableCollection<Spot>(pendingResult.Success ? pendingResult.Data ?? new List<Spot>() : new List<Spot>());
                SpotsUnderReview = new ObservableCollection<Spot>(underReviewResult.Success ? underReviewResult.Data ?? new List<Spot>() : new List<Spot>());
                SafetyFlaggedSpots = new ObservableCollection<Spot>(safetyResult.Success ? safetyResult.Data ?? new List<Spot>() : new List<Spot>());
                ValidationStats = statsResult.Success ? statsResult.Data : null;

                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] Loaded: {PendingSpots.Count} pending, {SpotsUnderReview.Count} under review, {SafetyFlaggedSpots.Count} safety flagged");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] LoadDataAsync error: {ex.Message}");
                IsError = true;
                ErrorMessage = "Erreur lors du chargement des données de validation.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SelectSpotAsync(Spot spot)
        {
            if (spot == null) return;

            try
            {
                SelectedSpot = spot;
                ValidationNotes = string.Empty;

                // Charger l'historique de validation
                var historyResult = await _spotValidationService.GetSpotValidationHistoryAsync(spot.Id);
                var historyCount = historyResult.Success ? historyResult.Data?.Count ?? 0 : 0;
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] Selected spot {spot.Id} - {historyCount} history entries");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] SelectSpotAsync error: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de la sélection du spot.", "D'accord");
            }
        }

        [RelayCommand]
        private async Task ApproveSpotAsync(Spot? spot = null)
        {
            var spotToApprove = spot ?? SelectedSpot;
            if (spotToApprove == null || CurrentUser == null) return;

            SelectedSpot = spotToApprove;
            await ValidateSpotAsync(SpotValidationStatus.Approved, "Spot approuvé");
        }

        [RelayCommand]
        private async Task RejectSpotAsync(Spot? spot = null)
        {
            var spotToReject = spot ?? SelectedSpot;
            if (spotToReject == null || CurrentUser == null) return;

            SelectedSpot = spotToReject;

            // Demander une raison pour le rejet
            string reason = await _dialogService.ShowPromptAsync(
                "Raison du rejet", 
                "Veuillez indiquer pourquoi ce spot est rejeté:", 
                "D'accord", 
                "Annuler", 
                "Raison du rejet...");

            if (string.IsNullOrWhiteSpace(reason))
            {
                return; // L'utilisateur a annulé
            }

            ValidationNotes = reason;
            await ValidateSpotAsync(SpotValidationStatus.Rejected, reason);
        }

        [RelayCommand]
        private async Task AssignForReviewAsync(Spot? spot = null)
        {
            var spotToAssign = spot ?? SelectedSpot;
            if (spotToAssign == null || CurrentUser == null) return;

            try
            {
                IsValidationInProgress = true;

                var result = await _spotValidationService.AssignSpotForReviewAsync(spotToAssign.Id, CurrentUser.Id);
                
                if (result.Success)
                {
                    await _dialogService.ShowAlertAsync("Succès", "Le spot a été assigné pour révision.", "D'accord");
                    await LoadDataAsync();
                    SelectedSpot = null;
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Erreur", result.ErrorMessage ?? "Impossible d'assigner le spot pour révision.", "D'accord");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] AssignForReviewAsync error: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de l'assignation du spot.", "D'accord");
            }
            finally
            {
                IsValidationInProgress = false;
            }
        }

        [RelayCommand]
        private async Task CompleteSafetyReviewAsync(string parameter)
        {
            if (SelectedSpot == null || CurrentUser == null) return;

            if (!bool.TryParse(parameter, out bool isSafe))
                return;

            try
            {
                IsValidationInProgress = true;

                // Demander des notes optionnelles
                string reviewNotes = await _dialogService.ShowPromptAsync(
                    "Notes de révision", 
                    $"Ajoutez des notes sur cette évaluation de sécurité (optionnel):", 
                    "D'accord", 
                    "Passer", 
                    "Notes...") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(reviewNotes))
                {
                    reviewNotes = isSafe ? "Spot considéré comme sûr" : "Problème de sécurité identifié";
                }

                var safetyReviewResult = new SafetyReviewResult
                {
                    IsSafe = isSafe,
                    ReviewNotes = reviewNotes,
                    RiskLevel = isSafe ? SafetyRiskLevel.Low : SafetyRiskLevel.High
                };
                var result = await _spotValidationService.CompleteSafetyReviewAsync(
                    SelectedSpot.Id, CurrentUser.Id, safetyReviewResult);
                
                if (result.Success)
                {
                    var message = isSafe ? "Le spot a été marqué comme sûr." : "Le spot a été marqué comme dangereux.";
                    await _dialogService.ShowAlertAsync("Succès", message, "D'accord");
                    await LoadDataAsync();
                    SelectedSpot = null;
                    ValidationNotes = string.Empty;
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Erreur", result.ErrorMessage ?? "Impossible de compléter la révision de sécurité.", "D'accord");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] CompleteSafetyReviewAsync error: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de la révision de sécurité.", "D'accord");
            }
            finally
            {
                IsValidationInProgress = false;
            }
        }

        [RelayCommand]
        private async Task ViewSpotDetailsAsync(Spot spot)
        {
            if (spot == null) return;

            try
            {
                // Naviguer vers les détails du spot
                var parameter = new Models.Navigation.SpotNavigationParameter(spot.Id, spot.Name, spot.Latitude, spot.Longitude);

                await NavigationService.NavigateToAsync<ViewModels.Spots.SpotDetailsViewModel>(parameter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] ViewSpotDetailsAsync error: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de l'ouverture des détails du spot.", "D'accord");
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            try
            {
                await NavigationService.GoBackAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] GoBackAsync error: {ex.Message}");
                // Try to go to home as fallback
                await GoToHomeAsync();
            }
        }

        [RelayCommand]
        private async Task GoToHomeAsync()
        {
            try
            {
                await NavigationService.GoToHomeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] GoToHomeAsync error: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", "Impossible de retourner à l'accueil.", "D'accord");
            }
        }

        private async Task ValidateSpotAsync(SpotValidationStatus status, string notes)
        {
            if (SelectedSpot == null || CurrentUser == null) return;

            try
            {
                IsValidationInProgress = true;

                var validationNotes = string.IsNullOrWhiteSpace(ValidationNotes) ? notes : ValidationNotes;
                
                // Use command pattern for validation
                IValidationCommand command = status == SpotValidationStatus.Approved 
                    ? new ApproveSpotCommand(SelectedSpot.Id, CurrentUser.Id, validationNotes)
                    : new RejectSpotCommand(SelectedSpot.Id, CurrentUser.Id, validationNotes);
                
                var result = await _spotValidationService.ExecuteValidationCommandAsync(command);
                
                if (result.Success)
                {
                    var message = status == SpotValidationStatus.Approved 
                        ? "Le spot a été approuvé avec succès." 
                        : "Le spot a été rejeté.";
                    
                    await _dialogService.ShowAlertAsync("Succès", message, "D'accord");
                    await LoadDataAsync();
                    SelectedSpot = null;
                    ValidationNotes = string.Empty;
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Erreur", result.ErrorMessage ?? "Impossible de valider le spot.", "D'accord");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] ValidateSpotAsync error: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de la validation du spot.", "D'accord");
            }
            finally
            {
                IsValidationInProgress = false;
            }
        }

        public string GetValidationStatusText(SpotValidationStatus status)
        {
            return status switch
            {
                SpotValidationStatus.Pending => "En attente",
                SpotValidationStatus.UnderReview => "En cours de révision",
                SpotValidationStatus.Approved => "Approuvé",
                SpotValidationStatus.Rejected => "Rejeté",
                SpotValidationStatus.SafetyReview => "Révision de sécurité",
                _ => "Inconnu"
            };
        }

        public Color GetValidationStatusColor(SpotValidationStatus status)
        {
            return status switch
            {
                SpotValidationStatus.Pending => Colors.Orange,
                SpotValidationStatus.UnderReview => Colors.Blue,
                SpotValidationStatus.Approved => Colors.Green,
                SpotValidationStatus.Rejected => Colors.Red,
                SpotValidationStatus.SafetyReview => Colors.Purple,
                _ => Colors.Gray
            };
        }
    }
}