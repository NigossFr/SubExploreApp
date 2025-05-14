using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Map
{
    public partial class MapViewModel : ViewModelBase
    {
        private readonly ISpotRepository _spotRepository;
        private readonly ILocationService _locationService;
        private readonly ISpotTypeRepository _spotTypeRepository;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        [ObservableProperty]
        private ObservableCollection<Models.Domain.Spot> _spots;

        [ObservableProperty]
        private ObservableCollection<Pin> _pins;

        [ObservableProperty]
        private double _userLatitude;

        [ObservableProperty]
        private double _userLongitude;

        [ObservableProperty]
        private double _mapLatitude;

        [ObservableProperty]
        private double _mapLongitude;

        [ObservableProperty]
        private double _mapZoomLevel;

        [ObservableProperty]
        private bool _isLocationAvailable;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private ObservableCollection<SpotType> _spotTypes;

        [ObservableProperty]
        private SpotType _selectedSpotType;

        [ObservableProperty]
        private bool _isFiltering;

        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private MapSpan _visibleRegion;

        public MapViewModel(
            ISpotRepository spotRepository,
            ILocationService locationService,
            ISpotTypeRepository spotTypeRepository,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _spotRepository = spotRepository;
            _locationService = locationService;
            _spotTypeRepository = spotTypeRepository;
            _configuration = configuration;

            Spots = new ObservableCollection<Models.Domain.Spot>();
            Pins = new ObservableCollection<Pin>();
            SpotTypes = new ObservableCollection<SpotType>();

            // Valeurs par défaut, seront remplacées par la géolocalisation si disponible
            double defaultLat = _configuration.GetValue<double>("AppSettings:DefaultLatitude", 43.2965);
            double defaultLong = _configuration.GetValue<double>("AppSettings:DefaultLongitude", 5.3698);
            double defaultZoom = _configuration.GetValue<double>("AppSettings:DefaultZoomLevel", 12);

            MapLatitude = defaultLat;
            MapLongitude = defaultLong;
            MapZoomLevel = defaultZoom;

            Title = "Carte";
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            try
            {
                // Récupération des types de spots pour les filtres
                await LoadSpotTypesCommand.ExecuteAsync(null);

                // Tentative de géolocalisation
                await RefreshLocationCommand.ExecuteAsync(null);

                // Chargement des spots
                await LoadSpotsCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Une erreur est survenue lors de l'initialisation : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task LoadSpots()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                IEnumerable<Models.Domain.Spot> spots;

                // Si la géolocalisation est disponible, récupérer les spots à proximité
                if (IsLocationAvailable)
                {
                    spots = await _spotRepository.GetNearbySpots(
                        (decimal)UserLatitude,
                        (decimal)UserLongitude,
                        10.0, // 10km de rayon
                        100); // Maximum 100 spots
                }
                else
                {
                    // Sinon, récupérer tous les spots validés
                    spots = await _spotRepository.GetSpotsByValidationStatusAsync(SpotValidationStatus.Approved);
                }

                Spots.Clear();
                foreach (var spot in spots)
                {
                    Spots.Add(spot);
                }

                UpdatePins();
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les spots. Veuillez réessayer plus tard.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoadSpotTypes()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var types = await _spotTypeRepository.GetActiveTypesAsync();

                SpotTypes.Clear();
                foreach (var type in types)
                {
                    SpotTypes.Add(type);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les types de spots.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RefreshLocation()
        {
            try
            {
                // Vérifier si nous avons déjà les permissions
                bool hasPermission = await _locationService.RequestLocationPermissionAsync();

                if (!hasPermission)
                {
                    IsLocationAvailable = false;
                    await DialogService.ShowAlertAsync("Permissions", "L'accès à la localisation est nécessaire pour utiliser cette fonctionnalité.", "OK");
                    return;
                }

                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    // Conversion de decimal à double pour l'affichage
                    UserLatitude = Convert.ToDouble(location.Latitude);
                    UserLongitude = Convert.ToDouble(location.Longitude);

                    // Centrer la carte sur la position de l'utilisateur
                    MapLatitude = UserLatitude;
                    MapLongitude = UserLongitude;

                    IsLocationAvailable = true;

                    // Recharger les spots à proximité
                    await LoadSpotsCommand.ExecuteAsync(null);
                }
                else
                {
                    IsLocationAvailable = false;
                    await DialogService.ShowAlertAsync("Localisation", "Impossible d'obtenir votre position. Vérifiez que les services de localisation sont activés.", "OK");
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
            if (string.IsNullOrEmpty(filterType) || IsBusy)
                return;

            try
            {
                IsBusy = true;
                IsFiltering = true;

                // Convertir le filterType en typeId
                int typeId;
                switch (filterType.ToLower())
                {
                    case "diving":
                        typeId = 1; // Plongée
                        break;
                    case "freediving":
                        typeId = 2; // Apnée
                        break;
                    case "snorkeling":
                        typeId = 3; // Randonnée
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

                    UpdatePins();
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
                IsBusy = false;
                IsFiltering = false;
            }
        }

        [RelayCommand]
        private async Task FilterSpotsByType(SpotType spotType)
        {
            if (spotType == null || IsBusy)
                return;

            try
            {
                IsBusy = true;
                IsFiltering = true;
                SelectedSpotType = spotType;

                var filteredSpots = await _spotRepository.GetSpotsByTypeAsync(spotType.Id);

                Spots.Clear();
                foreach (var spot in filteredSpots)
                {
                    Spots.Add(spot);
                }

                UpdatePins();

                // Optionnel : zoomer sur les résultats si peu nombreux
                if (Spots.Count > 0 && Spots.Count <= 5)
                {
                    CenterMapOnSpots(Spots);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de filtrer les spots. Veuillez réessayer plus tard.", "OK");
            }
            finally
            {
                IsBusy = false;
                IsFiltering = false;
            }
        }

        [RelayCommand]
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

        [RelayCommand]
        private async Task SearchSpots()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || IsBusy)
                return;

            try
            {
                IsBusy = true;
                IsSearching = true;

                var searchResults = await _spotRepository.SearchSpotsAsync(SearchText);

                Spots.Clear();
                foreach (var spot in searchResults)
                {
                    Spots.Add(spot);
                }

                UpdatePins();

                // Zoom sur les résultats de recherche
                if (Spots.Count > 0)
                {
                    CenterMapOnSpots(Spots);
                }
                else
                {
                    await DialogService.ShowToastAsync("Aucun spot trouvé pour cette recherche");
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'effectuer la recherche. Veuillez réessayer plus tard.", "OK");
            }
            finally
            {
                IsBusy = false;
                IsSearching = false;
            }
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SelectedSpotType = null;
            SearchText = string.Empty;
            IsFiltering = false;
            IsSearching = false;

            // Recharger tous les spots
            LoadSpotsCommand.Execute(null);
        }

        [RelayCommand]
        private void PinSelected(Pin pin)
        {
            if (pin?.BindingContext is Models.Domain.Spot spot)
            {
                SpotSelected(spot);
            }
        }

        [RelayCommand]
        private void MapClicked(Microsoft.Maui.Controls.Maps.MapClickedEventArgs args)
        {
            // Gérer les clics sur la carte
            // Par exemple, vous pourriez permettre l'ajout d'un nouveau spot à cet endroit
        }

        [RelayCommand]
        private void VisibleRegionChanged(MapSpan mapSpan)
        {
            VisibleRegion = mapSpan;

            // Vous pourriez déclencher un chargement de spots dans la région visible
            // si l'utilisateur a déplacé la carte d'une distance significative
        }

        private void UpdatePins()
        {
            Application.Current?.Dispatcher.Dispatch(() => {
                Pins.Clear();

                foreach (var spot in Spots)
                {
                    var pin = new Pin
                    {
                        Label = spot.Name,
                        Address = $"{spot.Type?.Name ?? "Spot"} - {spot.DifficultyLevel}",
                        Location = new Location(Convert.ToDouble(spot.Latitude), Convert.ToDouble(spot.Longitude)),
                        Type = PinType.Place,
                        BindingContext = spot
                    };

                    // Code couleur selon le type
                    if (spot.Type?.ColorCode != null)
                    {
                        // Vous pourriez définir une propriété personnalisée pour la couleur du pin
                        // pin.ImageSource = spot.Type.IconPath;
                    }

                    Pins.Add(pin);
                }
            });
        }

        private void CenterMapOnSpots(IEnumerable<Models.Domain.Spot> spots)
        {
            if (!spots.Any()) return;

            // Calculer le centre du groupe de spots
            double minLat = Convert.ToDouble(spots.Min(s => s.Latitude));
            double maxLat = Convert.ToDouble(spots.Max(s => s.Latitude));
            double minLon = Convert.ToDouble(spots.Min(s => s.Longitude));
            double maxLon = Convert.ToDouble(spots.Max(s => s.Longitude));

            double centerLat = (minLat + maxLat) / 2;
            double centerLon = (minLon + maxLon) / 2;

            // Calculer un niveau de zoom approprié
            double latSpan = maxLat - minLat;
            double lonSpan = maxLon - minLon;

            // Appliquer les valeurs
            MapLatitude = centerLat;
            MapLongitude = centerLon;

            // Le zoom devrait être défini en fonction de l'étendue
            // Plus la valeur est grande, plus on est zoomé
            double maxSpan = Math.Max(latSpan, lonSpan);
            if (maxSpan > 0)
            {
                // Cette formule est approximative et dépend de l'API de carte utilisée
                MapZoomLevel = Math.Max(1, Math.Min(18, Math.Log(180 / maxSpan) / Math.Log(2)));
            }
        }
    }
}