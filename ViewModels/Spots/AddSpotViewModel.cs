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
using Microsoft.Extensions.Logging;

namespace SubExplore.ViewModels.Spots
{
    public partial class AddSpotViewModel : ViewModelBase
    {
        private readonly ILocationService _locationService;
        private readonly ISpotRepository _spotRepository;
        private readonly ISpotTypeRepository _spotTypeRepository;
        private readonly ISpotMediaRepository _spotMediaRepository;
        private readonly IMediaService _mediaService;
        private readonly ISettingsService _settingsService;
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
        private ObservableCollection<SpotType> _availableSpotTypes;

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

        public AddSpotViewModel(
            ILocationService locationService,
            ISpotRepository spotRepository,
            ISpotTypeRepository spotTypeRepository,
            ISpotMediaRepository spotMediaRepository,
            IMediaService mediaService,
            ISettingsService settingsService,
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
            _logger = logger;
            
            // Initialize validators
            _locationValidator = new LocationStepValidator();
            _characteristicsValidator = new CharacteristicsStepValidator();
            _photosValidator = new PhotosStepValidator();
            _summaryValidator = new SummaryStepValidator();

            AvailableSpotTypes = new ObservableCollection<SpotType>();
            PhotosPaths = new ObservableCollection<string>();
            CurrentStep = 1;
            Title = "Nouveau spot";

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
                CurrentStrength.Light,
                CurrentStrength.Moderate,
                CurrentStrength.Strong,
                CurrentStrength.Extreme
            };

            // Initialiser un nouveau spot
            NewSpot = new Models.Domain.Spot
            {
                CreatorId = 1, // À remplacer par l'ID de l'utilisateur connecté
                CreatedAt = DateTime.UtcNow,
                ValidationStatus = SpotValidationStatus.Draft
            };

            SelectedCurrentStrength = CurrentStrength.Light;
            SelectedDifficultyLevel = DifficultyLevel.Beginner;
            
            // Initialize coordinates with default values to prevent 0,0 issues
            Latitude = 43.2965m; // Marseille, France
            Longitude = 5.3698m;
            IsLocationReady = false; // Will be set to true after proper initialization
            
            _logger.LogDebug("AddSpotViewModel initialized with default coordinates: {Latitude}, {Longitude}", Latitude, Longitude);
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            _logger.LogDebug("AddSpotViewModel.InitializeAsync called with parameter: {Parameter}", parameter?.ToString() ?? "null");
            _logger.LogDebug("Current coordinates before initialization: {Latitude}, {Longitude}", Latitude, Longitude);
            
            await LoadSpotTypes();
            
            // Handle location parameter passed from navigation
            if (parameter != null)
            {
                _logger.LogDebug("Handling location parameter from navigation");
                await HandleLocationParameter(parameter);
            }
            else
            {
                _logger.LogDebug("No parameter provided, trying to get current location");
                await TryGetCurrentLocation();
            }
            
            _logger.LogDebug("Final coordinates after initialization: {Latitude}, {Longitude}", Latitude, Longitude);
            
            // Mark location as ready for UI binding
            IsLocationReady = true;
            _logger.LogDebug("Location is now ready for UI binding");
        }

        private async Task LoadSpotTypes()
        {
            try
            {
                var types = await _spotTypeRepository.GetActiveTypesAsync();

                AvailableSpotTypes.Clear();
                foreach (var type in types)
                {
                    AvailableSpotTypes.Add(type);
                }

                if (AvailableSpotTypes.Count > 0)
                {
                    SelectedSpotType = AvailableSpotTypes[0];
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les types de spots.", "OK");
            }
        }

        private async Task TryGetCurrentLocation()
        {
            try
            {
                _logger.LogDebug("Attempting to get current location");
                var location = await _locationService.GetCurrentLocationAsync();
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
                    // Utiliser une position par défaut (Marseille)
                    Latitude = 43.2965m;
                    Longitude = 5.3698m;
                    _logger.LogInformation("Location not available, using default location: Marseille");
                }
            }
            catch (Exception ex)
            {
                HasUserLocation = false;
                // Utiliser une position par défaut (Marseille)
                Latitude = 43.2965m;
                Longitude = 5.3698m;
                _logger.LogError(ex, "Error getting current location, using default location: Marseille");
            }
        }

