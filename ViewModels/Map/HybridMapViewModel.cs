using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Map
{
    /// <summary>
    /// MapViewModel hybride utilisant Entity Framework + PostGIS pour les performances
    /// avec fallback vers l'API Supabase si nécessaire
    /// </summary>
    public partial class HybridMapViewModel : ViewModelBase
    {
        // SERVICE HYBRIDE PRINCIPAL
        private readonly IHybridMapService _hybridMapService;
        private readonly ILocationService _locationService;

        [ObservableProperty]
        private Location? currentLocation;

        [ObservableProperty]
        private bool isLocationLoading;

        [ObservableProperty]
        private string selectedFilter = "all";

        // COLLECTIONS DOMAIN MODELS (Entity Framework)
        public ObservableCollection<PracticeSpot> PracticeSpots { get; } = new();
        public ObservableCollection<Organization> Organizations { get; } = new();
        public ObservableCollection<Business> Businesses { get; } = new();
        public ObservableCollection<SpotType> SpotTypes { get; } = new();
        
        public ObservableCollection<Pin> Pins { get; } = new();

        // Propriétés de filtrage
        [ObservableProperty]
        private bool showPracticeSpots = true;

        [ObservableProperty]
        private bool showOrganizations = true;

        [ObservableProperty]
        private bool showBusinesses = true;

        [ObservableProperty]
        private int searchRadius = 10;

        [ObservableProperty]
        private double mapLatitude = 48.8566; // Default Paris

        [ObservableProperty]
        private double mapLongitude = 2.3522; // Default Paris

        [ObservableProperty]
        private double mapZoomLevel = 12;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isLocationAvailable;

        [ObservableProperty]
        private string searchText = string.Empty;

        // Mini-window properties
        [ObservableProperty]
        private bool isSpotMiniWindowVisible;

        [ObservableProperty]
        private object? selectedEntity; // Can be PracticeSpot, Organization, or Business

        public string SelectedEntityName => SelectedEntity switch
        {
            PracticeSpot spot => spot.Name ?? string.Empty,
            Organization org => org.Name ?? string.Empty,
            Business business => business.Name ?? string.Empty,
            _ => string.Empty
        };

        partial void OnSelectedEntityChanged(object? value)
        {
            OnPropertyChanged(nameof(SelectedEntityName));
        }

        // Computed property for empty state
        public bool IsEmptyState => 
            PracticeSpots.Count == 0 && 
            Organizations.Count == 0 && 
            Businesses.Count == 0 && 
            !IsBusy;

        [ObservableProperty]
        private bool isNetworkError = false;

        public HybridMapViewModel(
            IHybridMapService hybridMapService,
            ILocationService locationService,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _hybridMapService = hybridMapService;
            _locationService = locationService;

            Title = "Carte Hybride";
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            await GetCurrentLocationAsync();
            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                IsNetworkError = false;

                var tasks = new List<Task>();

                // Load SpotTypes first (needed for filtering)
                tasks.Add(LoadSpotTypesAsync());

                // Si nous avons une position, charger les données géospatiales
                if (CurrentLocation != null)
                {
                    if (ShowPracticeSpots)
                    {
                        tasks.Add(LoadNearbyPracticeSpotsAsync());
                    }

                    if (ShowOrganizations)
                    {
                        tasks.Add(LoadNearbyOrganizationsAsync());
                    }

                    if (ShowBusinesses)
                    {
                        tasks.Add(LoadNearbyBusinessesAsync());
                    }
                }
                else
                {
                    // Fallback : charger quelques données par défaut
                    if (ShowPracticeSpots)
                    {
                        tasks.Add(LoadDefaultPracticeSpotsAsync());
                    }
                }

                await Task.WhenAll(tasks);
                UpdatePins();
                OnPropertyChanged(nameof(IsEmptyState));
            }
            catch (Exception ex)
            {
                IsNetworkError = true;
                await DialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de charger les données: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // MÉTHODES HYBRIDES UTILISANT ENTITY FRAMEWORK + POSTGIS
        private async Task LoadNearbyPracticeSpotsAsync()
        {
            try
            {
                if (CurrentLocation == null) return;

                var practiceSpots = await _hybridMapService.GetNearbyPracticeSpotsAsync(
                    (decimal)CurrentLocation.Latitude,
                    (decimal)CurrentLocation.Longitude,
                    SearchRadius);

                PracticeSpots.Clear();
                foreach (var spot in practiceSpots)
                {
                    PracticeSpots.Add(spot);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur PostGIS", 
                    "Impossible de charger les spots de pratique avec PostGIS.", "OK");
            }
        }

        private async Task LoadNearbyOrganizationsAsync()
        {
            try
            {
                if (CurrentLocation == null) return;

                var organizations = await _hybridMapService.GetNearbyOrganizationsAsync(
                    (decimal)CurrentLocation.Latitude,
                    (decimal)CurrentLocation.Longitude,
                    SearchRadius);

                Organizations.Clear();
                foreach (var org in organizations)
                {
                    Organizations.Add(org);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", 
                    "Impossible de charger les organisations.", "OK");
            }
        }

        private async Task LoadNearbyBusinessesAsync()
        {
            try
            {
                if (CurrentLocation == null) return;

                var businesses = await _hybridMapService.GetNearbyBusinessesAsync(
                    (decimal)CurrentLocation.Latitude,
                    (decimal)CurrentLocation.Longitude,
                    SearchRadius);

                Businesses.Clear();
                foreach (var business in businesses)
                {
                    Businesses.Add(business);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", 
                    "Impossible de charger les commerces.", "OK");
            }
        }

        private async Task LoadDefaultPracticeSpotsAsync()
        {
            try
            {
                // Charger quelques spots par défaut près de Paris
                var practiceSpots = await _hybridMapService.GetNearbyPracticeSpotsAsync(
                    48.8566m, 2.3522m, 50); // 50km autour de Paris

                PracticeSpots.Clear();
                foreach (var spot in practiceSpots.Take(20)) // Limiter à 20 spots
                {
                    PracticeSpots.Add(spot);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", 
                    "Impossible de charger les spots par défaut.", "OK");
            }
        }

        private async Task LoadSpotTypesAsync()
        {
            try
            {
                var spotTypes = await _hybridMapService.GetActiveSpotTypesAsync();

                SpotTypes.Clear();
                foreach (var spotType in spotTypes)
                {
                    SpotTypes.Add(spotType);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de charger les types de spots: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadDataAsync();
                return;
            }

            try
            {
                IsBusy = true;

                var searchResults = await _hybridMapService.SearchPracticeSpotsAsync(SearchText);

                PracticeSpots.Clear();
                foreach (var spot in searchResults)
                {
                    PracticeSpots.Add(spot);
                }

                UpdatePins();
                OnPropertyChanged(nameof(IsEmptyState));
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", 
                    $"Impossible d'effectuer la recherche: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task FilterByTypeAsync(SpotType spotType)
        {
            try
            {
                IsBusy = true;

                var filteredSpots = await _hybridMapService.GetPracticeSpotsByTypeAsync(spotType.Id);

                PracticeSpots.Clear();
                foreach (var spot in filteredSpots)
                {
                    PracticeSpots.Add(spot);
                }

                UpdatePins();
                OnPropertyChanged(nameof(IsEmptyState));
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de filtrer par type: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task FilterByDifficultyAsync(DifficultyLevel difficulty)
        {
            try
            {
                IsBusy = true;

                var filteredSpots = await _hybridMapService.GetPracticeSpotsByDifficultyAsync(difficulty);

                PracticeSpots.Clear();
                foreach (var spot in filteredSpots)
                {
                    PracticeSpots.Add(spot);
                }

                UpdatePins();
                OnPropertyChanged(nameof(IsEmptyState));
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de filtrer par difficulté: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdatePins()
        {
            Pins.Clear();

            // Add pins for PracticeSpots
            if (ShowPracticeSpots)
            {
                foreach (var spot in PracticeSpots)
                {
                    var pin = new Pin
                    {
                        Label = spot.Name ?? "Spot de pratique",
                        Address = spot.Description ?? "",
                        Type = PinType.Place,
                        Location = new Location((double)spot.Latitude, (double)spot.Longitude)
                    };
                    pin.MarkerClicked += (s, e) => OnPinClicked(spot);
                    Pins.Add(pin);
                }
            }

            // Add pins for Organizations
            if (ShowOrganizations)
            {
                foreach (var org in Organizations)
                {
                    var pin = new Pin
                    {
                        Label = $"🏢 {org.Name}",
                        Address = $"{org.OrganizationType} - {org.Address}",
                        Type = PinType.Place,
                        Location = new Location((double)org.Latitude, (double)org.Longitude)
                    };
                    pin.MarkerClicked += (s, e) => OnPinClicked(org);
                    Pins.Add(pin);
                }
            }

            // Add pins for Businesses
            if (ShowBusinesses)
            {
                foreach (var business in Businesses)
                {
                    var pin = new Pin
                    {
                        Label = $"🏪 {business.Name}",
                        Address = $"{business.BusinessType} - {business.Address}",
                        Type = PinType.Place,
                        Location = new Location((double)business.Latitude, (double)business.Longitude)
                    };
                    pin.MarkerClicked += (s, e) => OnPinClicked(business);
                    Pins.Add(pin);
                }
            }
        }

        private void OnPinClicked(object entity)
        {
            SelectedEntity = entity;
            IsSpotMiniWindowVisible = true;
        }

        [RelayCommand]
        public void CloseMiniWindow()
        {
            IsSpotMiniWindowVisible = false;
            SelectedEntity = null;
        }

        [RelayCommand]
        public async Task GetCurrentLocationAsync()
        {
            try
            {
                IsLocationLoading = true;

                var locationCoords = await _locationService.GetCurrentLocationAsync();
                if (locationCoords != null)
                {
                    // Convert LocationCoordinates to MAUI Location
                    CurrentLocation = new Location((double)locationCoords.Latitude, (double)locationCoords.Longitude);
                    MapLatitude = (double)locationCoords.Latitude;
                    MapLongitude = (double)locationCoords.Longitude;
                    MapZoomLevel = 14; // Zoom plus proche pour la position actuelle
                    IsLocationAvailable = true;
                }
                else
                {
                    IsLocationAvailable = false;
                    await DialogService.ShowAlertAsync("Localisation", 
                        "Impossible d'obtenir votre position actuelle.", "OK");
                }
            }
            catch (Exception ex)
            {
                IsLocationAvailable = false;
                await DialogService.ShowAlertAsync("Erreur de localisation", 
                    $"Erreur lors de la récupération de votre position: {ex.Message}", "OK");
            }
            finally
            {
                IsLocationLoading = false;
            }
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            await LoadDataAsync();
        }

        // Event handlers for filter changes
        partial void OnShowPracticeSpotsChanged(bool value)
        {
            _ = Task.Run(async () => await LoadDataAsync());
        }

        partial void OnShowOrganizationsChanged(bool value)
        {
            _ = Task.Run(async () => await LoadDataAsync());
        }

        partial void OnShowBusinessesChanged(bool value)
        {
            _ = Task.Run(async () => await LoadDataAsync());
        }

        partial void OnSearchRadiusChanged(int value)
        {
            _ = Task.Run(async () => await LoadDataAsync());
        }
    }
}