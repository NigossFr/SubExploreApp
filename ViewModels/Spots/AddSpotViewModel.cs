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

namespace SubExplore.ViewModels.Spot
{
    public partial class AddSpotViewModel : ViewModelBase
    {
        private readonly ILocationService _locationService;
        private readonly ISpotRepository _spotRepository;
        private readonly ISpotTypeRepository _spotTypeRepository;
        private readonly IMediaService _mediaService;
        private readonly ISettingsService _settingsService;

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

        public AddSpotViewModel(
            ILocationService locationService,
            ISpotRepository spotRepository,
            ISpotTypeRepository spotTypeRepository,
            IMediaService mediaService,
            ISettingsService settingsService,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _locationService = locationService;
            _spotRepository = spotRepository;
            _spotTypeRepository = spotTypeRepository;
            _mediaService = mediaService;
            _settingsService = settingsService;

            AvailableSpotTypes = new ObservableCollection<SpotType>();
            PhotosPaths = new ObservableCollection<string>();
            CurrentStep = 1;
            Title = "Nouveau spot";

            // Initialiser un nouveau spot
            NewSpot = new Models.Domain.Spot
            {
                CreatorId = 1, // À remplacer par l'ID de l'utilisateur connecté
                CreatedAt = DateTime.UtcNow,
                ValidationStatus = SpotValidationStatus.Draft
            };

            SelectedCurrentStrength = CurrentStrength.Light;
            SelectedDifficultyLevel = DifficultyLevel.Beginner;
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            await LoadSpotTypes();
            await TryGetCurrentLocation();
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
                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    Latitude = location.Latitude;
                    Longitude = location.Longitude;
                    HasUserLocation = true;
                }
                else
                {
                    HasUserLocation = false;

                    // Utiliser une position par défaut (Marseille)
                    Latitude = 43.2965m;
                    Longitude = 5.3698m;
                }
            }
            catch (Exception)
            {
                HasUserLocation = false;

                // Utiliser une position par défaut (Marseille)
                Latitude = 43.2965m;
                Longitude = 5.3698m;
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
            if (!ValidateCurrentStep())
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

        private bool ValidateCurrentStep()
        {
            switch (CurrentStep)
            {
                case 1: // Localisation
                    if (Latitude == 0 || Longitude == 0)
                    {
                        DialogService.ShowAlertAsync("Validation", "La localisation est requise.", "OK");
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(AccessDescription))
                    {
                        DialogService.ShowAlertAsync("Validation", "La description de l'accès est requise.", "OK");
                        return false;
                    }
                    return true;

                case 2: // Caractéristiques
                    if (SelectedSpotType == null)
                    {
                        DialogService.ShowAlertAsync("Validation", "Le type de spot est requis.", "OK");
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(SpotName))
                    {
                        DialogService.ShowAlertAsync("Validation", "Le nom du spot est requis.", "OK");
                        return false;
                    }
                    return true;

                case 3: // Photos
                    if (PhotosPaths.Count == 0)
                    {
                        DialogService.ShowAlertAsync("Validation", "Au moins une photo est requise.", "OK");
                        return false;
                    }
                    return true;

                case 4: // Récapitulatif
                    return true;

                default:
                    return true;
            }
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
                    foreach (var photoPath in PhotosPaths)
                    {
                        var isPrimary = photoPath == PrimaryPhotoPath;
                        var media = await _mediaService.CreateSpotMediaAsync(NewSpot.Id, photoPath, isPrimary);

                        if (media != null)
                        {
                            // Dans une implémentation réelle, vous sauvegarderiez ce média
                            // await _spotMediaRepository.AddAsync(media);
                        }
                    }

                    // await _spotMediaRepository.SaveChangesAsync();
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