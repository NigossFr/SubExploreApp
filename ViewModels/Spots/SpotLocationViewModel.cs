using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Spot
{
    public partial class SpotLocationViewModel : ViewModelBase
    {
        private readonly ILocationService _locationService;

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

        public SpotLocationViewModel(
            ILocationService locationService,
            IDialogService dialogService)
            : base(dialogService)
        {
            _locationService = locationService;
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
                var hasPermission = await _locationService.RequestLocationPermissionAsync();
                if (!hasPermission)
                {
                    HasUserLocation = false;
                    await DialogService.ShowAlertAsync("Permission", "L'accès à la localisation est nécessaire pour cette fonctionnalité.", "OK");
                    return;
                }

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
                    await DialogService.ShowAlertAsync("Localisation", "Impossible d'obtenir votre position. Vous pouvez placer le marqueur manuellement sur la carte.", "OK");
                }
            }
            catch (Exception ex)
            {
                HasUserLocation = false;
                await DialogService.ShowAlertAsync("Erreur", "Une erreur est survenue lors de la récupération de votre position.", "OK");
            }
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
        private void ValidateLocation()
        {
            // Cette méthode serait appelée pour valider cette étape
            // Le ViewModel parent (AddSpotViewModel) gère la transition d'étapes
        }
    }
}