        private async Task HandleLocationParameter(object parameter)
        {
            try
            {
                _logger.LogDebug("Handling location parameter from navigation");
                
                // Try to extract location from parameter object
                var parameterType = parameter.GetType();
                var latitudeProperty = parameterType.GetProperty("Latitude");
                var longitudeProperty = parameterType.GetProperty("Longitude");
                
                if (latitudeProperty != null && longitudeProperty != null)
                {
                    var latValue = latitudeProperty.GetValue(parameter);
                    var lngValue = longitudeProperty.GetValue(parameter);
                    
                    if (latValue != null && lngValue != null)
                    {
                        Latitude = Convert.ToDecimal(latValue);
                        Longitude = Convert.ToDecimal(lngValue);
                        HasUserLocation = true;
                        _logger.LogInformation("Using location from navigation: {Latitude}, {Longitude}", Latitude, Longitude);
                        return;
                    }
                }
                
                // Fallback to getting current location if parameter extraction fails
                _logger.LogWarning("Failed to extract location from navigation parameter, falling back to current location");
                await TryGetCurrentLocation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling location parameter, falling back to current location");
                await TryGetCurrentLocation();
            }
        }

        [RelayCommand]
        private async Task TakePhoto()
        {
            var photoPath = await _mediaService.TakePhotoAsync();
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

        [RelayCommand]
        private async Task PickPhoto()
        {
            var photoPath = await _mediaService.PickPhotoAsync();
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

        [RelayCommand]
        private void SetPrimaryPhoto(string path)
        {
            if (PhotosPaths.Contains(path))
            {
                PrimaryPhotoPath = path;
            }
        }

        [RelayCommand]
        private async Task NextStep()
        {
            if (!await ValidateCurrentStep())
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
                await SubmitSpot();
            }
        }

        [RelayCommand]
        private void PreviousStep()
        {
            CurrentStep--;
            if (CurrentStep < 1)
            {
                CurrentStep = 1;
            }
        }


        [RelayCommand]
        private async Task GetCurrentLocation()
        {
            try
            {
                IsLoading = true;
                _logger.LogInformation("Getting current location...");
                
                var location = await _locationService.GetCurrentLocationAsync();
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

        [RelayCommand]
        private void SpotTypeSelected(SpotType selectedType)
        {
            SelectedSpotType = selectedType;
            _logger.LogInformation("Spot type selected: {SpotTypeName}", selectedType?.Name ?? "null");
        }

        private async Task<bool> ValidateCurrentStep()
        {
            ValidationResult result;
            
            switch (CurrentStep)
            {
                case 1: // Localisation
                    var locationData = new LocationStepData
                    {
                        Latitude = Latitude,
                        Longitude = Longitude,
                        AccessDescription = AccessDescription,
                        HasUserLocation = HasUserLocation
                    };
                    result = _locationValidator.Validate(locationData);
                    break;

                case 2: // Caractéristiques
                    var characteristicsData = new CharacteristicsStepData
                    {
                        SpotName = SpotName,
                        SelectedSpotType = SelectedSpotType,
                        SelectedDifficultyLevel = SelectedDifficultyLevel,
                        MaxDepth = MaxDepth,
                        RequiredEquipment = RequiredEquipment,
                        SafetyNotes = SafetyNotes,
                        BestConditions = BestConditions,
                        SelectedCurrentStrength = SelectedCurrentStrength
                    };
                    result = _characteristicsValidator.Validate(characteristicsData);
                    break;

                case 3: // Photos
                    var photosData = new PhotosStepData
                    {
                        PhotosPaths = PhotosPaths.ToList(),
                        PrimaryPhotoPath = PrimaryPhotoPath,
                        MaxPhotosAllowed = 3
                    };
                    result = _photosValidator.Validate(photosData);
                    break;

                case 4: // Récapitulatif
                    var summaryData = new SummaryStepData
                    {
                        Location = new LocationStepData
                        {
                            Latitude = Latitude,
                            Longitude = Longitude,
                            AccessDescription = AccessDescription,
                            HasUserLocation = HasUserLocation
                        },
                        Characteristics = new CharacteristicsStepData
                        {
                            SpotName = SpotName,
                            SelectedSpotType = SelectedSpotType,
                            SelectedDifficultyLevel = SelectedDifficultyLevel,
                            MaxDepth = MaxDepth,
                            RequiredEquipment = RequiredEquipment,
                            SafetyNotes = SafetyNotes,
                            BestConditions = BestConditions,
                            SelectedCurrentStrength = SelectedCurrentStrength
                        },
                        Photos = new PhotosStepData
                        {
                            PhotosPaths = PhotosPaths.ToList(),
                            PrimaryPhotoPath = PrimaryPhotoPath,
                            MaxPhotosAllowed = 3
                        }
                    };
                    result = _summaryValidator.Validate(summaryData);
                    break;

                default:
                    return true;
            }
            
            if (!result.IsValid)
            {
                var errorMessage = string.Join("\n", result.Errors);
                await DialogService.ShowAlertAsync("Validation", errorMessage, "OK");
                _logger.LogWarning("Step validation failed for {StepName}: {Errors}", result.StepName, string.Join(", ", result.Errors));
                return false;
            }
            
            return true;
        }

        private void SaveCurrentStepData()
        {
            switch (CurrentStep)
            {
                case 1: // Localisation
                    NewSpot.Latitude = Latitude;
                    NewSpot.Longitude = Longitude;
                    NewSpot.Description = AccessDescription;
                    break;

                case 2: // Caractéristiques
                    NewSpot.Name = SpotName;
                    NewSpot.TypeId = SelectedSpotType.Id;
                    NewSpot.DifficultyLevel = SelectedDifficultyLevel;
                    NewSpot.MaxDepth = MaxDepth;
                    NewSpot.RequiredEquipment = RequiredEquipment;
                    NewSpot.SafetyNotes = SafetyNotes;
                    NewSpot.BestConditions = BestConditions;
                    NewSpot.CurrentStrength = SelectedCurrentStrength;
                    break;

                case 3: // Photos - juste passer à l'étape suivante
                    break;

                case 4: // Récapitulatif
                    break;
            }
        }

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

                // Mise à jour du statut
                NewSpot.ValidationStatus = SpotValidationStatus.Pending;

                // Enregistrer le spot
                await _spotRepository.AddAsync(NewSpot);
                await _spotRepository.SaveChangesAsync();

                // Une fois le spot sauvegardé, ajouter les photos
                if (PhotosPaths.Count > 0)
                {
                    _logger.LogInformation("Adding {PhotoCount} photos for spot {SpotId}", PhotosPaths.Count, NewSpot.Id);
                    
                    foreach (var photoPath in PhotosPaths)
                    {
                        var isPrimary = photoPath == PrimaryPhotoPath;
                        var media = await _mediaService.CreateSpotMediaAsync(NewSpot.Id, photoPath, isPrimary);

                        if (media != null)
                        {
                            await _spotMediaRepository.AddAsync(media);
                            _logger.LogDebug("Added media {MediaId} for spot {SpotId}, IsPrimary: {IsPrimary}", media.Id, NewSpot.Id, isPrimary);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create media for photo path: {PhotoPath}", photoPath);
                        }
                    }

                    await _spotMediaRepository.SaveChangesAsync();
                    _logger.LogInformation("Successfully saved {PhotoCount} photos for spot {SpotId}", PhotosPaths.Count, NewSpot.Id);
                }

                // Afficher un message de succès
                await DialogService.ShowAlertAsync("Succès", "Votre spot a été soumis avec succès et sera vérifié par un modérateur.", "OK");

                // Retourner à la carte
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
                await NavigationService.GoBackAsync();
            }
        }
    }
}