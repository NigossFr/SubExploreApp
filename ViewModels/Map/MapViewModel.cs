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
    public partial class MapViewModel : ViewModelBase
    {
        // SERVICES SUPABASE NATIFS
        private readonly ISupabaseApiService _supabaseApiService;
        private readonly ISupabaseSpotTypeService _spotTypeService;
        private readonly ILocationService _locationService;

        [ObservableProperty]
        private Location? currentLocation;

        [ObservableProperty]
        private bool isLocationLoading;

        [ObservableProperty]
        private string selectedFilter = "all";

        // COLLECTIONS SUPABASE POUR LES 3 ENTITÉS
        public ObservableCollection<Models.Supabase.SupabasePracticeSpot> PracticeSpots { get; } = new();
        public ObservableCollection<Models.Supabase.SupabaseOrganization> Organizations { get; } = new();
        public ObservableCollection<Models.Supabase.SupabaseBusiness> Businesses { get; } = new();
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
            Models.Supabase.SupabasePracticeSpot spot => spot.Name ?? string.Empty,
            Models.Supabase.SupabaseOrganization org => org.Name ?? string.Empty,
            Models.Supabase.SupabaseBusiness business => business.Name ?? string.Empty,
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

        public MapViewModel(
            ISupabaseApiService supabaseApiService,
            ISupabaseSpotTypeService spotTypeService,
            ILocationService locationService,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _supabaseApiService = supabaseApiService;
            _spotTypeService = spotTypeService;
            _locationService = locationService;

            Title = "Carte";
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            await GetCurrentLocationAsync();
            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsBusy || CurrentLocation == null)
                return;

            try
            {
                IsBusy = true;

                var tasks = new List<Task>();

                // Load SpotTypes first (needed for filtering)
                tasks.Add(LoadSpotTypesAsync());

                if (ShowPracticeSpots)
                {
                    tasks.Add(LoadPracticeSpotsAsync());
                }

                if (ShowOrganizations)
                {
                    tasks.Add(LoadOrganizationsAsync());
                }

                if (ShowBusinesses)
                {
                    tasks.Add(LoadBusinessesAsync());
                }

                await Task.WhenAll(tasks);
                UpdatePins();
                OnPropertyChanged(nameof(IsEmptyState));
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les données.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // MÉTHODES POUR LA NOUVELLE ARCHITECTURE 3-TABLES SUPABASE
        private async Task LoadPracticeSpotsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📥 MapViewModel: Début chargement Practice Spots");
                
                var practiceSpots = await _supabaseApiService.GetPracticeSpotsAsync();
                
                System.Diagnostics.Debug.WriteLine($"📊 MapViewModel: {practiceSpots.Count} Practice Spots récupérés du service");

                PracticeSpots.Clear();
                foreach (var spot in practiceSpots)
                {
                    System.Diagnostics.Debug.WriteLine($"➕ MapViewModel: Ajout Practice Spot: ID={spot.Id}, Name='{spot.Name}'");
                    PracticeSpots.Add(spot);
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ MapViewModel: {PracticeSpots.Count} Practice Spots ajoutés à la collection");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ MapViewModel: Erreur Practice Spots: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ MapViewModel: Stack trace: {ex.StackTrace}");
                await DialogService.ShowAlertAsync("Erreur", $"Impossible de charger les spots de pratique: {ex.Message}", "OK");
            }
        }

        private async Task LoadOrganizationsAsync()
        {
            try
            {
                var organizations = await _supabaseApiService.GetOrganizationsAsync();

                Organizations.Clear();
                foreach (var org in organizations)
                {
                    Organizations.Add(org);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les organisations.", "OK");
            }
        }

        private async Task LoadBusinessesAsync()
        {
            try
            {
                var businesses = await _supabaseApiService.GetBusinessesAsync();

                Businesses.Clear();
                foreach (var business in businesses)
                {
                    Businesses.Add(business);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les commerces.", "OK");
            }
        }

        private async Task LoadSpotTypesAsync()
        {
            try
            {
                var supabaseSpotTypes = await _spotTypeService.GetActiveSpotTypesAsync();
                var spotTypes = supabaseSpotTypes.Select(st => _spotTypeService.ConvertToDomainModel(st)).ToList();

                SpotTypes.Clear();
                foreach (var spotType in spotTypes)
                {
                    SpotTypes.Add(spotType);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Impossible de charger les types de spots: {ex.Message}", "OK");
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
                        Location = new Location((double)spot.Latitude, (double)spot.Longitude),
                        BindingContext = new { Type = "PracticeSpot", Data = spot }
                    };
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
                        Label = org.Name ?? "Organisation",
                        Address = org.Description ?? "",
                        Type = PinType.Place,
                        Location = new Location((double)org.Latitude, (double)org.Longitude),
                        BindingContext = new { Type = "Organization", Data = org }
                    };
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
                        Label = business.Name ?? "Commerce",
                        Address = business.Description ?? "",
                        Type = PinType.Place,
                        Location = new Location((double)business.Latitude, (double)business.Longitude),
                        BindingContext = new { Type = "Business", Data = business }
                    };
                    Pins.Add(pin);
                }
            }
        }

        // Commandes de navigation pour entités Supabase
        [RelayCommand]
        public async Task ShowPracticeSpotDetailsAsync(Models.Supabase.SupabasePracticeSpot spot)
        {
            if (spot != null)
            {
                try
                {
                    // Convert SupabasePracticeSpot to Spot model for SpotDetailsViewModel
                    var domainSpot = ConvertSupabasePracticeSpotToDomainSpot(spot);
                    await NavigationService.NavigateToAsync<ViewModels.Spots.SpotDetailsViewModel>(domainSpot);
                }
                catch (Exception ex)
                {
                    await DialogService.ShowAlertAsync("Erreur", $"Impossible d'afficher les détails du spot : {ex.Message}", "OK");
                }
            }
        }

        [RelayCommand]
        public async Task ShowOrganizationDetailsAsync(Models.Supabase.SupabaseOrganization org)
        {
            if (org != null)
            {
                try
                {
                    await NavigationService.NavigateToAsync<ViewModels.Organizations.OrganizationDetailsViewModel>(org);
                }
                catch (Exception ex)
                {
                    await DialogService.ShowAlertAsync("Erreur", $"Impossible d'afficher les détails de l'organisation : {ex.Message}", "OK");
                }
            }
        }

        [RelayCommand]
        public async Task ShowBusinessDetailsAsync(Models.Supabase.SupabaseBusiness business)
        {
            if (business != null)
            {
                try
                {
                    await NavigationService.NavigateToAsync<ViewModels.Businesses.BusinessDetailsViewModel>(business);
                }
                catch (Exception ex)
                {
                    await DialogService.ShowAlertAsync("Erreur", $"Impossible d'afficher les détails du commerce : {ex.Message}", "OK");
                }
            }
        }

        [RelayCommand]
        public async Task PinSelectedAsync(Pin pin)
        {
            if (pin?.BindingContext is { } context)
            {
                var contextData = (dynamic)context;
                string type = contextData.Type;

                switch (type)
                {
                    case "PracticeSpot":
                        await ShowPracticeSpotDetailsAsync((Models.Supabase.SupabasePracticeSpot)contextData.Data);
                        break;
                    case "Organization":
                        await ShowOrganizationDetailsAsync((Models.Supabase.SupabaseOrganization)contextData.Data);
                        break;
                    case "Business":
                        await ShowBusinessDetailsAsync((Models.Supabase.SupabaseBusiness)contextData.Data);
                        break;
                }
            }
        }

        [RelayCommand]
        public async Task GetCurrentLocationAsync()
        {
            try
            {
                IsLocationLoading = true;
                var locationResult = await _locationService.GetCurrentLocationAsync();
                
                if (locationResult != null)
                {
                    // Convert LocationCoordinates to Location
                    CurrentLocation = new Microsoft.Maui.Devices.Sensors.Location(
                        (double)locationResult.Latitude, 
                        (double)locationResult.Longitude);
                    
                    MapLatitude = (double)locationResult.Latitude;
                    MapLongitude = (double)locationResult.Longitude;
                    IsLocationAvailable = true;
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'obtenir votre localisation.", "OK");
            }
            finally
            {
                IsLocationLoading = false;
            }
        }

        [RelayCommand]
        public async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return;

            try
            {
                IsBusy = true;

                if (ShowPracticeSpots)
                {
                    await SearchPracticeSpotsAsync();
                }
                if (ShowOrganizations)
                {
                    await SearchOrganizationsAsync();
                }
                if (ShowBusinesses)
                {
                    await SearchBusinessesAsync();
                }

                UpdatePins();
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Erreur lors de la recherche.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SearchPracticeSpotsAsync()
        {
            try
            {
                var allSpots = await _supabaseApiService.GetPracticeSpotsAsync();
                var filteredSpots = allSpots.Where(s => 
                    !string.IsNullOrEmpty(s.Name) && 
                    s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                PracticeSpots.Clear();
                foreach (var spot in filteredSpots)
                {
                    PracticeSpots.Add(spot);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Erreur lors de la recherche des spots de pratique.", "OK");
            }
        }

        private async Task SearchOrganizationsAsync()
        {
            try
            {
                var allOrganizations = await _supabaseApiService.GetOrganizationsAsync();
                var filteredOrganizations = allOrganizations.Where(o => 
                    !string.IsNullOrEmpty(o.Name) && 
                    o.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                Organizations.Clear();
                foreach (var org in filteredOrganizations)
                {
                    Organizations.Add(org);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Erreur lors de la recherche des organisations.", "OK");
            }
        }

        private async Task SearchBusinessesAsync()
        {
            try
            {
                var allBusinesses = await _supabaseApiService.GetBusinessesAsync();
                var filteredBusinesses = allBusinesses.Where(b => 
                    !string.IsNullOrEmpty(b.Name) && 
                    b.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                Businesses.Clear();
                foreach (var business in filteredBusinesses)
                {
                    Businesses.Add(business);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Erreur lors de la recherche des commerces.", "OK");
            }
        }

        [RelayCommand]
        public async Task ClearFiltersAsync()
        {
            SearchText = string.Empty;
            ShowPracticeSpots = true;
            ShowOrganizations = true;
            ShowBusinesses = true;
            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task FilterByTypeAsync(string filterType)
        {
            // Nouvelle architecture 3-tables
            switch (filterType?.ToLower())
            {
                case "practice":
                    ShowPracticeSpots = true;
                    ShowOrganizations = false;
                    ShowBusinesses = false;
                    break;
                case "organization":
                    ShowPracticeSpots = false;
                    ShowOrganizations = true;
                    ShowBusinesses = false;
                    break;
                case "business":
                    ShowPracticeSpots = false;
                    ShowOrganizations = false;
                    ShowBusinesses = true;
                    break;
                default:
                    ShowPracticeSpots = true;
                    ShowOrganizations = true;
                    ShowBusinesses = true;
                    break;
            }
            await LoadDataAsync();
        }

        // Mini-window commands
        [RelayCommand]
        public void ShowEntityMiniWindow(object entity)
        {
            SelectedEntity = entity;
            IsSpotMiniWindowVisible = true;
        }

        [RelayCommand]
        public void CloseSpotMiniWindow()
        {
            IsSpotMiniWindowVisible = false;
            SelectedEntity = null;
        }

        [RelayCommand]
        public async Task ViewEntityDetailsAsync()
        {
            if (SelectedEntity == null) return;

            switch (SelectedEntity)
            {
                case Models.Supabase.SupabasePracticeSpot practiceSpot:
                    await ShowPracticeSpotDetailsAsync(practiceSpot);
                    break;
                case Models.Supabase.SupabaseOrganization organization:
                    await ShowOrganizationDetailsAsync(organization);
                    break;
                case Models.Supabase.SupabaseBusiness business:
                    await ShowBusinessDetailsAsync(business);
                    break;
            }

            // Close mini window after navigation
            CloseSpotMiniWindow();
        }

        [RelayCommand]
        public async Task NavigateToAddSpotAsync()
        {
            await NavigationService.NavigateToAsync<ViewModels.Spots.SimpleApiAddSpotViewModel>();
        }

        /// <summary>
        /// Convert SupabasePracticeSpot to Domain Spot model for SpotDetailsViewModel
        /// </summary>
        private Models.Domain.Spot ConvertSupabasePracticeSpotToDomainSpot(Models.Supabase.SupabasePracticeSpot practiceSpot)
        {
            return new Models.Domain.Spot
            {
                Id = Guid.NewGuid(), // Generate a temporary ID for navigation
                Name = practiceSpot.Name ?? "Spot de pratique",
                Description = practiceSpot.Description ?? "",
                Latitude = practiceSpot.Latitude,
                Longitude = practiceSpot.Longitude,
                CreatorId = Guid.Empty, // Temporary ID
                CreatedAt = DateTime.UtcNow,
                // Map other properties with correct names and types
                MaxDepth = (int?)practiceSpot.MaxDepth,
                DifficultyLevel = MapDifficultyLevel(practiceSpot.DifficultyLevel),
                TypeId = Guid.Empty, // Default to empty, could be improved
                ValidationStatus = Models.Enums.SpotValidationStatus.Approved,
                RequiredEquipment = "",
                SafetyNotes = "",
                BestConditions = ""
            };
        }

        /// <summary>
        /// Map difficulty level from string to enum
        /// </summary>
        private Models.Enums.DifficultyLevel MapDifficultyLevel(string? difficulty)
        {
            return difficulty?.ToLower() switch
            {
                "facile" => Models.Enums.DifficultyLevel.Beginner,
                "moyen" => Models.Enums.DifficultyLevel.Intermediate,
                "difficile" => Models.Enums.DifficultyLevel.Advanced,
                "expert" => Models.Enums.DifficultyLevel.Expert,
                _ => Models.Enums.DifficultyLevel.Beginner
            };
        }
    }
}