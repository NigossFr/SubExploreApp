using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Models.Navigation;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Extensions.Logging;

namespace SubExplore.ViewModels.Spots
{
    public partial class SpotDetailsViewModel : ViewModelBase
    {
        private readonly ISupabaseApiService _supabaseApiService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IFavoriteSpotService _favoriteSpotService;
        private readonly ISimpleAuthenticationService _authenticationService;
        private readonly IWeatherService _weatherService;
        private readonly ISharingService _sharingService;
        private readonly ISpotReportService _spotReportService;
        private readonly ISpotEditService _spotEditService;
        private readonly IEnhancedMediaService _enhancedMediaService;
        private readonly IConnectivityService? _connectivityService;
        private readonly ILogger<SpotDetailsViewModel>? _logger;

        [ObservableProperty]
        private Spot? _spot;

        [ObservableProperty]
        private bool _isLoading = true;

        [ObservableProperty]
        private bool _isFavorite;

        [ObservableProperty]
        private ObservableCollection<string> _photos = new();

        [ObservableProperty]
        private string _spotTypeDisplay = string.Empty;

        [ObservableProperty]
        private string _difficultyDisplay = string.Empty;

        [ObservableProperty]
        private string _depthDisplay = string.Empty;

        [ObservableProperty]
        private string _coordinatesDisplay = string.Empty;

        [ObservableProperty]
        private bool _isFavoriteLoading = false;

        // ✅ PROPRIÉTÉS MÉTÉO ET CONDITIONS DE PLONGÉE
        [ObservableProperty]
        private WeatherInfo? _currentWeather;

        [ObservableProperty]
        private Services.Interfaces.DivingWeatherConditions? _divingConditions;

        [ObservableProperty]
        private bool _hasWeatherData = false;

        [ObservableProperty]
        private bool _isLoadingWeather = false;

        [ObservableProperty]
        private bool _showWeatherError = false;

        [ObservableProperty]
        private string _weatherErrorMessage = string.Empty;

        // ✅ PROPRIÉTÉS POUR TOAST MESSAGES
        [ObservableProperty]
        private bool _isToastVisible = false;

        [ObservableProperty]
        private string _toastMessage = string.Empty;

        [ObservableProperty]
        private string _toastBackgroundColor = "#4CAF50";

        [ObservableProperty]
        private string _toastBorderColor = "#4CAF50";

        [ObservableProperty]
        private string _toastTextColor = "White";

        // ✅ PROPRIÉTÉS POUR COMMANDS SUPPLÉMENTAIRES
        [ObservableProperty]
        private bool _isError = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _pageTitle = "Détails du spot";

        // Validation mode properties
        [ObservableProperty]
        private bool _isValidationMode = false;

        [ObservableProperty]
        private string _validationMode = string.Empty;

        // Property change handler for dynamic title updates
        partial void OnSpotChanged(Spot? value)
        {
            if (value != null && !string.IsNullOrEmpty(value.Name))
            {
                PageTitle = value.Name;
            }
            else
            {
                PageTitle = "Détails du spot";
            }
        }

