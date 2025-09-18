// ========================================
// REFACTORED ADD SPOT VIEWMODEL
// ========================================
// Clean architecture with separated concerns
// Uses new form services for better maintainability

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.ViewModels;
using SubExplore.Models.Enums;
using SubExplore.Models.Supabase;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Spots
{
    /// <summary>
    /// Refactored Add Spot ViewModel with clean architecture
    /// Separated concerns using dedicated services
    /// </summary>
    public partial class RefactoredAddSpotViewModel : ViewModelBase, IDisposable
    {
        #region Services
        private readonly ISupabaseApiService _apiService;
        private readonly ILocationService _locationService;
        private readonly ISimpleAuthenticationService _authService;
        private readonly ILogger<RefactoredAddSpotViewModel> _logger;
        private readonly IAddSpotFormService _formService;
        private readonly ISpotTypeService _spotTypeService;
        private readonly ISpotTypeTestDataService _testDataService;
        private readonly IApplicationPerformanceService? _performanceService;
        #endregion

        #region Form Data Properties
        [ObservableProperty]
        private string _spotName = string.Empty;

        [ObservableProperty]
        private string _spotDescription = string.Empty;

        [ObservableProperty]
        private double _latitude = 43.6047; // Default to Marseille

        [ObservableProperty]
        private double _longitude = 1.4442; // Default to Toulouse area

        [ObservableProperty]
        private ObservableCollection<SpotType> _selectedSpotTypes = new();

        [ObservableProperty]
        private ObservableCollection<SpotTypeItem> _spotTypes = new();

        partial void OnSpotTypesChanged(ObservableCollection<SpotTypeItem> value)
        {
            OnPropertyChanged(nameof(SpotTypesCount));
        }

        // Propriété calculée pour le count
        public int SpotTypesCount => SpotTypes?.Count ?? 0;
        #endregion

        #region UI State Properties
        [ObservableProperty]
        private bool _isLoadingSpotTypes;

        [ObservableProperty]
        private bool _canCreateSpot;

        [ObservableProperty]
        private bool _isCreatingSpot;

        [ObservableProperty]
        private bool _isGettingLocation;

        [ObservableProperty]
        private string _creationProgress = string.Empty;

        [ObservableProperty]
        private double _progressPercentage;
        #endregion

        #region Validation Properties
        [ObservableProperty]
        private string _spotNameError = string.Empty;

        [ObservableProperty]
        private string _locationError = string.Empty;

        [ObservableProperty]
        private string _spotTypeError = string.Empty;

        [ObservableProperty]
        private bool _hasValidationErrors = false;

        [ObservableProperty]
        private string _validationSummary = string.Empty;
        #endregion

        #region Location Properties
        [ObservableProperty]
        private bool _isLocationPickerVisible;

        [ObservableProperty]
        private string _locationDisplayName = "📍 France, Sud-Ouest (par défaut)";

        [ObservableProperty]
        private bool _isLocationAccurate = true;

        [ObservableProperty]
        private double _locationAccuracy;
        #endregion


        #region Constructor and Initialization
        public RefactoredAddSpotViewModel(
            ISupabaseApiService apiService,
            ILocationService locationService,
            ISimpleAuthenticationService authService,
            ILogger<RefactoredAddSpotViewModel> logger,
            IAddSpotFormService formService,
            ISpotTypeService spotTypeService,
            ISpotTypeTestDataService testDataService,
            IApplicationPerformanceService? performanceService = null,
            IDialogService? dialogService = null,
            INavigationService? navigationService = null) : base(dialogService, navigationService)
        {
            _apiService = apiService;
            _locationService = locationService;
            _authService = authService;
            _logger = logger;
            _formService = formService;
            _spotTypeService = spotTypeService;
            _testDataService = testDataService;
            _performanceService = performanceService;

            Title = "Ajouter un Spot";

            // Setup real-time validation
            PropertyChanged += OnPropertyChanged;
        }

        public override async Task InitializeAsync(IDictionary<string, object> parameters)
        {
            try
            {
                _logger.LogInformation("🎯 Initializing RefactoredAddSpotViewModel...");
                await LoadSpotTypesAsync();

                ValidateForm();

                _logger.LogInformation("✅ RefactoredAddSpotViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error initializing RefactoredAddSpotViewModel");
                ShowError("Erreur lors de l'initialisation de la page");
            }
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(SpotName) or nameof(SpotDescription) or
                nameof(Latitude) or nameof(Longitude) or nameof(SelectedSpotTypes))
            {
                ValidateForm();
            }
        }
        #endregion

        #region Spot Type Management
        [RelayCommand]
        private async Task LoadSpotTypesAsync(bool forceReload = false)
        {
            if (IsLoadingSpotTypes)
            {
                _logger.LogWarning("⚠️ LoadSpotTypesAsync called while already loading");
                return;
            }

            try
            {
                IsLoadingSpotTypes = true;
                ClearError();

                _logger.LogInformation("🎯 Loading spot types... (forceReload: {ForceReload})", forceReload);

                var result = await _spotTypeService.LoadSpotTypesAsync();

                if (result.IsSuccess)
                {
                    _logger.LogInformation("🎯 SpotTypeService returned {Count} spot types", result.SpotTypes?.Count ?? 0);

                    var spotTypeItems = _spotTypeService.CreateSpotTypeItems(result.SpotTypes);
                    _logger.LogInformation("🎯 Created {Count} SpotTypeItems for UI", spotTypeItems.Count);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        try
                        {
                            _logger.LogInformation("🎯 Clearing existing {Count} items from SpotTypes collection", SpotTypes.Count);
                            SpotTypes.Clear();

                            foreach (var item in spotTypeItems)
                            {
                                SpotTypes.Add(item);
                                _logger.LogDebug("🎯 Added SpotTypeItem: {Name}", item.Name);
                            }

                            _logger.LogInformation("✅ SpotTypes collection now has {Count} items", SpotTypes.Count);

                            // Force PropertyChanged notification just in case
                            OnPropertyChanged(nameof(SpotTypes));
                            OnPropertyChanged(nameof(SpotTypesCount));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Exception while updating SpotTypes collection");
                        }
                    });

                    _logger.LogInformation($"✅ Loaded {result.FilteredCount} spot types successfully");
                }
                else
                {
                    _logger.LogError($"❌ Failed to load spot types: {result.ErrorMessage}");

                    // Try to initialize basic spot types if none found
                    if (result.ErrorMessage?.Contains("Aucun type de spot") == true)
                    {
                        _logger.LogInformation("🎯 No spot types found, initializing basic data...");
                        try
                        {
                            await _testDataService.EnsureBasicSpotTypesAsync();
                            _logger.LogInformation("✅ Basic spot types initialized, retrying load...");

                            // Retry loading after initialization
                            var retryResult = await _spotTypeService.LoadSpotTypesAsync();
                            if (retryResult.IsSuccess)
                            {
                                var spotTypeItems = _spotTypeService.CreateSpotTypeItems(retryResult.SpotTypes);
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    SpotTypes.Clear();
                                    foreach (var item in spotTypeItems)
                                    {
                                        SpotTypes.Add(item);
                                    }
                                });
                                _logger.LogInformation($"✅ Loaded {retryResult.FilteredCount} spot types after initialization");
                                return; // Success, exit early
                            }
                        }
                        catch (Exception initEx)
                        {
                            _logger.LogError(initEx, "❌ Failed to initialize basic spot types");
                        }
                    }

                    ShowError(result.ErrorMessage ?? "Impossible de charger les types de spots");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error loading spot types");
                ShowError("Erreur inattendue lors du chargement des types");
            }
            finally
            {
                IsLoadingSpotTypes = false;
                _logger.LogInformation("🎯 LoadSpotTypesAsync finished, calling ValidateForm()");
                ValidateForm();
            }
        }

        [RelayCommand]
        private void SelectSpotType(SpotTypeItem spotTypeItem)
        {
            try
            {
                _logger.LogInformation($"🎯 Toggle spot type selection: {spotTypeItem.Name}");

                // Toggle selection state
                spotTypeItem.IsSelected = !spotTypeItem.IsSelected;

                // Update SelectedSpotTypes collection
                if (spotTypeItem.IsSelected)
                {
                    if (!SelectedSpotTypes.Contains(spotTypeItem.SpotType))
                    {
                        SelectedSpotTypes.Add(spotTypeItem.SpotType);
                        _logger.LogInformation($"✅ Added spot type: {spotTypeItem.SpotType.Name}");
                    }
                }
                else
                {
                    SelectedSpotTypes.Remove(spotTypeItem.SpotType);
                    _logger.LogInformation($"➖ Removed spot type: {spotTypeItem.SpotType.Name}");
                }

                ValidateForm();

                _logger.LogInformation($"📊 Total selected types: {SelectedSpotTypes.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error toggling spot type: {spotTypeItem?.Name}");
            }
        }
        #endregion

        #region Location Management
        [RelayCommand]
        private async Task GetCurrentLocationAsync()
        {
            if (IsGettingLocation) return;

            try
            {
                IsGettingLocation = true;
                ClearError();

                _logger.LogInformation("🎯 Getting current location...");

                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    Latitude = (double)location.Latitude;
                    Longitude = (double)location.Longitude;
                    LocationAccuracy = location.Accuracy;
                    IsLocationAccurate = location.Accuracy <= 50; // Within 50 meters

                    // Update display name
                    LocationDisplayName = $"📍 {location.Latitude:F4}, {location.Longitude:F4}";

                    _logger.LogInformation($"✅ Location updated: {Latitude:F4}, {Longitude:F4}");
                }
                else
                {
                    ShowError("Impossible d'obtenir la localisation actuelle");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting current location");
                ShowError("Erreur lors de l'obtention de la localisation");
            }
            finally
            {
                IsGettingLocation = false;
                ValidateForm();
            }
        }

        public void UpdateLocationFromMap(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
            LocationDisplayName = $"📍 {latitude:F4}, {longitude:F4}";
            IsLocationAccurate = true; // User-selected location is considered accurate

            _logger.LogInformation($"🎯 Location updated from map: {latitude:F4}, {longitude:F4}");
            ValidateForm();
        }
        #endregion

        #region Form Validation
        private void ValidateForm()
        {
            try
            {
                _logger.LogDebug("🎯 ValidateForm called - SpotTypes.Count: {Count}, SelectedSpotTypes: {Selected}",
                    SpotTypes.Count, SelectedSpotTypes.Count);

                // Validate individual sections
                var basicInfoResult = _formService.ValidateBasicInfo(SpotName, SpotDescription);
                var locationResult = _formService.ValidateLocation(Latitude, Longitude, IsLocationAccurate, LocationAccuracy);
                var spotTypeResult = _formService.ValidateSpotTypes(SelectedSpotTypes, SpotTypes.Any());

                _logger.LogDebug("🎯 Validation results - Basic: {Basic}, Location: {Location}, SpotType: {SpotType}",
                    basicInfoResult.IsValid, locationResult.IsValid, spotTypeResult.IsValid);

                // Update individual error messages
                SpotNameError = basicInfoResult.IsValid ? string.Empty : string.Join("; ", basicInfoResult.Errors);
                LocationError = locationResult.IsValid ? string.Empty : string.Join("; ", locationResult.Errors);
                SpotTypeError = spotTypeResult.IsValid ? string.Empty : string.Join("; ", spotTypeResult.Errors);

                // Check overall form validity
                var formData = new AddSpotFormData
                {
                    Name = SpotName,
                    Description = SpotDescription,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    IsAccurate = IsLocationAccurate,
                    Accuracy = LocationAccuracy,
                    SelectedSpotTypes = SelectedSpotTypes.ToList(),
                    HasAvailableTypes = SpotTypes.Any()
                };

                var overallResult = _formService.ValidateCompleteForm(formData);
                HasValidationErrors = !overallResult.IsValid;
                CanCreateSpot = overallResult.IsValid && !IsCreatingSpot;

                if (HasValidationErrors)
                {
                    var results = new List<Models.Validation.StepValidationResult> { basicInfoResult, locationResult, spotTypeResult };
                    ValidationSummary = _formService.CreateValidationSummary(results);
                    _logger.LogDebug("🎯 HasValidationErrors=true, ValidationSummary length: {Length}", ValidationSummary?.Length ?? 0);
                }
                else
                {
                    ValidationSummary = string.Empty;
                    _logger.LogDebug("🎯 HasValidationErrors=false, form is valid");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during form validation");
            }
        }
        #endregion

        #region Spot Creation
        [RelayCommand]
        private async Task CreateSpotAsync()
        {
            if (IsCreatingSpot || !CanCreateSpot) return;

            try
            {
                IsCreatingSpot = true;
                CanCreateSpot = false;
                CreationProgress = "Validation des données...";
                ProgressPercentage = 10;

                // Final validation
                if (!_formService.CanSubmitForm(new AddSpotFormData
                {
                    Name = SpotName,
                    Description = SpotDescription,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    IsAccurate = IsLocationAccurate,
                    Accuracy = LocationAccuracy,
                    SelectedSpotTypes = SelectedSpotTypes.ToList(),
                    HasAvailableTypes = SpotTypes.Any()
                }))
                {
                    ShowError("Le formulaire contient des erreurs. Veuillez les corriger.");
                    return;
                }

                CreationProgress = "Récupération de l'utilisateur...";
                ProgressPercentage = 30;

                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    ShowError("Vous devez être connecté pour créer un spot");
                    return;
                }

                CreationProgress = "Création du spot...";
                ProgressPercentage = 60;

                // Create the practice spot
                var newSpot = new SupabasePracticeSpot
                {
                    Name = SpotName.Trim(),
                    Description = SpotDescription.Trim(),
                    Latitude = (decimal)Latitude,
                    Longitude = (decimal)Longitude,
                    CreatorId = currentUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    ValidationStatus = "pending"
                };

                CreationProgress = "Envoi vers l'API...";
                ProgressPercentage = 80;

                var createdSpot = await _apiService.CreatePracticeSpotAsync(newSpot);

                if (createdSpot != null && createdSpot.Id > 0)
                {
                    CreationProgress = "Spot créé avec succès!";
                    ProgressPercentage = 100;

                    _logger.LogInformation($"✅ Practice spot created successfully: {newSpot.Name} with ID: {createdSpot.Id}");

                    // Navigate back or show success
                    if (NavigationService != null)
                    {
                        await NavigationService.GoBackAsync();
                    }
                }
                else
                {
                    _logger.LogError($"❌ Failed to create practice spot");
                    ShowError("Erreur lors de la création du spot");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error creating spot");
                ShowError("Erreur inattendue lors de la création du spot");
            }
            finally
            {
                IsCreatingSpot = false;
                CreationProgress = string.Empty;
                ProgressPercentage = 0;
                ValidateForm(); // This will update CanCreateSpot
            }
        }
        #endregion

        #region Additional Commands
        [RelayCommand]
        private void ToggleLocationPicker()
        {
            IsLocationPickerVisible = !IsLocationPickerVisible;
        }

        [RelayCommand]
        private void ClearForm()
        {
            SpotName = string.Empty;
            SpotDescription = string.Empty;
            SelectedSpotTypes.Clear();
            ValidateForm();
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            if (NavigationService != null)
            {
                await NavigationService.GoBackAsync();
            }
        }
        #endregion

        #region Public Methods for UI Binding Fix
        /// <summary>
        /// Force PropertyChanged notifications for SpotTypes collection
        /// Called from code-behind to fix MAUI BindingContext instance mismatch
        /// </summary>
        public void ForceSpotTypesPropertyChanged()
        {
            OnPropertyChanged(nameof(SpotTypes));
            OnPropertyChanged(nameof(SpotTypesCount));
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            PropertyChanged -= OnPropertyChanged;
            _logger.LogInformation("🧹 RefactoredAddSpotViewModel disposed");
        }
        #endregion
    }
}