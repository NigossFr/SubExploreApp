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
        private readonly IServiceProvider _serviceProvider;

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
            INavigationService navigationService,
            IServiceProvider serviceProvider)
            : base(dialogService, navigationService)
        {
            _spotRepository = spotRepository ?? throw new ArgumentNullException(nameof(spotRepository));
            _spotService = spotService ?? throw new ArgumentNullException(nameof(spotService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

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
                            "Vous devez être connecté pour voir vos spots. Veuillez vous connecter d'abord.", "D'accord");
                        return;
                    }
                }
                
                var userId = _authenticationService.CurrentUser.Id;
                _logger.LogInformation("🔍 Loading spots for user {UserId} (Username: {Username})", 
                    userId, _authenticationService.CurrentUser.Username);

                // Add detailed debugging for the repository call
                _logger.LogInformation("🔍 Calling _spotRepository.GetSpotsByUserAsync({UserId})", userId);
                
                var userSpots = await _spotRepository.GetSpotsByUserAsync(userId);
                var spotsList = userSpots?.ToList() ?? new List<Spot>();
                
                _logger.LogInformation("🔍 Repository returned {Count} spots for user {UserId}", spotsList.Count, userId);
                
                // Debug: Log details about each spot found
                if (spotsList.Any())
                {
                    _logger.LogInformation("🔍 Spots found:");
                    foreach (var spot in spotsList)
                    {
                        _logger.LogInformation("🔍   - Spot ID: {SpotId}, Name: {SpotName}, CreatorId: {CreatorId}, CreatedAt: {CreatedAt}", 
                            spot.Id, spot.Name, spot.CreatorId, spot.CreatedAt);
                    }
                }
                else
                {
                    _logger.LogWarning("🔍 No spots found for user {UserId}. Checking database connectivity and data...", userId);
                    
                    // Additional debugging: Check if there are any spots in the database at all
                    try
                    {
                        var allSpots = await _spotRepository.GetAllAsync();
                        var allSpotsList = allSpots?.ToList() ?? new List<Spot>();
                        _logger.LogInformation("🔍 Total spots in database: {TotalCount}", allSpotsList.Count);
                        
                        if (allSpotsList.Any())
                        {
                            _logger.LogInformation("🔍 Sample of spots in database:");
                            foreach (var spot in allSpotsList.Take(5))
                            {
                                _logger.LogInformation("🔍   - Spot ID: {SpotId}, Name: {SpotName}, CreatorId: {CreatorId}", 
                                    spot.Id, spot.Name, spot.CreatorId);
                            }
                            
                            // Count spots by creator ID
                            var spotsByCreator = allSpotsList.GroupBy(s => s.CreatorId)
                                .Select(g => new { CreatorId = g.Key, Count = g.Count() })
                                .ToList();
                                
                            _logger.LogInformation("🔍 Spots by creator ID:");
                            foreach (var group in spotsByCreator)
                            {
                                _logger.LogInformation("🔍   - Creator ID {CreatorId}: {Count} spots", group.CreatorId, group.Count);
                            }
                        }
                    }
                    catch (Exception debugEx)
                    {
                        _logger.LogError(debugEx, "🔍 Error during additional debugging queries");
                    }
                }

                // Ensure UI updates happen on the main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MySpots.Clear();
                    foreach (var spot in spotsList.OrderByDescending(s => s.CreatedAt))
                    {
                        MySpots.Add(spot);
                    }

                    UpdateStatistics();
                    ApplyFilters();
                });

                _logger.LogInformation("Successfully loaded {Count} spots for user {Username}", 
                    MySpots.Count, _authenticationService.CurrentUser.Username);

                // Auto-diagnose spot types for monitoring
                await AutoDiagnoseSpotTypesAsync();
                    
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
            {
                _logger.LogWarning("EditSpot called with null spot");
                return;
            }

            try
            {
                _logger.LogInformation("🔧 Starting EditSpot for spot {SpotId}: {SpotName}", spot.Id, spot.Name);
                
                // Validate spot data before creating parameter
                if (spot.Id <= 0)
                {
                    _logger.LogError("🔧 Invalid spot ID: {SpotId}", spot.Id);
                    await HandleErrorAsync("Erreur", "ID du spot invalide. Impossible d'éditer ce spot.");
                    return;
                }

                _logger.LogInformation("🔧 Creating SpotNavigationParameter with ID: {SpotId}, Name: {SpotName}, Lat: {Lat}, Lng: {Lng}", 
                    spot.Id, spot.Name, spot.Latitude, spot.Longitude);
                
                // Create navigation parameter for editing with enhanced error checking
                SubExplore.Models.Navigation.SpotNavigationParameter editParameter;
                try
                {
                    editParameter = new SubExplore.Models.Navigation.SpotNavigationParameter(
                        spot.Id, 
                        spot.Name, 
                        spot.Latitude, 
                        spot.Longitude);
                    
                    _logger.LogInformation("🔧 SpotNavigationParameter created successfully: {Parameter}", editParameter.ToString());
                }
                catch (Exception paramEx)
                {
                    _logger.LogError(paramEx, "🔧 Error creating SpotNavigationParameter for spot {SpotId}", spot.Id);
                    await HandleErrorAsync("Erreur", "Erreur lors de la préparation des données d'édition.");
                    return;
                }
                
                _logger.LogInformation("🔧 Attempting navigation to AddSpotViewModel with parameter");
                
                // Navigate to AddSpotPage in edit mode with enhanced error handling
                try
                {
                    await NavigateToAsync<AddSpotViewModel>(editParameter);
                    _logger.LogInformation("🔧 Navigation to AddSpotViewModel completed successfully");
                }
                catch (InvalidOperationException navEx)
                {
                    _logger.LogError(navEx, "🔧 Navigation InvalidOperationException for spot {SpotId}", spot.Id);
                    await HandleErrorAsync("Erreur de navigation", $"Impossible d'ouvrir l'éditeur de spot. Détails: {navEx.Message}");
                }
                catch (ArgumentException argEx)
                {
                    _logger.LogError(argEx, "🔧 Navigation ArgumentException for spot {SpotId}", spot.Id);
                    await HandleErrorAsync("Erreur de paramètres", $"Paramètres invalides pour l'édition. Détails: {argEx.Message}");
                }
                catch (System.Runtime.InteropServices.COMException comEx)
                {
                    _logger.LogError(comEx, "🔧 Navigation COMException (possible debugger issue) for spot {SpotId}", spot.Id);
                    await HandleErrorAsync("Erreur système", "Erreur système lors de l'ouverture de l'éditeur. Veuillez réessayer.");
                }
                catch (Exception navEx)
                {
                    _logger.LogError(navEx, "🔧 Unexpected navigation error for spot {SpotId}", spot.Id);
                    await HandleErrorAsync("Erreur de navigation", $"Erreur inattendue lors de l'ouverture de l'éditeur. Type: {navEx.GetType().Name}, Message: {navEx.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔧 Outer exception in EditSpot for spot {SpotId}. Type: {ExceptionType}, Message: {Message}", 
                    spot?.Id, ex.GetType().Name, ex.Message);
                
                // Log additional details about the exception
                if (ex.InnerException != null)
                {
                    _logger.LogError("🔧 Inner exception: {InnerType}: {InnerMessage}", 
                        ex.InnerException.GetType().Name, ex.InnerException.Message);
                }
                
                // Log stack trace for debugging
                _logger.LogDebug("🔧 Full stack trace: {StackTrace}", ex.StackTrace);
                
                await HandleErrorAsync("Erreur", $"Impossible d'ouvrir l'édition du spot. Type d'erreur: {ex.GetType().Name}");
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
            await ShowAlertAsync(title, message, "D'accord");
        }

        /// <summary>
        /// Diagnose and repair spot type configuration issues
        /// </summary>
        [RelayCommand]
        public async Task DiagnoseAndRepairSpotTypesAsync()
        {
            try
            {
                IsLoading = true;
                _logger.LogInformation("🔧 Starting spot type diagnosis and repair...");

                // Get diagnostic services from DI container
                var spotTypeDiagnostic = _serviceProvider.GetService<SubExplore.Services.Implementations.SpotTypeDiagnosticService>();
                var migrationService = _serviceProvider.GetService<SubExplore.Services.Implementations.SpotTypeMigrationService>();

                if (spotTypeDiagnostic == null || migrationService == null)
                {
                    _logger.LogError("Diagnostic or migration services not available");
                    await ShowAlertAsync("Erreur", "Services de diagnostic non disponibles", "D'accord");
                    return;
                }

                // Step 1: Run initial diagnosis
                _logger.LogInformation("📊 Running initial spot type diagnosis...");
                var initialReport = await spotTypeDiagnostic.DiagnoseSpotTypesAsync();
                _logger.LogInformation("Initial Diagnostic Report:\n{Report}", initialReport);

                // Step 2: Check migration status
                _logger.LogInformation("📋 Checking migration status...");
                var migrationStatus = await migrationService.GetMigrationStatusAsync();
                _logger.LogInformation("Migration Status:\n{Status}", migrationStatus);

                // Step 3: Execute migration if needed
                if (migrationStatus.Contains("MIGRATION REQUISE") || migrationStatus.Contains("MIGRATION PARTIELLE"))
                {
                    _logger.LogInformation("🚀 Migration required - executing...");
                    var migrationSuccess = await migrationService.ExecuteMigrationAsync();
                    
                    if (migrationSuccess)
                    {
                        _logger.LogInformation("✅ Migration completed successfully");
                        await ShowToastAsync("Migration des types de spots réussie");
                    }
                    else
                    {
                        _logger.LogError("❌ Migration failed");
                        await ShowAlertAsync("Erreur", "Échec de la migration des types de spots", "D'accord");
                        return;
                    }
                }

                // Step 4: Repair any remaining issues
                _logger.LogInformation("🔧 Running spot type repair...");
                var repairSuccess = await spotTypeDiagnostic.RepairSpotTypesAsync();
                
                if (repairSuccess)
                {
                    _logger.LogInformation("✅ Spot type repair completed successfully");
                    await ShowToastAsync("Réparation des types de spots réussie");
                }
                else
                {
                    _logger.LogWarning("⚠️ Spot type repair had issues");
                    await ShowAlertAsync("Avertissement", "Réparation partielle des types de spots", "D'accord");
                }

                // Step 5: Final diagnosis and ecosystem validation
                _logger.LogInformation("📊 Running final diagnosis and ecosystem validation...");
                var finalReport = await spotTypeDiagnostic.DiagnoseSpotTypesAsync();
                _logger.LogInformation("Final Diagnostic Report:\n{Report}", finalReport);

                var ecosystemValid = await spotTypeDiagnostic.ValidateSpotTypeEcosystemAsync();
                _logger.LogInformation("Ecosystem validation result: {Valid}", ecosystemValid ? "VALID" : "ISSUES DETECTED");

                // Step 6: Reload spots to reflect changes
                await LoadMySpots();

                await ShowAlertAsync("Réparation terminée", 
                    "Diagnostic et réparation des types de spots terminés. Vérifiez les logs pour plus de détails.", 
                    "D'accord");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during spot type diagnosis and repair");
                await HandleErrorAsync("Erreur de réparation", 
                    $"Erreur lors du diagnostic et de la réparation: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Quick diagnosis of spot types without repair
        /// </summary>
        [RelayCommand]
        public async Task QuickDiagnoseSpotTypesAsync()
        {
            try
            {
                _logger.LogInformation("🔍 Running quick spot type diagnosis...");
                
                // Simple diagnosis of current spots
                var spotsWithMissingTypes = MySpots?.Where(s => s.Type == null).ToList() ?? new List<Spot>();
                var spotsWithInactiveTypes = MySpots?.Where(s => s.Type != null && !s.Type.IsActive).ToList() ?? new List<Spot>();
                
                var report = $@"
=== DIAGNOSTIC RAPIDE ===
📊 Total spots: {MySpots?.Count ?? 0}
📊 Spots avec types manquants: {spotsWithMissingTypes.Count}
📊 Spots avec types inactifs: {spotsWithInactiveTypes.Count}
📊 Spots valides: {MySpots?.Count(s => s.Type != null && s.Type.IsActive) ?? 0}

Problèmes détectés: {(spotsWithMissingTypes.Any() || spotsWithInactiveTypes.Any() ? "OUI" : "NON")}
=== FIN DIAGNOSTIC ===";

                _logger.LogInformation("Quick Diagnostic:\n{Report}", report);
                
                if (spotsWithMissingTypes.Any() || spotsWithInactiveTypes.Any())
                {
                    await ShowAlertAsync("Problèmes détectés", 
                        $"Spots avec problèmes: {spotsWithMissingTypes.Count + spotsWithInactiveTypes.Count}\n" +
                        "Utilisez la réparation complète pour résoudre ces problèmes.", 
                        "D'accord");
                }
                else
                {
                    await ShowToastAsync("Aucun problème détecté");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during quick diagnosis");
                await HandleErrorAsync("Erreur de diagnostic", ex.Message);
            }
        }

        /// <summary>
        /// Automatic silent diagnosis for monitoring (called during LoadMySpots)
        /// </summary>
        private async Task AutoDiagnoseSpotTypesAsync()
        {
            try
            {
                // Silent diagnosis without UI alerts - only logging
                var spotsWithMissingTypes = MySpots?.Where(s => s.Type == null).ToList() ?? new List<Spot>();
                var spotsWithInactiveTypes = MySpots?.Where(s => s.Type != null && !s.Type.IsActive).ToList() ?? new List<Spot>();
                var validSpots = MySpots?.Where(s => s.Type != null && s.Type.IsActive).ToList() ?? new List<Spot>();

                if (spotsWithMissingTypes.Any())
                {
                    _logger.LogWarning("🚨 Found {Count} spots with missing types: {SpotIds}", 
                        spotsWithMissingTypes.Count, 
                        string.Join(", ", spotsWithMissingTypes.Select(s => $"{s.Id}({s.Name})")));
                }

                if (spotsWithInactiveTypes.Any())
                {
                    _logger.LogWarning("⚠️ Found {Count} spots with inactive types: {SpotDetails}", 
                        spotsWithInactiveTypes.Count, 
                        string.Join(", ", spotsWithInactiveTypes.Select(s => $"{s.Id}({s.Name}->TypeId:{s.TypeId})")));
                }

                if (validSpots.Any())
                {
                    var typeStats = validSpots.GroupBy(s => s.Type.Name)
                        .Select(g => $"{g.Key}:{g.Count()}")
                        .ToList();
                    
                    _logger.LogInformation("✅ Valid spots by type: {TypeStats}", string.Join(", ", typeStats));
                }

                // Log summary for monitoring
                var totalProblems = spotsWithMissingTypes.Count + spotsWithInactiveTypes.Count;
                if (totalProblems > 0)
                {
                    _logger.LogWarning("📊 MySpotsPage Health: {ValidSpots} valid, {ProblemsCount} with issues", 
                        validSpots.Count, totalProblems);
                    
                    // Suggest running repair if problems detected
                    _logger.LogInformation("💡 Suggestion: Run spot type repair to resolve {ProblemsCount} issues", totalProblems);
                }
                else
                {
                    _logger.LogInformation("✅ MySpotsPage Health: All {Count} spots have valid types", validSpots.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automatic spot type diagnosis");
                // Don't throw - this is a monitoring function
            }
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