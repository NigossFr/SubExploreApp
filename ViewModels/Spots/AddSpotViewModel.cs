using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Models.Validation;
using SubExplore.Services.Validation;
using SubExplore.Models.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Graphics;

namespace SubExplore.ViewModels.Spots
{
    public partial class SpotTypeItem : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        partial void OnIsSelectedChanged(bool value)
        {
            OnPropertyChanged(nameof(BackgroundColor));
        }

        public SpotType SpotType { get; set; }
        
        public string Name => SpotType.Name;
        public string Description => SpotType.Description;
        public string ColorCode => SpotType.ColorCode;
        public int Id => SpotType.Id;
        public ActivityCategory Category => SpotType.Category;
        public bool RequiresExpertValidation => SpotType.RequiresExpertValidation;
        public bool IsActive => SpotType.IsActive;
        public string IconPath => SpotType.IconPath;
        public string ValidationCriteria => SpotType.ValidationCriteria;
        public DateTime CreatedAt => SpotType.CreatedAt;
        public DateTime? UpdatedAt => SpotType.UpdatedAt;

        public Color BackgroundColor
        {
            get
            {
                if (IsSelected && !string.IsNullOrEmpty(ColorCode))
                {
                    try
                    {
                        return Color.FromArgb(ColorCode);
                    }
                    catch
                    {
                        return Colors.Green;
                    }
                }
                return Colors.White;
            }
        }
    }

    public partial class AddSpotViewModel : ViewModelBase
    {
        private readonly ILocationService _locationService;
        private readonly ISpotRepository _spotRepository;
        private readonly ISpotTypeRepository _spotTypeRepository;
        private readonly ISpotMediaRepository _spotMediaRepository;
        private readonly IMediaService _mediaService;
        private readonly ISettingsService _settingsService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<AddSpotViewModel> _logger;
        
        // Step validators
        private readonly LocationStepValidator _locationValidator;
        private readonly CharacteristicsStepValidator _characteristicsValidator;
        private readonly PhotosStepValidator _photosValidator;
        private readonly SummaryStepValidator _summaryValidator;

        [ObservableProperty]
        private int _currentStep;

        [ObservableProperty]
        private Models.Domain.Spot _newSpot;

        [ObservableProperty]
        private ObservableCollection<SpotTypeItem> _availableSpotTypes;

        [ObservableProperty]
        private ObservableCollection<SpotType> _selectedSpotTypes;

        [ObservableProperty]
        private ObservableCollection<string> _photosPaths;

        [ObservableProperty]
        private DifficultyLevel _selectedDifficultyLevel;

        [ObservableProperty]
        private SpotType _selectedSpotType;

        [ObservableProperty]
        private string _primaryPhotoPath;

        [ObservableProperty]
        private bool _isSubmitting;

        [ObservableProperty]
        private decimal _latitude;

        [ObservableProperty]
        private decimal _longitude;

        [ObservableProperty]
        private int _maxDepth;

        [ObservableProperty]
        private string _accessDescription;

        [ObservableProperty]
        private string _requiredEquipment;

        [ObservableProperty]
        private string _safetyNotes;

        [ObservableProperty]
        private string _bestConditions;

        [ObservableProperty]
        private CurrentStrength _selectedCurrentStrength;

        [ObservableProperty]
        private string _spotName;

        [ObservableProperty]
        private bool _hasUserLocation;

        [ObservableProperty]
        private bool _isLocationReady;

        [ObservableProperty]
        private ObservableCollection<DifficultyLevel> _difficultyLevels;

        [ObservableProperty]
        private ObservableCollection<CurrentStrength> _currentStrengths;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private int _editingSpotId;

        public AddSpotViewModel(
            ILocationService locationService,
            ISpotRepository spotRepository,
            ISpotTypeRepository spotTypeRepository,
            ISpotMediaRepository spotMediaRepository,
            IMediaService mediaService,
            ISettingsService settingsService,
            IAuthenticationService authenticationService,
            ILogger<AddSpotViewModel> logger,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _locationService = locationService;
            _spotRepository = spotRepository;
            _spotTypeRepository = spotTypeRepository;
            _spotMediaRepository = spotMediaRepository;
            _mediaService = mediaService;
            _settingsService = settingsService;
            _authenticationService = authenticationService;
            _logger = logger;
            
            // Initialize validators
            _locationValidator = new LocationStepValidator();
            _characteristicsValidator = new CharacteristicsStepValidator();
            _photosValidator = new PhotosStepValidator();
            _summaryValidator = new SummaryStepValidator();

            AvailableSpotTypes = new ObservableCollection<SpotTypeItem>();
            SelectedSpotTypes = new ObservableCollection<SpotType>();
            PhotosPaths = new ObservableCollection<string>();
            CurrentStep = 1;
            Title = "Nouveau spot";
            IsEditMode = false;

            // Initialize collections with enum values
            DifficultyLevels = new ObservableCollection<DifficultyLevel>
            {
                DifficultyLevel.Beginner,
                DifficultyLevel.Intermediate,
                DifficultyLevel.Advanced,
                DifficultyLevel.Expert
            };

            CurrentStrengths = new ObservableCollection<CurrentStrength>
            {
                CurrentStrength.None,
                CurrentStrength.Weak,
                CurrentStrength.Moderate,
                CurrentStrength.Strong,
                CurrentStrength.Extreme
            };

            // Initialiser un nouveau spot
            NewSpot = new Models.Domain.Spot
            {
                CreatorId = _authenticationService.CurrentUserId ?? 1, // Use authenticated user or fallback
                CreatedAt = DateTime.UtcNow,
                ValidationStatus = SpotValidationStatus.Draft,
                Name = string.Empty,
                Description = string.Empty,
                RequiredEquipment = string.Empty,
                SafetyNotes = string.Empty,
                BestConditions = string.Empty,
                TypeId = 1 // Default value, will be updated when user selects
            };

            SelectedCurrentStrength = CurrentStrength.Weak;
            SelectedDifficultyLevel = DifficultyLevel.Beginner;
            
            // Initialize coordinates with default values to prevent 0,0 issues
            Latitude = 48.8566m; // Paris, France
            Longitude = 2.3522m;
            IsLocationReady = false; // Will be set to true after proper initialization
            
            _logger.LogDebug("AddSpotViewModel initialized with default coordinates: {Latitude}, {Longitude}", Latitude, Longitude);
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            _logger.LogDebug("🔧 AddSpotViewModel.InitializeAsync called with parameter: {Parameter}", parameter?.ToString() ?? "null");
            if (parameter != null)
            {
                _logger.LogDebug("🔧 Parameter type: {ParameterType}", parameter.GetType().Name);
                _logger.LogDebug("🔧 Parameter is SpotNavigationParameter: {IsSpotParam}", parameter.IsParameterType<SpotNavigationParameter>());
                
                // Log properties for anonymous types to debug Shell navigation parameters
                if (parameter.GetType().IsAnonymousType())
                {
                    var properties = parameter.GetType().GetProperties();
                    _logger.LogDebug("🔧 Anonymous parameter properties: {Properties}", 
                        string.Join(", ", properties.Select(p => $"{p.Name}={p.GetValue(parameter)}")));
                }
            }
            _logger.LogDebug("🔧 Current coordinates before initialization: {Latitude}, {Longitude}", Latitude, Longitude);
            
            await LoadSpotTypes().ConfigureAwait(false);
            
            // First check if this is editing an existing spot with strongly-typed parameter
            if (parameter != null && parameter.IsParameterType<SpotNavigationParameter>())
            {
                var spotParam = parameter.AsParameter<SpotNavigationParameter>();
                if (spotParam.SpotId > 0)
                {
                    _logger.LogInformation("🔧 Initializing for edit mode with SpotId: {SpotId}", spotParam.SpotId);
                    await LoadSpotForEditing(spotParam.SpotId).ConfigureAwait(false);
                    return; // Skip location handling as we'll use the spot's location
                }
            }
            
            // Check for edit mode in anonymous parameters (from Shell navigation)
            if (parameter != null)
            {
                var spotId = TryExtractSpotIdFromParameter(parameter);
                if (spotId > 0)
                {
                    _logger.LogInformation("🔧 Extracted SpotId {SpotId} from parameter, initializing for edit mode", spotId);
                    await LoadSpotForEditing(spotId).ConfigureAwait(false);
                    return; // Skip location handling as we'll use the spot's location
                }
            }
            
            // Handle location parameter passed from navigation
            if (parameter != null)
            {
                _logger.LogDebug("🔧 Handling location parameter from navigation");
                await HandleLocationParameter(parameter).ConfigureAwait(false);
            }
            else
            {
                _logger.LogDebug("🔧 No parameter provided, trying to get current location");
                await TryGetCurrentLocation().ConfigureAwait(false);
            }
            
            _logger.LogDebug("🔧 Final coordinates after initialization: {Latitude}, {Longitude}", Latitude, Longitude);
            
            // Mark location as ready for UI binding
            IsLocationReady = true;
            _logger.LogDebug("🔧 Location is now ready for UI binding");
        }

        /// <summary>
        /// Try to extract SpotId from any parameter format for edit mode detection
        /// </summary>
        /// <param name="parameter">Parameter object to extract SpotId from</param>
        /// <returns>SpotId if found, 0 otherwise</returns>
        private int TryExtractSpotIdFromParameter(object parameter)
        {
            if (parameter == null) return 0;
            
            try
            {
                var parameterType = parameter.GetType();
                _logger.LogInformation("🔧 TryExtractSpotIdFromParameter: Parameter type: {Type}", parameterType.Name);
                
                // Log all available properties for debugging
                var properties = parameterType.GetProperties();
                _logger.LogInformation("🔧 TryExtractSpotIdFromParameter: All properties: {Properties}", 
                    string.Join(", ", properties.Select(p => $"{p.Name}={p.GetValue(parameter)}")));
                
                // Try direct property access with various naming conventions (primary method for simple parameters)
                var spotIdProperty = parameterType.GetProperty("spotid") ?? 
                                   parameterType.GetProperty("SpotId") ?? 
                                   parameterType.GetProperty("SPOTID") ??
                                   parameterType.GetProperty("spot_id") ??
                                   parameterType.GetProperty("id");
                
                if (spotIdProperty != null)
                {
                    var spotIdValue = spotIdProperty.GetValue(parameter);
                    _logger.LogInformation("🔧 TryExtractSpotIdFromParameter: Found property {PropertyName} with value: {Value}", 
                        spotIdProperty.Name, spotIdValue);
                    
                    if (spotIdValue != null && int.TryParse(spotIdValue.ToString(), out int spotId) && spotId > 0)
                    {
                        _logger.LogInformation("🔧 TryExtractSpotIdFromParameter: Successfully parsed SpotId={SpotId} from property {PropertyName}", 
                            spotId, spotIdProperty.Name);
                        return spotId;
                    }
                    else
                    {
                        _logger.LogWarning("🔧 TryExtractSpotIdFromParameter: Property {PropertyName} found but value is invalid: {Value}", 
                            spotIdProperty.Name, spotIdValue);
                    }
                }
                else
                {
                    _logger.LogWarning("🔧 TryExtractSpotIdFromParameter: No SpotId property found in parameter");
                }
                
                // Check if we have a mode property indicating edit mode (even without SpotId)
                var modeProperty = parameterType.GetProperty("mode") ?? parameterType.GetProperty("Mode");
                if (modeProperty != null)
                {
                    var modeValue = modeProperty.GetValue(parameter);
                    _logger.LogInformation("🔧 TryExtractSpotIdFromParameter: Found mode property with value: {Mode}", modeValue);
                    if (modeValue?.ToString()?.ToLower() == "edit")
                    {
                        _logger.LogWarning("🔧 TryExtractSpotIdFromParameter: Edit mode detected but no valid SpotId found");
                    }
                }
                
                // Check LocationParameter for encoded query string (fallback method)
                var locationParamProperty = parameterType.GetProperty("LocationParameter");
                if (locationParamProperty != null)
                {
                    var locationParamValue = locationParamProperty.GetValue(parameter);
                    _logger.LogInformation("🔧 TryExtractSpotIdFromParameter: LocationParameter value: {LocationParam}", locationParamValue);
                    
                    if (locationParamValue != null && !string.IsNullOrEmpty(locationParamValue.ToString()))
                    {
                        var locationParamStr = locationParamValue.ToString();
                        var queryParts = locationParamStr.Split('&');
                        foreach (var part in queryParts)
                        {
                            var keyValue = part.Split('=');
                            if (keyValue.Length == 2)
                            {
                                var key = keyValue[0].ToLower();
                                if (key == "spotid" || key == "id")
                                {
                                    if (int.TryParse(System.Web.HttpUtility.UrlDecode(keyValue[1]), out int spotId) && spotId > 0)
                                    {
                                        _logger.LogInformation("🔧 TryExtractSpotIdFromParameter: Found SpotId={SpotId} in LocationParameter with key={Key}", spotId, key);
                                        return spotId;
                                    }
                                }
                            }
                        }
                    }
                }
                
                _logger.LogInformation("🔧 TryExtractSpotIdFromParameter: No SpotId found in parameter");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔧 TryExtractSpotIdFromParameter: Error extracting SpotId");
                return 0;
            }
        }

        /// <summary>
        /// Loads active spot types from repository for user selection
        /// </summary>
        /// <returns>Task representing the asynchronous loading operation</returns>
        private async Task LoadSpotTypes()
        {
            try
            {
                var types = await _spotTypeRepository.GetActiveTypesAsync().ConfigureAwait(false);

                AvailableSpotTypes.Clear();
                foreach (var type in types)
                {
                    AvailableSpotTypes.Add(new SpotTypeItem { SpotType = type, IsSelected = false });
                }

                // Don't auto-select any spot type, let user choose
                SelectedSpotType = null;
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les types de spots.", "OK");
            }
        }

        /// <summary>
        /// Attempts to retrieve the user's current location, falling back to default coordinates
        /// </summary>
        /// <returns>Task representing the asynchronous location retrieval operation</returns>
        private async Task TryGetCurrentLocation()
        {
            try
            {
                _logger.LogDebug("Attempting to get current location");
                var location = await _locationService.GetCurrentLocationAsync().ConfigureAwait(false);
                if (location != null)
                {
                    Latitude = location.Latitude;
                    Longitude = location.Longitude;
                    HasUserLocation = true;
                    _logger.LogInformation("Successfully obtained user location: {Latitude}, {Longitude}", Latitude, Longitude);
                }
                else
                {
                    HasUserLocation = false;
                    // Utiliser une position par défaut (Paris)
                    Latitude = 48.8566m;
                    Longitude = 2.3522m;
                    _logger.LogInformation("Location not available, using default location: Paris");
                }
            }
            catch (Exception ex)
            {
                HasUserLocation = false;
                // Utiliser une position par défaut (Paris)
                Latitude = 48.8566m;
                Longitude = 2.3522m;
                _logger.LogError(ex, "Error getting current location, using default location: Paris");
            }
        }

        /// <summary>
        /// Loads an existing spot for editing
        /// </summary>
        /// <param name="spotId">The ID of the spot to edit</param>
        /// <returns>Task representing the asynchronous loading operation</returns>
        private async Task LoadSpotForEditing(int spotId)
        {
            try
            {
                _logger.LogInformation("Loading spot {SpotId} for editing", spotId);
                
                var existingSpot = await _spotRepository.GetByIdAsync(spotId).ConfigureAwait(false);
                if (existingSpot == null)
                {
                    _logger.LogWarning("Spot {SpotId} not found for editing", spotId);
                    await DialogService.ShowAlertAsync("Erreur", "Le spot à modifier n'a pas été trouvé.", "OK");
                    return;
                }

                // Set edit mode
                IsEditMode = true;
                EditingSpotId = spotId;
                Title = "Modifier le spot";

                // Load spot data into the form
                NewSpot = existingSpot;
                SpotName = existingSpot.Name;
                
                // Location
                Latitude = existingSpot.Latitude;
                Longitude = existingSpot.Longitude;
                HasUserLocation = false; // This is from a spot, not user location
                IsLocationReady = true;

                // Characteristics
                MaxDepth = existingSpot.MaxDepth ?? 0;
                AccessDescription = existingSpot.Description ?? string.Empty;
                RequiredEquipment = existingSpot.RequiredEquipment ?? string.Empty;
                SafetyNotes = existingSpot.SafetyNotes ?? string.Empty;
                BestConditions = existingSpot.BestConditions ?? string.Empty;
                SelectedDifficultyLevel = existingSpot.DifficultyLevel;
                SelectedCurrentStrength = existingSpot.CurrentStrength ?? CurrentStrength.Weak;

                // Select the spot type
                if (existingSpot.Type != null)
                {
                    SelectedSpotType = existingSpot.Type;
                    var spotTypeItem = AvailableSpotTypes.FirstOrDefault(st => st.Id == existingSpot.Type.Id);
                    if (spotTypeItem != null)
                    {
                        spotTypeItem.IsSelected = true;
                        SelectedSpotTypes.Clear();
                        SelectedSpotTypes.Add(existingSpot.Type);
                    }
                }

                // Load existing photos
                if (existingSpot.Media != null && existingSpot.Media.Any())
                {
                    PhotosPaths.Clear();
                    foreach (var media in existingSpot.Media.Where(m => m.MediaType == MediaType.Photo))
                    {
                        PhotosPaths.Add(media.MediaUrl);
                        if (media.IsPrimary)
                        {
                            PrimaryPhotoPath = media.MediaUrl;
                        }
                    }
                }

                _logger.LogInformation("Successfully loaded spot {SpotId} for editing: {SpotName}", spotId, existingSpot.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading spot {SpotId} for editing", spotId);
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger le spot pour modification.", "OK");
            }
        }

        /// <summary>
        /// Handles location parameters passed from navigation with support for multiple parameter types
        /// </summary>
        /// <param name="parameter">Navigation parameter containing location data</param>
        /// <returns>Task representing the asynchronous parameter handling operation</returns>
        private async Task HandleLocationParameter(object parameter)
        {
            try
            {
                _logger.LogDebug("🔧 Handling location parameter from navigation: {ParameterType}", parameter?.GetType().Name ?? "null");
                
                // Use strongly-typed parameter system
                if (parameter.IsParameterType<LocationNavigationParameter>())
                {
                    var locationParam = parameter.AsParameter<LocationNavigationParameter>();
                    Latitude = locationParam.Latitude;
                    Longitude = locationParam.Longitude;
                    AccessDescription = locationParam.Description;
                    HasUserLocation = locationParam.IsFromUserLocation;
                    _logger.LogInformation("🔧 Using LocationNavigationParameter: {Latitude}, {Longitude}, UserLocation: {IsFromUserLocation}", 
                        Latitude, Longitude, HasUserLocation);
                    return;
                }
                
                // Handle SpotNavigationParameter (for editing scenarios)
                if (parameter.IsParameterType<SpotNavigationParameter>())
                {
                    var spotParam = parameter.AsParameter<SpotNavigationParameter>();
                    _logger.LogInformation("🔧 Found SpotNavigationParameter with SpotId: {SpotId}", spotParam.SpotId);
                    if (spotParam.Latitude.HasValue && spotParam.Longitude.HasValue)
                    {
                        Latitude = spotParam.Latitude.Value;
                        Longitude = spotParam.Longitude.Value;
                        HasUserLocation = false; // This is from a spot, not user location
                        _logger.LogInformation("🔧 Using SpotNavigationParameter location: {Latitude}, {Longitude}", Latitude, Longitude);
                        return;
                    }
                }
                
                // Try to extract location using extension method
                var (extractedLat, extractedLng) = parameter.ExtractLocation();
                if (extractedLat.HasValue && extractedLng.HasValue)
                {
                    Latitude = extractedLat.Value;
                    Longitude = extractedLng.Value;
                    HasUserLocation = false;
                    _logger.LogInformation("🔧 Using extracted location: {Latitude}, {Longitude}", Latitude, Longitude);
                    return;
                }
                
                // Handle legacy anonymous object format (for backward compatibility)
                if (parameter is object legacyParam && legacyParam.GetType().IsAnonymousType())
                {
                    _logger.LogWarning("🔧 Received legacy anonymous parameter, consider updating to use strongly-typed parameters");
                    await HandleLegacyParameter(legacyParam);
                    return;
                }
                
                // Check if this looks like a query parameter object (Shell navigation)
                if (parameter != null && !parameter.GetType().IsAnonymousType())
                {
                    _logger.LogWarning("🔧 Received non-anonymous parameter that isn't strongly typed, treating as legacy parameter");
                    await HandleLegacyParameter(parameter);
                    return;
                }
                
                // Fallback to default location if parameter extraction fails
                _logger.LogWarning("🔧 Failed to extract location from navigation parameter of type {ParameterType}, using default location", 
                    parameter?.GetType().Name ?? "null");
                Latitude = 48.8566m; // Paris
                Longitude = 2.3522m;
                HasUserLocation = false;
                _logger.LogInformation("Using default location (Paris): {Latitude}, {Longitude}", Latitude, Longitude);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔧 Error handling location parameter, using default location");
                Latitude = 48.8566m; // Paris
                Longitude = 2.3522m;
                HasUserLocation = false;
                _logger.LogInformation("Using default location after error (Paris): {Latitude}, {Longitude}", Latitude, Longitude);
            }
        }
        
        /// <summary>
        /// Handles legacy anonymous object parameters for backward compatibility
        /// </summary>
        /// <param name="parameter">Legacy anonymous object containing location data</param>
        /// <returns>Task representing the asynchronous legacy parameter handling operation</returns>
        private async Task HandleLegacyParameter(object parameter)
        {
            try
            {
                // Legacy reflection-based handling for backward compatibility
                var parameterType = parameter.GetType();
                _logger.LogInformation("🔧 HandleLegacyParameter: Parameter type: {ParameterType}", parameterType.Name);
                
                // Log all properties for debugging
                var properties = parameterType.GetProperties();
                _logger.LogInformation("🔧 Available properties: {Properties}", string.Join(", ", properties.Select(p => $"{p.Name}={p.GetValue(parameter)}")));
                
                // Check if this is an edit mode parameter (has spotid) - try multiple variations
                var spotIdProperty = parameterType.GetProperty("spotid") ?? 
                                   parameterType.GetProperty("SpotId") ?? 
                                   parameterType.GetProperty("SPOTID") ??
                                   parameterType.GetProperty("spot_id");
                
                if (spotIdProperty != null)
                {
                    var spotIdValue = spotIdProperty.GetValue(parameter);
                    _logger.LogInformation("🔧 Found spotid property: {SpotIdValue}", spotIdValue);
                    if (spotIdValue != null && int.TryParse(spotIdValue.ToString(), out int spotId) && spotId > 0)
                    {
                        _logger.LogInformation("🔧 Detected edit mode via legacy parameter - SpotId: {SpotId}", spotId);
                        await LoadSpotForEditing(spotId).ConfigureAwait(false);
                        return;
                    }
                    else
                    {
                        _logger.LogWarning("🔧 spotid property found but value is invalid: {SpotIdValue}", spotIdValue);
                    }
                }
                
                // Check if there's a LocationParameter that might contain the spotid
                var locationParamProperty = parameterType.GetProperty("LocationParameter");
                if (locationParamProperty != null)
                {
                    var locationParamValue = locationParamProperty.GetValue(parameter);
                    _logger.LogInformation("🔧 Found LocationParameter: {LocationParameter}", locationParamValue);
                    
                    // Try to parse LocationParameter as a URL query string
                    if (locationParamValue != null)
                    {
                        var locationParamStr = locationParamValue.ToString();
                        if (!string.IsNullOrEmpty(locationParamStr))
                        {
                            // Parse query string manually to extract spotid
                            var queryParts = locationParamStr.Split('&');
                            foreach (var part in queryParts)
                            {
                                var keyValue = part.Split('=');
                                if (keyValue.Length == 2 && keyValue[0].ToLower() == "spotid")
                                {
                                    if (int.TryParse(System.Web.HttpUtility.UrlDecode(keyValue[1]), out int spotId) && spotId > 0)
                                    {
                                        _logger.LogInformation("🔧 Found spotid in LocationParameter - SpotId: {SpotId}", spotId);
                                        await LoadSpotForEditing(spotId).ConfigureAwait(false);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                
                _logger.LogInformation("🔧 No spotid found in any property, checking for location parameters");
                
                // Handle location-only parameters
                var latitudeProperty = parameterType.GetProperty("Latitude") ?? parameterType.GetProperty("latitude");
                var longitudeProperty = parameterType.GetProperty("Longitude") ?? parameterType.GetProperty("longitude");
                
                if (latitudeProperty != null && longitudeProperty != null)
                {
                    var latValue = latitudeProperty.GetValue(parameter);
                    var lngValue = longitudeProperty.GetValue(parameter);
                    
                    _logger.LogInformation("🔧 Found location properties - Lat: {Lat}, Lng: {Lng}", latValue, lngValue);
                    
                    if (latValue != null && lngValue != null)
                    {
                        Latitude = Convert.ToDecimal(latValue);
                        Longitude = Convert.ToDecimal(lngValue);
                        HasUserLocation = true;
                        _logger.LogInformation("Using legacy parameter format: {Latitude}, {Longitude}", Latitude, Longitude);
                        return;
                    }
                }
                
                _logger.LogWarning("🔧 No valid parameters found, using default location");
                // Use default location instead of triggering location permission dialog
                Latitude = 48.8566m; // Paris
                Longitude = 2.3522m;
                HasUserLocation = false;
                _logger.LogInformation("Using default location (Paris): {Latitude}, {Longitude}", Latitude, Longitude);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔧 Error handling legacy parameter format, using default location");
                // Use default location instead of triggering location permission dialog
                Latitude = 48.8566m; // Paris
                Longitude = 2.3522m;
                HasUserLocation = false;
                _logger.LogInformation("Using default location after error (Paris): {Latitude}, {Longitude}", Latitude, Longitude);
            }
        }

        /// <summary>
        /// Command to take a photo using the device camera for the spot
        /// </summary>
        /// <returns>Task representing the asynchronous photo capture operation</returns>
        [RelayCommand]
        private async Task TakePhoto()
        {
            var photoPath = await _mediaService.TakePhotoAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(photoPath))
            {
                if (string.IsNullOrEmpty(PrimaryPhotoPath))
                {
                    PrimaryPhotoPath = photoPath;
                }

                PhotosPaths.Add(photoPath);

                // Maximum 3 photos
                if (PhotosPaths.Count >= 3)
                {
                    await DialogService.ShowToastAsync("Maximum 3 photos atteint");
                }
            }
        }

        /// <summary>
        /// Command to pick an existing photo from the device gallery for the spot
        /// </summary>
        /// <returns>Task representing the asynchronous photo selection operation</returns>
        [RelayCommand]
        private async Task PickPhoto()
        {
            var photoPath = await _mediaService.PickPhotoAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(photoPath))
            {
                if (string.IsNullOrEmpty(PrimaryPhotoPath))
                {
                    PrimaryPhotoPath = photoPath;
                }

                PhotosPaths.Add(photoPath);

                // Maximum 3 photos
                if (PhotosPaths.Count >= 3)
                {
                    await DialogService.ShowToastAsync("Maximum 3 photos atteint");
                }
            }
        }

        /// <summary>
        /// Command to remove a photo from the spot's photo collection
        /// </summary>
        /// <param name="path">File path of the photo to remove</param>
        [RelayCommand]
        private void RemovePhoto(string path)
        {
            if (PhotosPaths.Contains(path))
            {
                PhotosPaths.Remove(path);

                // Si c'était la photo principale, mettre à jour
                if (PrimaryPhotoPath == path)
                {
                    PrimaryPhotoPath = PhotosPaths.Count > 0 ? PhotosPaths[0] : null;
                }
            }
        }

        /// <summary>
        /// Command to set a specific photo as the primary photo for the spot
        /// </summary>
        /// <param name="path">File path of the photo to set as primary</param>
        [RelayCommand]
        private void SetPrimaryPhoto(string path)
        {
            if (PhotosPaths.Contains(path))
            {
                PrimaryPhotoPath = path;
            }
        }

        /// <summary>
        /// Command to advance to the next step in the spot creation workflow
        /// </summary>
        /// <returns>Task representing the asynchronous step advancement operation</returns>
        [RelayCommand]
        private async Task NextStep()
        {
            if (!await ValidateCurrentStep().ConfigureAwait(false))
            {
                return;
            }

            // Sauvegarder les données de l'étape actuelle
            SaveCurrentStepData();

            CurrentStep++;

            // Limiter à 4 étapes maximum
            if (CurrentStep > 4)
            {
                CurrentStep = 4;

                // Si on arrive à l'étape 4, c'est la validation finale
                await SubmitSpot().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Command to go back to the previous step in the spot creation workflow
        /// </summary>
        [RelayCommand]
        private void PreviousStep()
        {
            CurrentStep--;
            if (CurrentStep < 1)
            {
                CurrentStep = 1;
            }
        }


        /// <summary>
        /// Command to manually request the user's current location
        /// </summary>
        /// <returns>Task representing the asynchronous location request operation</returns>
        [RelayCommand]
        private async Task GetCurrentLocation()
        {
            try
            {
                IsLoading = true;
                _logger.LogInformation("Getting current location...");
                
                var location = await _locationService.GetCurrentLocationAsync().ConfigureAwait(false);
                if (location != null)
                {
                    Latitude = (decimal)location.Latitude;
                    Longitude = (decimal)location.Longitude;
                    HasUserLocation = true;
                    IsLocationReady = true;
                    
                    _logger.LogInformation("Successfully obtained current location: {Latitude}, {Longitude}", Latitude, Longitude);
                    await DialogService.ShowAlertAsync("Succès", "Position actuelle obtenue avec succès.", "OK");
                }
                else
                {
                    _logger.LogWarning("Unable to get current location");
                    await DialogService.ShowAlertAsync("Erreur", "Impossible d'obtenir la position actuelle. Vérifiez les permissions de localisation.", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current location");
                await DialogService.ShowAlertAsync("Erreur", "Erreur lors de l'obtention de la position. Vérifiez les permissions de localisation.", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Command to navigate directly to a specific step in the workflow
        /// </summary>
        /// <param name="stepNumber">Target step number (1-based)</param>
        [RelayCommand]
        private void GoToStep(int stepNumber)
        {
            // Only allow navigation to previous steps or current step
            if (stepNumber <= CurrentStep && stepNumber >= 1)
            {
                CurrentStep = stepNumber;
                _logger.LogInformation("Navigated to step {StepNumber}", stepNumber);
            }
            else
            {
                _logger.LogWarning("Invalid step navigation attempted: {StepNumber} (current: {CurrentStep})", stepNumber, CurrentStep);
            }
        }

        /// <summary>
        /// Command to handle spot type selection with toggle behavior
        /// </summary>
        /// <param name="selectedItem">The spot type item that was selected/deselected</param>
        [RelayCommand]
        private void SpotTypeSelected(SpotTypeItem selectedItem)
        {
            if (selectedItem == null) return;

            // Toggle selection state
            selectedItem.IsSelected = !selectedItem.IsSelected;

            // Update selected types collection
            if (selectedItem.IsSelected)
            {
                SelectedSpotTypes.Add(selectedItem.SpotType);
                _logger.LogInformation("Spot type selected: {SpotTypeName}", selectedItem.SpotType?.Name ?? "null");
            }
            else
            {
                SelectedSpotTypes.Remove(selectedItem.SpotType);
                _logger.LogInformation("Spot type deselected: {SpotTypeName}", selectedItem.SpotType?.Name ?? "null");
            }

            // Update the primary selected spot type (for backward compatibility)
            if (SelectedSpotTypes.Count > 0)
            {
                SelectedSpotType = SelectedSpotTypes[0];
            }
            else
            {
                SelectedSpotType = null;
            }

            _logger.LogInformation("Total selected spot types: {Count}", SelectedSpotTypes.Count);
        }

        /// <summary>
        /// Validates the current step with specific validation logic for each step
        /// </summary>
        private async Task<bool> ValidateCurrentStep()
        {
            var result = CurrentStep switch
            {
                1 => await ValidateLocationStepAsync(),
                2 => await ValidateCharacteristicsStepAsync(),
                3 => await ValidatePhotosStepAsync(),
                4 => await ValidateSummaryStepAsync(),
                _ => StepValidationResult.Success("Unknown")
            };

            if (!result.IsValid)
            {
                await HandleValidationFailureAsync(result);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates location step (step 1)
        /// </summary>
        private async Task<StepValidationResult> ValidateLocationStepAsync()
        {
            var locationData = CreateLocationStepData();
            return _locationValidator.Validate(locationData);
        }

        /// <summary>
        /// Validates characteristics step (step 2)
        /// </summary>
        private async Task<StepValidationResult> ValidateCharacteristicsStepAsync()
        {
            var characteristicsData = CreateCharacteristicsStepData();
            return _characteristicsValidator.Validate(characteristicsData);
        }

        /// <summary>
        /// Validates photos step (step 3)
        /// </summary>
        private async Task<StepValidationResult> ValidatePhotosStepAsync()
        {
            var photosData = CreatePhotosStepData();
            return _photosValidator.Validate(photosData);
        }

        /// <summary>
        /// Validates summary step (step 4)
        /// </summary>
        private async Task<StepValidationResult> ValidateSummaryStepAsync()
        {
            var summaryData = CreateSummaryStepData();
            return _summaryValidator.Validate(summaryData);
        }

        /// <summary>
        /// Creates location step data object
        /// </summary>
        private LocationStepData CreateLocationStepData()
        {
            return new LocationStepData
            {
                Latitude = Latitude,
                Longitude = Longitude,
                AccessDescription = AccessDescription,
                HasUserLocation = HasUserLocation
            };
        }

        /// <summary>
        /// Creates characteristics step data object
        /// </summary>
        private CharacteristicsStepData CreateCharacteristicsStepData()
        {
            return new CharacteristicsStepData
            {
                SpotName = SpotName,
                SelectedSpotType = SelectedSpotTypes.Count > 0 ? SelectedSpotTypes[0] : null,
                SelectedDifficultyLevel = SelectedDifficultyLevel,
                MaxDepth = MaxDepth,
                RequiredEquipment = RequiredEquipment,
                SafetyNotes = SafetyNotes,
                BestConditions = BestConditions,
                SelectedCurrentStrength = SelectedCurrentStrength
            };
        }

        /// <summary>
        /// Creates photos step data object
        /// </summary>
        private PhotosStepData CreatePhotosStepData()
        {
            return new PhotosStepData
            {
                PhotosPaths = PhotosPaths.ToList(),
                PrimaryPhotoPath = PrimaryPhotoPath,
                MaxPhotosAllowed = 3
            };
        }

        /// <summary>
        /// Creates summary step data object
        /// </summary>
        private SummaryStepData CreateSummaryStepData()
        {
            return new SummaryStepData
            {
                Location = CreateLocationStepData(),
                Characteristics = CreateCharacteristicsStepData(),
                Photos = CreatePhotosStepData()
            };
        }

        /// <summary>
        /// Handles validation failure with user feedback and logging
        /// </summary>
        private async Task HandleValidationFailureAsync(StepValidationResult result)
        {
            var errorMessage = string.Join("\n", result.Errors);
            await DialogService.ShowAlertAsync("Validation", errorMessage, "OK");
            _logger.LogWarning("Step validation failed for {StepName}: {Errors}", 
                result.StepName, string.Join(", ", result.Errors));
        }

        /// <summary>
        /// Saves current step data to the NewSpot object
        /// </summary>
        private void SaveCurrentStepData()
        {
            switch (CurrentStep)
            {
                case 1:
                    SaveLocationData();
                    break;
                case 2:
                    SaveCharacteristicsData();
                    break;
                case 3:
                case 4:
                    // No additional data to save for photos and summary steps
                    break;
            }
        }

        /// <summary>
        /// Saves location data from step 1
        /// </summary>
        private void SaveLocationData()
        {
            NewSpot.Latitude = Latitude;
            NewSpot.Longitude = Longitude;
            NewSpot.Description = AccessDescription;
        }

        /// <summary>
        /// Saves characteristics data from step 2
        /// </summary>
        private void SaveCharacteristicsData()
        {
            NewSpot.Name = SpotName;
            NewSpot.TypeId = SelectedSpotTypes.Count > 0 ? SelectedSpotTypes[0].Id : 1;
            NewSpot.DifficultyLevel = SelectedDifficultyLevel;
            NewSpot.MaxDepth = MaxDepth;
            NewSpot.RequiredEquipment = RequiredEquipment ?? string.Empty;
            NewSpot.SafetyNotes = SafetyNotes ?? string.Empty;
            NewSpot.BestConditions = BestConditions ?? string.Empty;
            NewSpot.CurrentStrength = SelectedCurrentStrength;
        }

        /// <summary>
        /// Command to submit the completed spot for validation and storage
        /// </summary>
        /// <returns>Task representing the asynchronous spot submission operation</returns>
        [RelayCommand]
        private async Task SubmitSpot()
        {
            if (IsSubmitting)
                return;

            try
            {
                IsSubmitting = true;

                // Sauvegarder les données de l'étape actuelle
                SaveCurrentStepData();

                // Handle create vs update
                if (IsEditMode && EditingSpotId > 0)
                {
                    // Update existing spot
                    _logger.LogInformation("Updating existing spot {SpotId}", EditingSpotId);
                    
                    // Keep the original validation status for updates
                    // (don't reset to Pending unless specifically required)
                    
                    await _spotRepository.UpdateAsync(NewSpot).ConfigureAwait(false);
                    await _spotRepository.SaveChangesAsync().ConfigureAwait(false);
                }
                else
                {
                    // Create new spot
                    _logger.LogInformation("Creating new spot");
                    
                    // Ensure CreatorId is set to current authenticated user
                    NewSpot.CreatorId = _authenticationService.CurrentUserId ?? 1;
                    
                    // Determine initial validation status based on user role
                    var validationStatus = GetInitialValidationStatusForNewSpot();
                    NewSpot.ValidationStatus = validationStatus;
                    
                    _logger.LogInformation("New spot will be created with status: {ValidationStatus} for user {UserId} with role: {AccountType}", 
                        validationStatus, NewSpot.CreatorId, _authenticationService.CurrentUser?.AccountType);
                    
                    await _spotRepository.AddAsync(NewSpot).ConfigureAwait(false);
                    await _spotRepository.SaveChangesAsync().ConfigureAwait(false);
                }

                // Une fois le spot sauvegardé, ajouter les photos
                if (PhotosPaths.Count > 0)
                {
                    _logger.LogInformation("Adding {PhotoCount} photos for spot {SpotId}", PhotosPaths.Count, NewSpot.Id);
                    
                    foreach (var photoPath in PhotosPaths)
                    {
                        var isPrimary = photoPath == PrimaryPhotoPath;
                        var media = await _mediaService.CreateSpotMediaAsync(NewSpot.Id, photoPath, isPrimary).ConfigureAwait(false);

                        if (media != null)
                        {
                            await _spotMediaRepository.AddAsync(media).ConfigureAwait(false);
                            _logger.LogDebug("Added media {MediaId} for spot {SpotId}, IsPrimary: {IsPrimary}", media.Id, NewSpot.Id, isPrimary);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create media for photo path: {PhotoPath}", photoPath);
                        }
                    }

                    await _spotMediaRepository.SaveChangesAsync().ConfigureAwait(false);
                    _logger.LogInformation("Successfully saved {PhotoCount} photos for spot {SpotId}", PhotosPaths.Count, NewSpot.Id);
                }

                // Afficher un message de succès
                var successMessage = IsEditMode 
                    ? "Votre spot a été modifié avec succès." 
                    : "Votre spot a été soumis avec succès et sera vérifié par un modérateur.";
                await DialogService.ShowAlertAsync("Succès", successMessage, "OK");

                // Retourner à la carte (stay on main thread for UI updates)
                await NavigationService.NavigateToAsync<ViewModels.Map.MapViewModel>();
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Une erreur est survenue lors de la soumission : {ex.Message}", "OK");
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        /// <summary>
        /// Command to cancel the spot creation process with confirmation
        /// </summary>
        /// <returns>Task representing the asynchronous cancellation operation</returns>
        [RelayCommand]
        private async Task Cancel()
        {
            bool confirm = await DialogService.ShowConfirmationAsync(
                "Confirmation",
                "Êtes-vous sûr de vouloir annuler ? Les données non enregistrées seront perdues.",
                "Oui",
                "Non");

            if (confirm)
            {
                // Stay on main thread for navigation to avoid UI threading issues
                await NavigationService.GoBackAsync();
            }
        }

        /// <summary>
        /// Determines the initial validation status for a new spot based on the current user's role
        /// Admin and moderator spots are automatically approved, regular users need validation
        /// </summary>
        /// <returns>Initial validation status for the new spot</returns>
        private SpotValidationStatus GetInitialValidationStatusForNewSpot()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null)
            {
                _logger.LogWarning("No authenticated user found, defaulting to Pending status");
                return SpotValidationStatus.Pending;
            }

            // Auto-approve spots created by administrators and moderators
            if (currentUser.AccountType == AccountType.Administrator || 
                currentUser.AccountType == AccountType.ExpertModerator)
            {
                _logger.LogInformation("User {UserId} with role {AccountType} creating spot - auto-approving", 
                    currentUser.Id, currentUser.AccountType);
                return SpotValidationStatus.Approved;
            }

            // Regular users and professionals need validation
            _logger.LogInformation("User {UserId} with role {AccountType} creating spot - requires validation", 
                currentUser.Id, currentUser.AccountType);
            return SpotValidationStatus.Pending;
        }

        // Back command for header navigation
        public IRelayCommand BackCommand => CancelCommand;
    }
}