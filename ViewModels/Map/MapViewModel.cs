using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Map
{
    public partial class MapViewModel : ViewModelBase
    {
        private readonly ISpotRepository _spotRepository;
        private readonly ILocationService _locationService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        // Utilisez le nom complet avec le namespace pour Spot
        private ObservableCollection<Models.Domain.Spot> _spots;

        [ObservableProperty]
        private double _userLatitude;

        [ObservableProperty]
        private double _userLongitude;

        [ObservableProperty]
        private bool _isLocationAvailable;

        public MapViewModel(
            ISpotRepository spotRepository,
            ILocationService locationService,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _spotRepository = spotRepository;
            _locationService = locationService;

            // Utilisez le nom complet pour éviter les conflits
            Spots = new ObservableCollection<Models.Domain.Spot>();
            Title = "Carte";
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            await RefreshLocationCommand.ExecuteAsync(null);
            await LoadSpotsCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private async Task LoadSpots()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;

                var spots = await _spotRepository.GetAllAsync();

                Spots.Clear();
                foreach (var spot in spots)
                {
                    Spots.Add(spot);
                }
            }
            catch (Exception ex)
            {
                // Log error
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les spots. Veuillez réessayer plus tard.", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshLocation()
        {
            try
            {
                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    // Conversion de decimal à double pour l'affichage
                    UserLatitude = Convert.ToDouble(location.Latitude);
                    UserLongitude = Convert.ToDouble(location.Longitude);
                    IsLocationAvailable = true;
                }
                else
                {
                    IsLocationAvailable = false;
                }
            }
            catch (Exception ex)
            {
                IsLocationAvailable = false;
                await DialogService.ShowAlertAsync("Localisation", "La géolocalisation n'est pas disponible.", "OK");
            }
        }

        [RelayCommand]
        private async Task FilterSpots(string filterType)
        {
            // Implement filtering logic based on the type
            if (string.IsNullOrEmpty(filterType))
                return;

            try
            {
                IsLoading = true;

                // Convertir le filterType en typeId
                int typeId;
                switch (filterType.ToLower())
                {
                    case "diving":
                        typeId = 1; // Supposons que 1 = plongée
                        break;
                    case "freediving":
                        typeId = 2; // Supposons que 2 = apnée
                        break;
                    case "snorkeling":
                        typeId = 3; // Supposons que 3 = randonnée
                        break;
                    default:
                        typeId = 0;
                        break;
                }

                if (typeId > 0)
                {
                    var filteredSpots = await _spotRepository.GetSpotsByTypeAsync(typeId);

                    Spots.Clear();
                    foreach (var spot in filteredSpots)
                    {
                        Spots.Add(spot);
                    }
                }
                else
                {
                    // Si pas de filtre valide, charger tous les spots
                    await LoadSpotsCommand.ExecuteAsync(null);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de filtrer les spots. Veuillez réessayer plus tard.", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        // Utilisez le nom complet pour le paramètre
        private async Task SpotSelected(Models.Domain.Spot spot)
        {
            if (spot == null) return;

            // Pour naviguer vers les détails du spot
            await NavigationService.NavigateToAsync<ViewModels.Spot.SpotDetailsViewModel>(spot.Id);
        }

        [RelayCommand]
        private async Task NavigateToAddSpot()
        {
            // Pour naviguer vers l'ajout d'un spot
            await NavigationService.NavigateToAsync<ViewModels.Spot.AddSpotViewModel>();
        }
    }
}