        public SpotDetailsViewModel(
            ISupabaseApiService supabaseApiService,
            INavigationService navigationService,
            IDialogService dialogService,
            IFavoriteSpotService favoriteSpotService,
            ISimpleAuthenticationService authenticationService,
            IWeatherService weatherService,
            ISharingService sharingService,
            ISpotReportService spotReportService,
            ISpotEditService spotEditService,
            IEnhancedMediaService enhancedMediaService,
            IConnectivityService? connectivityService = null,
            ILogger<SpotDetailsViewModel>? logger = null) : base(dialogService, navigationService)
        {
            _supabaseApiService = supabaseApiService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _favoriteSpotService = favoriteSpotService;
            _authenticationService = authenticationService;
            _weatherService = weatherService;
            _sharingService = sharingService;
            _spotReportService = spotReportService;
            _spotEditService = spotEditService;
            _enhancedMediaService = enhancedMediaService;
            _connectivityService = connectivityService;
            _logger = logger;

            Title = "Détails du Spot";
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            try
            {
                IsLoading = true;

                if (parameter is Spot spot)
                {
                    Spot = spot;
                    await LoadSpotDetails();
                }
                else if (parameter is FavoriteNavigationParameter favoriteParam)
                {
                    // Handle favorite navigation parameter
                    _logger?.LogInformation("Loading spot details from favorite navigation parameter: {SpotId}", favoriteParam.SpotId);
                    await LoadSpotById(favoriteParam.SpotId);
                    
                    // Set favorite state from the parameter
                    IsFavorite = favoriteParam.IsFavorite;
                }
                else if (parameter is Dictionary<string, object> validationParam)
                {
                    // Handle validation mode parameters from NavigationService
                    _logger?.LogInformation("Loading spot details with validation parameters");
                    
                    if (validationParam.TryGetValue("SpotId", out var spotIdValue) && spotIdValue is Guid validationSpotId)
                    {
                        // Set validation mode properties first
                        if (validationParam.TryGetValue("IsValidationMode", out var isValidationValue) && isValidationValue is bool isValidation)
                        {
                            IsValidationMode = isValidation;
                        }
                        
                        if (validationParam.TryGetValue("ValidationMode", out var validationModeValue))
                        {
                            ValidationMode = validationModeValue?.ToString() ?? string.Empty;
                        }
                        
                        // Load the spot data
                        await LoadSpotById(validationSpotId);
                        
                        // Update page title for validation mode after spot is loaded
                        if (IsValidationMode && Spot != null)
                        {
                            PageTitle = $"Validation - {Spot.Name}";
                        }
                        
                        _logger?.LogInformation("Validation mode enabled: {ValidationMode} for spot {SpotId}", ValidationMode, validationSpotId);
                    }
                }
                else if (parameter is Guid spotId)
                {
                    await LoadSpotById(spotId);
                }
                else if (parameter is string stringParam && Guid.TryParse(stringParam, out var guidFromString))
                {
                    await LoadSpotById(guidFromString);
                }
                else
                {
                    if (parameter == null)
                    {
                        IsLoading = false;
                        return;
                    }
                    
                    await _dialogService.ShowAlertAsync("Erreur", "Paramètre de navigation invalide", "OK");
                    await _navigationService.GoBackAsync();
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors du chargement : {ex.Message}", "OK");
                await _navigationService.GoBackAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadSpotById(Guid spotId)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                var supabaseSpots = await _supabaseApiService.GetSpotsAsync().WaitAsync(cts.Token);
                var targetSupabaseSpot = supabaseSpots.FirstOrDefault(s => s.Id == spotId);
                
                if (targetSupabaseSpot == null)
                {
                    await _dialogService.ShowAlertAsync("Erreur", $"Spot non trouvé (ID: {spotId})", "OK");
                    await _navigationService.GoBackAsync();
                    return;
                }
                
                // Récupérer les types de spots pour la conversion
                var supabaseSpotTypes = await _supabaseApiService.GetSpotTypesAsync().WaitAsync(cts.Token);
                var spotTypes = supabaseSpotTypes.Select(st => ConvertToDomainSpotType(st)).Where(st => st != null).ToList();
                
                // Convertir vers le modèle de domaine avec les types
                Spot = ConvertToDomainSpot(targetSupabaseSpot, spotTypes);
                
                if (Spot != null)
                {
                    await LoadSpotDetails();
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Impossible de convertir les données du spot", "OK");
                    await _navigationService.GoBackAsync();
                }
            }
            catch (TimeoutException)
            {
                await _dialogService.ShowAlertAsync("Timeout", 
                    "Le chargement a pris trop de temps. Vérifiez votre connexion réseau.", "OK");
                await _navigationService.GoBackAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur API Supabase: {ex.Message}", "OK");
                await _navigationService.GoBackAsync();
            }
        }

        private async Task LoadSpotDetails()
        {
            if (Spot == null) 
            {
                IsLoading = false; // ✅ FIX: Reset loading state when no spot
                return;
            }

            try
            {
                // Formatage des informations d'affichage
                SpotTypeDisplay = Spot.Type?.Name ?? "Type non spécifié";
                DifficultyDisplay = Spot.DifficultyLevel.ToString();
                DepthDisplay = Spot.MaxDepth.HasValue ? $"{Spot.MaxDepth:F1}m" : "Profondeur non spécifiée";
                CoordinatesDisplay = $"{Spot.Latitude:F6}, {Spot.Longitude:F6}";

                // Charger les photos si disponibles
                if (Spot.Media?.Any() == true)
                {
                    var mediaUrls = Spot.Media.Where(m => !string.IsNullOrEmpty(m.MediaUrl)).Select(m => m.MediaUrl).ToList();
                    Photos = new ObservableCollection<string>(mediaUrls);
                }

                // ✅ CHARGER LE STATUT FAVORI DE L'UTILISATEUR CONNECTE
                await LoadFavoriteStatus();

                // ✅ CHARGER LES DONNÉES MÉTÉO
                await LoadWeatherData();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors du chargement: {ex.Message}", "OK");
            }
            finally
            {
                // ✅ CRITICAL FIX: Always reset loading state
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge le statut favori pour l'utilisateur connecté
        /// </summary>
        private async Task LoadFavoriteStatus()
        {
            try
            {
                // Obtenir l'utilisateur connecté
                var currentUser = await _authenticationService.GetCurrentUserAsync();
                
                if (currentUser != null && Spot != null)
                {
                    // Vérifier si le spot est en favoris
                    IsFavorite = await _favoriteSpotService.IsSpotFavoritedAsync(currentUser.Id, Spot.Id);
                }
                else
                {
                    // Utilisateur non connecté ou spot manquant
                    IsFavorite = false;
                }
            }
            catch (Exception ex)
            {
                IsFavorite = false; // Par défaut en cas d'erreur
            }
        }

        [RelayCommand]
        private async Task ToggleFavorite()
        {
            if (Spot == null)
            {
                await _dialogService.ShowAlertAsync("Erreur", "Aucun spot sélectionné", "OK");
                return;
            }

            try
            {
                // Désactiver le bouton pendant l'opération
                IsFavoriteLoading = true;

                // 🔧 AMÉLIORATION: Validation renforcée de l'authentification
                var currentUser = await _authenticationService.GetCurrentUserAsync();
                if (currentUser?.Id == null || currentUser.Id == Guid.Empty)
                {
                    _logger?.LogWarning("Authentication failed: user is null or has invalid ID");
                    
                    // Tentative de re-authentification automatique
                    await _authenticationService.InitializeAsync();
                    currentUser = await _authenticationService.GetCurrentUserAsync();
                    
                    if (currentUser?.Id == null || currentUser.Id == Guid.Empty)
                    {
                        await _dialogService.ShowAlertAsync(
                            "Authentification expirée", 
                            "Votre session a expiré. Veuillez vous reconnecter.", 
                            "OK");
                        return;
                    }
                }

                // 🔧 AMÉLIORATION: Vérification de la connectivité
                if (_connectivityService != null && !_connectivityService.IsConnected)
                {
                    await _dialogService.ShowAlertAsync(
                        "Pas de connexion", 
                        "Une connexion internet est requise pour modifier les favoris.", 
                        "OK");
                    return;
                }

                // ✅ UTILISATION DU SERVICE SUPABASE REAL
                bool newFavoriteState = await _favoriteSpotService.ToggleFavoriteAsync(currentUser.Id, Spot.Id);
                
                // Mettre à jour l'état
                IsFavorite = newFavoriteState;
                
                // Afficher un message de succès
                var successMessage = IsFavorite 
                    ? $"⭐ {Spot.Name} ajouté aux favoris !" 
                    : $"❌ {Spot.Name} retiré des favoris";
                    
                await _dialogService.ShowToastAsync(successMessage);
                
            }
            catch (UnauthorizedAccessException)
            {
                await _dialogService.ShowAlertAsync(
                    "Authentification requise", 
                    "Votre session a expiré. Veuillez vous reconnecter.", 
                    "OK");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("n'existe pas"))
            {
                await _dialogService.ShowAlertAsync(
                    "Erreur de synchronisation", 
                    "Problème de synchronisation utilisateur. Reconnectez-vous pour corriger.", 
                    "OK");
            }
            catch (TimeoutException)
            {
                await _dialogService.ShowAlertAsync(
                    "Timeout", 
                    "L'opération a pris trop de temps. Vérifiez votre connexion.", 
                    "OK");
            }
            catch (HttpRequestException)
            {
                await _dialogService.ShowAlertAsync(
                    "Erreur réseau", 
                    "Impossible de joindre le serveur. Vérifiez votre connexion.", 
                    "OK");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error in ToggleFavorite");
                
                // Restaurer l'état précédent en cas d'erreur
                await LoadFavoriteStatus();
                
                var errorMessage = ex.Message.Contains("déjà en favoris") 
                    ? "Ce spot est déjà dans vos favoris" 
                    : "Une erreur inattendue s'est produite. Réessayez dans quelques instants.";
                    
                await _dialogService.ShowAlertAsync("Erreur", errorMessage, "OK");
            }
            finally
            {
                // Réactiver le bouton
                IsFavoriteLoading = false;
            }
        }

        [RelayCommand]
        private async Task ShareSpot()
        {
            try
            {
                if (Spot == null) return;

                bool success = await _sharingService.ShareSpotAsync(Spot, includePhotos: false);
                
                if (success)
                {
                    ShowToastMessage("📤 Spot partagé avec succès!", "#4CAF50", "#4CAF50", "White");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors du partage : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ShowOnMap()
        {
            try
            {
                if (Spot == null) return;

                // Navigate using Shell navigation to map route
                await Shell.Current.GoToAsync("//map", new Dictionary<string, object>
                {
                    ["spot"] = Spot,
                    ["centerOnSpot"] = true
                });
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors de l'affichage sur la carte : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task TestFavorites()
        {
            try
            {
                if (Spot == null)
                {
                    await _dialogService.ShowAlertAsync("Test", "Aucun spot sélectionné pour le test", "OK");
                    return;
                }

                var currentUser = await _authenticationService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    await _dialogService.ShowAlertAsync("Test", "Aucun utilisateur connecté", "OK");
                    return;
                }

                
                // Test vérification statut
                bool isCurrentlyFavorite = await _favoriteSpotService.IsSpotFavoritedAsync(currentUser.Id, Spot.Id);

                var testResult = $"Spot: {Spot.Name}\nUtilisateur: {currentUser.FirstName} {currentUser.LastName}\nStatut favori: {(isCurrentlyFavorite ? "En favoris" : "Pas en favoris")}\nID Spot: {Spot.Id}\nID User: {currentUser.Id}";
                
                await _dialogService.ShowAlertAsync("Test Favoris", testResult, "OK");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur Test", $"Test échoué: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task EditSpot()
        {
            try
            {
                if (Spot == null) return;

                // Check if user can edit this spot
                bool canEdit = await _spotEditService.CanUserEditSpotAsync(Spot);
                if (!canEdit)
                {
                    await _dialogService.ShowAlertAsync("Permission refusée", 
                        "Vous n'avez pas l'autorisation de modifier ce spot.", "OK");
                    return;
                }

                // Show edit options
                var editOptions = new[]
                {
                    "Édition complète",
                    "Gestion des photos",
                    "Signaler pour revalidation"
                };

                var selectedOption = await _dialogService.ShowActionSheetAsync(
                    "Modifier le spot",
                    "Annuler",
                    null,
                    editOptions);

                if (selectedOption == "Annuler" || string.IsNullOrEmpty(selectedOption))
                    return;

                switch (selectedOption)
                {
                    case "Édition complète":
                        await NavigateToFullEdit();
                        break;
                    case "Gestion des photos":
                        await ManagePhotos();
                        break;
                    case "Signaler pour revalidation":
                        await SubmitForRevalidation();
                        break;
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors de l'édition : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ReportSpot()
        {
            try
            {
                if (Spot == null) return;

                // Show report type selection
                var reportTypes = _spotReportService.GetReportTypes();
                var typeNames = reportTypes.Values.ToArray();
                
                var selectedType = await _dialogService.ShowActionSheetAsync(
                    "Signaler ce spot",
                    "Annuler",
                    null,
                    typeNames);

                if (selectedType == "Annuler" || string.IsNullOrEmpty(selectedType))
                    return;

                // Get the report type enum from selected string
                var reportType = reportTypes.FirstOrDefault(x => x.Value == selectedType).Key;

                // Get description from user
                string description = await _dialogService.ShowPromptAsync(
                    "Description du problème",
                    "Veuillez décrire le problème en détail :",
                    "OK",
                    "Annuler",
                    "Description détaillée...");

                if (string.IsNullOrWhiteSpace(description))
                    return;

                // Submit report
                var reportId = await _spotReportService.SubmitReportAsync(
                    Spot.Id, 
                    reportType, 
                    description, 
                    severity: Models.Enums.SpotReportSeverity.Medium);

                if (reportId.HasValue)
                {
                    ShowToastMessage("🚨 Signalement soumis avec succès", "#FF9800", "#FF9800", "White");
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Impossible de soumettre le signalement", "OK");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors du signalement : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task RefreshWeather()
        {
            await LoadWeatherData(forceRefresh: true);
        }

        [RelayCommand]
        private async Task Refresh()
        {
            try
            {
                IsLoading = true;
                IsError = false;
                
                if (Spot != null)
                {
                    await LoadSpotDetails();
                }
            }
            catch (Exception ex)
            {
                IsError = true;
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand] 
        public async Task Back()
        {
            await _navigationService.GoBackAsync();
        }

        #region Weather Methods
        
        /// <summary>
        /// Charge les données météo pour le spot
        /// </summary>
        private async Task LoadWeatherData(bool forceRefresh = false)
        {
            if (Spot == null) return;

            try
            {
                IsLoadingWeather = true;
                ShowWeatherError = false;
                
                // Vérifier si le service météo est disponible
                if (!await _weatherService.IsServiceAvailableAsync())
                {
                    ShowWeatherError = true;
                    WeatherErrorMessage = "Service météo indisponible";
                    return;
                }
                

                // Charger la météo actuelle
                CurrentWeather = await _weatherService.GetCurrentWeatherAsync(
                    Spot.Latitude, Spot.Longitude);

                if (CurrentWeather != null)
                {
                    // Charger les conditions de plongée
                    DivingConditions = await _weatherService.GetDivingConditionsAsync(
                        Spot.Latitude, Spot.Longitude);
                    
                    HasWeatherData = true;
                    ShowWeatherError = false;
                    
                }
                else
                {
                    ShowWeatherError = true;
                    WeatherErrorMessage = "Impossible de récupérer les données météo";
                }
            }
            catch (Exception ex)
            {
                ShowWeatherError = true;
                WeatherErrorMessage = $"Erreur météo: {ex.Message}";
                HasWeatherData = false;
            }
            finally
            {
                IsLoadingWeather = false;
            }
        }

        #endregion
        
        #region Toast Methods
        
        /// <summary>
        /// Affiche un message toast
        /// </summary>
        private void ShowToastMessage(string message, string backgroundColor = "#4CAF50", string borderColor = "#4CAF50", string textColor = "White")
        {
            ToastMessage = message;
            ToastBackgroundColor = backgroundColor;
            ToastBorderColor = borderColor;
            ToastTextColor = textColor;
            IsToastVisible = true;
            
            // Masquer automatiquement après 3 secondes
            _ = Task.Delay(3000).ContinueWith(_ => IsToastVisible = false, TaskScheduler.FromCurrentSynchronizationContext());
        }
        
        #endregion

        #region Conversion Methods
        
        /// <summary>
        /// Convert SupabaseSpot to Domain Spot with spot types
        /// </summary>
        private Spot? ConvertToDomainSpot(Models.Supabase.SupabaseSpot supabaseSpot, List<SpotType> spotTypes)
        {
            if (supabaseSpot == null) return null;

            try
            {
                
                if (supabaseSpot.Name == "AquaTech Diving Store")
                {
                }
                
                // Lookup the spot type by TypeId
                SpotType? spotType = spotTypes.FirstOrDefault(t => t.Id == supabaseSpot.TypeId);
                
                return new Spot
                {
                    Id = supabaseSpot.Id,
                    Name = supabaseSpot.Name ?? string.Empty,
                    Description = supabaseSpot.Description ?? string.Empty,
                    Latitude = supabaseSpot.Latitude,
                    Longitude = supabaseSpot.Longitude,
                    CreatedAt = supabaseSpot.CreatedAt,
                    ValidationStatus = Models.Enums.SpotValidationStatus.Approved,
                    CreatorId = supabaseSpot.CreatorId,
                    TypeId = supabaseSpot.TypeId,
                    Type = spotType,
                    DifficultyLevel = (Models.Enums.DifficultyLevel)(supabaseSpot.DifficultyLevel ?? 0),
                    RequiredEquipment = supabaseSpot.RequiredEquipment ?? string.Empty,
                    SafetyNotes = supabaseSpot.SafetyNotes ?? string.Empty,
                    BestConditions = supabaseSpot.BestConditions ?? string.Empty,
                    MaxDepth = (int?)supabaseSpot.MaxDepth,
                    LastSafetyReview = supabaseSpot.LastSafetyReview,
                    SafetyFlags = supabaseSpot.SafetyFlags as Dictionary<string, object>,
                    Media = new List<SpotMedia>() // Empty for now
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert SupabaseSpotType to Domain SpotType
        /// </summary>
        private SpotType? ConvertToDomainSpotType(Models.Supabase.SupabaseSpotType supabaseSpotType)
        {
            if (supabaseSpotType == null) return null;

            try
            {
                // Parse category from Supabase string
                Models.Enums.ActivityCategory category = Models.Enums.ActivityCategory.Activity;
                if (!string.IsNullOrEmpty(supabaseSpotType.Category))
                {
                    if (Enum.TryParse<Models.Enums.ActivityCategory>(supabaseSpotType.Category, ignoreCase: true, out var parsedCategory))
                    {
                        category = parsedCategory;
                    }
                }

                return new SpotType
                {
                    Id = supabaseSpotType.Id,
                    Name = supabaseSpotType.Name ?? string.Empty,
                    Description = supabaseSpotType.Description ?? string.Empty,
                    Category = category,
                    CreatedAt = supabaseSpotType.CreatedAt,
                    UpdatedAt = supabaseSpotType.UpdatedAt,
                    IsActive = supabaseSpotType.IsActive == true,
                    IconPath = supabaseSpotType.IconPath ?? string.Empty,
                    ColorCode = supabaseSpotType.ColorCode ?? string.Empty,
                    RequiresExpertValidation = supabaseSpotType.RequiresExpertValidation,
                    ValidationCriteria = supabaseSpotType.ValidationCriteria as Dictionary<string, object>
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        
        #endregion

        #region Edit Methods

        private async Task NavigateToFullEdit()
        {
            if (Spot == null) return;

            try
            {
                // Navigate to comprehensive edit page
                await Shell.Current.GoToAsync("spotedit", new Dictionary<string, object>
                {
                    ["spotId"] = Spot.Id.ToString()
                });
            }
            catch (Exception ex)
            {
                ShowToastMessage("❌ Erreur navigation", "#F44336", "#F44336", "White");
            }
        }

        private async Task ManagePhotos()
        {
            if (Spot == null) return;

            var photoOptions = new[]
            {
                "Ajouter une photo",
                "Gérer les photos existantes",
                "Définir photo principale"
            };

            var selectedOption = await _dialogService.ShowActionSheetAsync(
                "Gestion des photos",
                "Annuler",
                null,
                photoOptions);

            switch (selectedOption)
            {
                case "Ajouter une photo":
                    await AddPhoto();
                    break;
                case "Gérer les photos existantes":
                    await ManageExistingPhotos();
                    break;
                case "Définir photo principale":
                    await SetPrimaryPhoto();
                    break;
            }
        }

        private async Task AddPhoto()
        {
            try
            {
                // TODO: Implement photo picker
                await _dialogService.ShowToastAsync("Sélection de photo - À implémenter avec MAUI Media picker");
                
                // Example implementation:
                /*
                var photo = await MediaPicker.PickPhotoAsync();
                if (photo != null)
                {
                    using var stream = await photo.OpenReadAsync();
                    var spotMedia = await _enhancedMediaService.AddSpotPhotoAsync(
                        Spot.Id, stream, photo.FileName);
                        
                    if (spotMedia != null)
                    {
                        await LoadSpotDetails(); // Refresh to show new photo
                    }
                }
                */
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors de l'ajout de photo: {ex.Message}", "OK");
            }
        }

        private async Task ManageExistingPhotos()
        {
            try
            {
                var photos = await _enhancedMediaService.GetSpotPhotosAsync(Spot.Id);
                if (photos.Count == 0)
                {
                    await _dialogService.ShowAlertAsync("Aucune photo", "Ce spot n'a pas encore de photos.", "OK");
                    return;
                }

                await _dialogService.ShowToastAsync("Gestion des photos existantes - UI à implémenter");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors de la gestion des photos: {ex.Message}", "OK");
            }
        }

        private async Task SetPrimaryPhoto()
        {
            try
            {
                var photos = await _enhancedMediaService.GetSpotPhotosAsync(Spot.Id);
                if (photos.Count == 0)
                {
                    await _dialogService.ShowAlertAsync("Aucune photo", "Ce spot n'a pas de photos.", "OK");
                    return;
                }

                await _dialogService.ShowToastAsync("Définition de la photo principale - UI à implémenter");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors de la définition de la photo principale: {ex.Message}", "OK");
            }
        }

        private async Task SubmitForRevalidation()
        {
            try
            {
                if (Spot == null) return;

                string reason = await _dialogService.ShowPromptAsync(
                    "Revalidation",
                    "Pourquoi ce spot nécessite-t-il une revalidation ?",
                    "Soumettre",
                    "Annuler",
                    "Raison de la revalidation...");

                if (string.IsNullOrWhiteSpace(reason))
                    return;

                bool success = await _spotEditService.SubmitForRevalidationAsync(Spot.Id, reason);
                
                if (success)
                {
                    ShowToastMessage("✅ Spot soumis pour revalidation", "#4CAF50", "#4CAF50", "White");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors de la soumission: {ex.Message}", "OK");
            }
        }

        #endregion
    }
}