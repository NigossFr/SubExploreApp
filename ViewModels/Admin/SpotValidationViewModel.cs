using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
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
        private SpotValidationStats? validationStats;

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
                        "Vous n'avez pas les permissions nécessaires pour accéder à cette page.", "OK");
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

                PendingSpots = new ObservableCollection<Spot>(await pendingTask);
                SpotsUnderReview = new ObservableCollection<Spot>(await underReviewTask);
                SafetyFlaggedSpots = new ObservableCollection<Spot>(await safetyTask);
                ValidationStats = await statsTask;

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
                var history = await _spotValidationService.GetSpotValidationHistoryAsync(spot.Id);
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] Selected spot {spot.Id} - {history.Count} history entries");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] SelectSpotAsync error: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de la sélection du spot.", "OK");
            }
        }

        [RelayCommand]
        private async Task ApproveSpotAsync()
        {
            if (SelectedSpot == null || CurrentUser == null) return;

            await ValidateSpotAsync(SpotValidationStatus.Approved, "Spot approuvé");
        }

        [RelayCommand]
        private async Task RejectSpotAsync()
        {
            if (SelectedSpot == null || CurrentUser == null) return;

            if (string.IsNullOrWhiteSpace(ValidationNotes))
            {
                await _dialogService.ShowAlertAsync("Notes requises", 
                    "Veuillez fournir une raison pour le rejet du spot.", "OK");
                return;
            }

            await ValidateSpotAsync(SpotValidationStatus.Rejected, ValidationNotes);
        }

        [RelayCommand]
        private async Task AssignForReviewAsync()
        {
            if (SelectedSpot == null || CurrentUser == null) return;

            try
            {
                IsValidationInProgress = true;

                var success = await _spotValidationService.AssignSpotForReviewAsync(SelectedSpot.Id, CurrentUser.Id);
                
                if (success)
                {
                    await _dialogService.ShowAlertAsync("Succès", "Le spot a été assigné pour révision.", "OK");
                    await LoadDataAsync();
                    SelectedSpot = null;
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Impossible d'assigner le spot pour révision.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] AssignForReviewAsync error: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de l'assignation du spot.", "OK");
            }
            finally
            {
                IsValidationInProgress = false;
            }
        }

        [RelayCommand]
        private async Task CompleteSafetyReviewAsync(bool isSafe)
        {
            if (SelectedSpot == null || CurrentUser == null) return;

            try
            {
                IsValidationInProgress = true;

                var reviewNotes = string.IsNullOrWhiteSpace(ValidationNotes) 
                    ? (isSafe ? "Spot considéré comme sûr" : "Problème de sécurité identifié")
                    : ValidationNotes;

                var success = await _spotValidationService.CompleteSafetyReviewAsync(
                    SelectedSpot.Id, CurrentUser.Id, isSafe, reviewNotes);
                
                if (success)
                {
                    var message = isSafe ? "Le spot a été marqué comme sûr." : "Le spot a été marqué comme dangereux.";
                    await _dialogService.ShowAlertAsync("Succès", message, "OK");
                    await LoadDataAsync();
                    SelectedSpot = null;
                    ValidationNotes = string.Empty;
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Impossible de compléter la révision de sécurité.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] CompleteSafetyReviewAsync error: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de la révision de sécurité.", "OK");
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
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de l'ouverture des détails du spot.", "OK");
            }
        }

        private async Task ValidateSpotAsync(SpotValidationStatus status, string notes)
        {
            if (SelectedSpot == null || CurrentUser == null) return;

            try
            {
                IsValidationInProgress = true;

                var validationNotes = string.IsNullOrWhiteSpace(ValidationNotes) ? notes : ValidationNotes;
                var success = await _spotValidationService.ValidateSpotAsync(
                    SelectedSpot.Id, CurrentUser.Id, status, validationNotes);
                
                if (success)
                {
                    var message = status == SpotValidationStatus.Approved 
                        ? "Le spot a été approuvé avec succès." 
                        : "Le spot a été rejeté.";
                    
                    await _dialogService.ShowAlertAsync("Succès", message, "OK");
                    await LoadDataAsync();
                    SelectedSpot = null;
                    ValidationNotes = string.Empty;
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Impossible de valider le spot.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationViewModel] ValidateSpotAsync error: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", "Erreur lors de la validation du spot.", "OK");
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