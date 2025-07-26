using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Validation;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Spots
{
    public partial class SpotLocationViewModel : ViewModelBase
    {
        private readonly ILocationService _locationService;
        private readonly IValidationService _validationService;

        [ObservableProperty]
        private decimal _latitude;

        [ObservableProperty]
        private decimal _longitude;

        [ObservableProperty]
        private string _accessDescription;

        [ObservableProperty]
        private bool _isMapDragging;

        [ObservableProperty]
        private bool _hasUserLocation;

        [ObservableProperty]
        private bool _isLoadingLocation;

        [ObservableProperty]
        private string _locationButtonText = "Utiliser ma position actuelle";

        [ObservableProperty]
        private bool _isLocationButtonEnabled = true;

        [ObservableProperty]
        private bool _hasValidationMessage;

        [ObservableProperty]
        private string _validationMessage = string.Empty;

        [ObservableProperty]
        private string _validationMessageBackgroundColor = "Transparent";

        [ObservableProperty]
        private string _validationMessageBorderColor = "Transparent";

        [ObservableProperty]
        private string _validationMessageTextColor = "Black";

        [ObservableProperty]
        private bool _hasLatitudeError;

        [ObservableProperty]
        private bool _hasLongitudeError;

        [ObservableProperty]
        private bool _isLocationValid;

        public SpotLocationViewModel(
            ILocationService locationService,
            IValidationService validationService,
            IDialogService dialogService)
            : base(dialogService)
        {
            _locationService = locationService;
            _validationService = validationService;
            Title = "Localisation du spot";
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            if (parameter is (decimal lat, decimal lng, string desc))
            {
                // Initialiser avec des valeurs existantes
                Latitude = lat;
                Longitude = lng;
                AccessDescription = desc;
                HasUserLocation = true;
            }
            else
            {
                // Essayer d'obtenir la position actuelle
                await GetCurrentLocationAsync();
            }
        }

        [RelayCommand]
        private async Task GetCurrentLocation()
        {
            await GetCurrentLocationAsync();
        }

        private async Task GetCurrentLocationAsync()
        {
            try
            {
                IsLoadingLocation = true;
                IsLocationButtonEnabled = false;
                LocationButtonText = "Localisation en cours...";
                
                var hasPermission = await _locationService.RequestLocationPermissionAsync();
                if (!hasPermission)
                {
                    HasUserLocation = false;
                    ShowValidationMessage("Permission de localisation requise", isError: true);
                    return;
                }

                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    Latitude = location.Latitude;
                    Longitude = location.Longitude;
                    HasUserLocation = true;
                    IsLocationValid = true;
                    ShowValidationMessage("Position obtenue avec succès", isError: false);
                }
                else
                {
                    HasUserLocation = false;
                    ShowValidationMessage("Impossible d'obtenir la position. Placez le marqueur manuellement.", isError: true);
                }
            }
            catch (Exception ex)
            {
                HasUserLocation = false;
                ShowValidationMessage("Erreur lors de la récupération de la position", isError: true);
            }
            finally
            {
                IsLoadingLocation = false;
                IsLocationButtonEnabled = true;
                LocationButtonText = "Utiliser ma position actuelle";
            }
        }
        
        private void ShowValidationMessage(string message, bool isError)
        {
            ValidationMessage = message;
            HasValidationMessage = true;
            
            if (isError)
            {
                ValidationMessageBackgroundColor = "#FFEBEE";
                ValidationMessageBorderColor = "#F44336";
                ValidationMessageTextColor = "#D32F2F";
            }
            else
            {
                ValidationMessageBackgroundColor = "#E8F5E8";
                ValidationMessageBorderColor = "#4CAF50";
                ValidationMessageTextColor = "#2E7D32";
            }
            
            // Auto-hide after 3 seconds
            Task.Delay(3000).ContinueWith(_ => 
            {
                HasValidationMessage = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        [RelayCommand]
        private void MapDragStarted()
        {
            IsMapDragging = true;
        }

        [RelayCommand]
        private void MapDragCompleted(Microsoft.Maui.Controls.Maps.MapClickedEventArgs args)
        {
            IsMapDragging = false;

            // Mettre à jour les coordonnées
            Latitude = (decimal)args.Location.Latitude;
            Longitude = (decimal)args.Location.Longitude;
        }

        [RelayCommand]
        private async Task ValidateLocation()
        {
            var validationResult = _validationService.ValidateLocation(Latitude, Longitude, AccessDescription);
            
            if (!validationResult.IsValid)
            {
                ShowValidationMessage(validationResult.GetErrorsText(), isError: true);
                IsLocationValid = false;
                return;
            }
            
            if (validationResult.HasWarnings)
            {
                var shouldContinue = await DialogService.ShowConfirmationAsync(
                    "Attention", 
                    $"Des avertissements ont été détectés:\n{validationResult.GetWarningsText()}\n\nVoulez-vous continuer ?", 
                    "Continuer", 
                    "Corriger");
                    
                if (!shouldContinue)
                {
                    IsLocationValid = false;
                    return;
                }
            }
            
            IsLocationValid = true;
            ShowValidationMessage("Localisation validée avec succès", isError: false);
        }
        
        // Auto-validate when coordinates change
        partial void OnLatitudeChanged(decimal value)
        {
            ValidateCoordinatesRealTime();
        }
        
        partial void OnLongitudeChanged(decimal value)
        {
            ValidateCoordinatesRealTime();
        }
        
        private void ValidateCoordinatesRealTime()
        {
            // Quick validation for immediate feedback
            HasLatitudeError = Latitude < -90 || Latitude > 90;
            HasLongitudeError = Longitude < -180 || Longitude > 180;
            
            // Update validity status
            IsLocationValid = !HasLatitudeError && !HasLongitudeError;
        }
    }
}
