using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

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

        public SpotDetailsViewModel(
            ISupabaseApiService supabaseApiService,
            INavigationService navigationService,
            IDialogService dialogService,
            IFavoriteSpotService favoriteSpotService,
            ISimpleAuthenticationService authenticationService,
            IWeatherService weatherService) : base(dialogService, navigationService)
        {
            _supabaseApiService = supabaseApiService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _favoriteSpotService = favoriteSpotService;
            _authenticationService = authenticationService;
            _weatherService = weatherService;

            Title = "Détails du Spot";
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            try
            {
                IsLoading = true;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel.InitializeAsync - Parameter: {parameter}, Type: {parameter?.GetType()?.Name ?? "null"}");

                if (parameter is Spot spot)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: Received Spot parameter - {spot.Name} (ID: {spot.Id})");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: Spot coordinates - Lat: {spot.Latitude}, Lon: {spot.Longitude}");
                    Spot = spot;
                    await LoadSpotDetails();
                }
                else if (parameter is Guid spotId)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: Received Guid parameter - {spotId}");
                    await LoadSpotById(spotId);
                }
                else if (parameter is string stringParam && Guid.TryParse(stringParam, out var guidFromString))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: Received string parameter converted to Guid - {guidFromString}");
                    await LoadSpotById(guidFromString);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] SpotDetailsViewModel: Invalid parameter type: {parameter?.GetType()?.Name ?? "null"}, Value: {parameter}");
                    
                    // ✅ CORRECTION: Ne pas charger automatiquement le premier spot, attendre le bon paramètre
                    if (parameter == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[WARNING] SpotDetailsViewModel: No parameter provided - waiting for ApplyQueryAttributes to be called");
                        // Ne pas faire d'action, laisser la page se charger normalement
                        // ApplyQueryAttributes devrait être appelé après et relancer l'initialisation
                        IsLoading = false;
                        return;
                    }
                    
                    // Si on arrive ici avec un paramètre invalide, afficher erreur
                    await _dialogService.ShowAlertAsync("Erreur", $"Paramètre invalide: {parameter?.GetType()?.Name ?? "null"} - {parameter}", "OK");
                    await _navigationService.GoBackAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SpotDetailsViewModel InitializeAsync failed: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadSpotById: Starting load for SpotId: {spotId}");
                
                // ✅ FIX: Add timeout to prevent hanging
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                System.Diagnostics.Debug.WriteLine("[DEBUG] LoadSpotById: Calling GetSpotsAsync...");
                // Récupérer tous les spots et trouver celui avec l'ID correspondant
                var supabaseSpots = await _supabaseApiService.GetSpotsAsync().WaitAsync(cts.Token);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadSpotById: Retrieved {supabaseSpots?.Count() ?? 0} spots from Supabase");
                
                var targetSupabaseSpot = supabaseSpots.FirstOrDefault(s => s.Id == spotId);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadSpotById: Target spot found: {targetSupabaseSpot != null}");
                
                if (targetSupabaseSpot != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadSpotById: RAW Supabase data - Name: {targetSupabaseSpot.Name}");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadSpotById: RAW Supabase data - Latitude: {targetSupabaseSpot.Latitude}");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadSpotById: RAW Supabase data - Longitude: {targetSupabaseSpot.Longitude}");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadSpotById: RAW Supabase data - Id: {targetSupabaseSpot.Id}");
                }
                
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
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadSpotById: Successfully converted spot - {Spot.Name} at {Spot.Latitude}, {Spot.Longitude}");
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
                System.Diagnostics.Debug.WriteLine("[ERROR] LoadSpotById timed out");
                await _dialogService.ShowAlertAsync("Timeout", 
                    "Le chargement a pris trop de temps. Vérifiez votre connexion réseau.", "OK");
                await _navigationService.GoBackAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotById failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotById stack trace: {ex.StackTrace}");
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
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotDetails failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotDetails stack trace: {ex.StackTrace}");
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
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadFavoriteStatus failed: {ex.Message}");
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

                // Obtenir l'utilisateur connecté
                var currentUser = await _authenticationService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    await _dialogService.ShowAlertAsync("Authentification requise", "Vous devez être connecté pour gérer vos favoris.", "OK");
                    return;
                }

                // Log de débogage
                var loadingMessage = IsFavorite ? "Suppression du favori..." : "Ajout aux favoris...";
                System.Diagnostics.Debug.WriteLine($"[DEBUG] {loadingMessage} - Utilisateur: {currentUser.Id}, Spot: {Spot.Id}");

                // ✅ UTILISATION DU SERVICE SUPABASE REAL
                bool newFavoriteState = await _favoriteSpotService.ToggleFavoriteAsync(currentUser.Id, Spot.Id);
                
                // Mettre à jour l'état
                IsFavorite = newFavoriteState;
                
                // Afficher un message de succès
                var successMessage = IsFavorite 
                    ? $"⭐ {Spot.Name} ajouté aux favoris !" 
                    : $"❌ {Spot.Name} retiré des favoris";
                    
                await _dialogService.ShowToastAsync(successMessage);
                
                System.Diagnostics.Debug.WriteLine($"[SUCCESS] Favori mis à jour - Nouveau statut: {IsFavorite}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Erreur lors du toggle favori: {ex.Message}");
                
                // Restaurer l'état précédent en cas d'erreur
                await LoadFavoriteStatus();
                
                var errorMessage = ex.Message.Contains("déjà en favoris") 
                    ? "Ce spot est déjà dans vos favoris" 
                    : $"Impossible de modifier les favoris : {ex.Message}";
                    
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

                var shareText = $"Découvrez ce spot de plongée : {Spot.Name}\n" +
                               $"Localisation : {CoordinatesDisplay}\n" +
                               $"Type : {SpotTypeDisplay}\n" +
                               $"Profondeur : {DepthDisplay}";

                // TODO: Implémenter le partage natif
                await _dialogService.ShowToastAsync("Partage non encore implémenté");
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

                System.Diagnostics.Debug.WriteLine($"[TEST] Test favoris - Utilisateur: {currentUser.Id}, Spot: {Spot.Id}");
                
                // Test vérification statut
                bool isCurrentlyFavorite = await _favoriteSpotService.IsSpotFavoritedAsync(currentUser.Id, Spot.Id);
                System.Diagnostics.Debug.WriteLine($"[TEST] Statut actuel: {isCurrentlyFavorite}");

                var testResult = $"Spot: {Spot.Name}\nUtilisateur: {currentUser.FirstName} {currentUser.LastName}\nStatut favori: {(isCurrentlyFavorite ? "En favoris" : "Pas en favoris")}\nID Spot: {Spot.Id}\nID User: {currentUser.Id}";
                
                await _dialogService.ShowAlertAsync("Test Favoris", testResult, "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Test favoris échoué: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur Test", $"Test échoué: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task EditSpot()
        {
            try
            {
                if (Spot == null) return;

                // For now, show a message that edit functionality is not yet implemented
                await _dialogService.ShowAlertAsync("Information", "La fonction d'édition des spots sera bientôt disponible.", "OK");
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

                bool confirmed = await _dialogService.ShowConfirmationAsync(
                    "Signaler le spot",
                    $"Voulez-vous vraiment signaler le spot '{Spot.Name}' ?\n\nCette action permettra à l'équipe de modération d'examiner ce spot.",
                    "Signaler",
                    "Annuler");

                if (confirmed)
                {
                    // TODO: Implémenter la fonctionnalité de signalement
                    ShowToastMessage("🚨 Spot signalé avec succès", "#FF9800", "#FF9800", "White");
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
                    System.Diagnostics.Debug.WriteLine("[WEATHER] ❌ Weather service not available");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[WEATHER] 🔄 Loading weather for coordinates: {Spot.Latitude}, {Spot.Longitude}");

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
                    
                    System.Diagnostics.Debug.WriteLine($"[WEATHER] ✅ Weather data loaded: {CurrentWeather.Temperature}°C, {CurrentWeather.Description}");
                }
                else
                {
                    ShowWeatherError = true;
                    WeatherErrorMessage = "Impossible de récupérer les données météo";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadWeatherData failed: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: Converting spot '{supabaseSpot.Name}'");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: RAW coordinates from Supabase - Lat: {supabaseSpot.Latitude}, Lon: {supabaseSpot.Longitude}");
                
                if (supabaseSpot.Name == "AquaTech Diving Store")
                {
                    System.Diagnostics.Debug.WriteLine($"[SPECIAL] SpotDetailsViewModel: AquaTech Diving Store RAW data - Lat: {supabaseSpot.Latitude}, Lon: {supabaseSpot.Longitude}");
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
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to convert SupabaseSpot to Spot: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to convert SupabaseSpotType to SpotType: {ex.Message}");
                return null;
            }
        }
        
        #endregion
    }
}