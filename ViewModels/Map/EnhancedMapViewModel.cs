// ========================================
// ENHANCED MAP VIEWMODEL - 100% API SUPABASE
// ========================================
// ViewModel de carte utilisant uniquement les services Supabase natifs

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Map
{
    /// <summary>
    /// ViewModel avancé pour la carte utilisant les services Supabase natifs
    /// </summary>
    public partial class EnhancedMapViewModel : ViewModelBase
    {
        private readonly ISupabaseSpotService _spotService;
        private readonly ISupabaseSpotTypeService _spotTypeService;
        private readonly IEnhancedAuthenticationService _authenticationService;
        private readonly ILocationService _locationService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;
        private readonly IConfiguration _configuration;
        private readonly IPlatformMapService _platformMapService;
        
        // Configuration de la carte
        private const double DEFAULT_SEARCH_RADIUS_KM = 10.0;
        private const int MAX_SPOTS_LIMIT = 100;
        private const double DEFAULT_ZOOM_LEVEL = 12.0;

        // Collections observables
        [ObservableProperty]
        private ObservableCollection<Spot> spots = new();

        [ObservableProperty]
        private ObservableCollection<SpotType> availableSpotTypes = new();

        [ObservableProperty]
        private ObservableCollection<Pin> mapPins = new();

        // Position et état de la carte
        [ObservableProperty]
        private Location userLocation = new(48.8566, 2.3522); // Paris par défaut

        [ObservableProperty]
        private bool isLocationAvailable = false;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private double mapLatitude = 48.8566;

        [ObservableProperty]
        private double mapLongitude = 2.3522;

        [ObservableProperty]
        private double mapZoomLevel = DEFAULT_ZOOM_LEVEL;

        // État des filtres
        [ObservableProperty]
        private SpotType? selectedSpotType;

        [ObservableProperty]
        private bool showOnlyApproved = true;

        // État UI
        [ObservableProperty]
        private bool isRefreshing = false;

        [ObservableProperty]
        private bool isEmptyState = false;

        [ObservableProperty]
        private string emptyStateMessage = "Aucun spot trouvé dans cette zone";

        public EnhancedMapViewModel(
            ISupabaseSpotService spotService,
            ISupabaseSpotTypeService spotTypeService,
            IEnhancedAuthenticationService authenticationService,
            ILocationService locationService,
            IDialogService dialogService,
            INavigationService navigationService,
            IConfiguration configuration,
            IPlatformMapService platformMapService)
        {
            _spotService = spotService;
            _spotTypeService = spotTypeService;
            _authenticationService = authenticationService;
            _locationService = locationService;
            _dialogService = dialogService;
            _navigationService = navigationService;
            _configuration = configuration;
            _platformMapService = platformMapService;

            Title = "Carte SubExplore";
            
            // Configuration de la position par défaut depuis appsettings
            var defaultLat = _configuration.GetValue<double>("AppSettings:DefaultLatitude", 48.8566);
            var defaultLong = _configuration.GetValue<double>("AppSettings:DefaultLongitude", 2.3522);
            MapLatitude = defaultLat;
            MapLongitude = defaultLong;
            UserLocation = new Location(defaultLat, defaultLong);
        }

        /// <summary>
        /// Initialise le ViewModel
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine("🗺️ Initialisation d'EnhancedMapViewModel...");

                // Charger les types de spots
                await LoadSpotTypesAsync();

                // Obtenir la position utilisateur
                await GetUserLocationAsync();

                // Charger les spots
                await LoadSpotsAsync();

                System.Diagnostics.Debug.WriteLine("✅ EnhancedMapViewModel initialisé avec succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lors de l'initialisation: {ex.Message}");
                await _dialogService.ShowToastAsync("Erreur lors de l'initialisation de la carte");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge les types de spots disponibles
        /// </summary>
        private async Task LoadSpotTypesAsync()
        {
            try
            {
                var spotTypes = await _spotTypeService.GetActiveSpotTypesAsync();
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AvailableSpotTypes.Clear();
                    foreach (var spotType in spotTypes)
                    {
                        var domainSpotType = _spotTypeService.ConvertToDomainModel(spotType);
                        AvailableSpotTypes.Add(domainSpotType);
                    }
                });

                System.Diagnostics.Debug.WriteLine($"✅ {spotTypes.Count} types de spots chargés");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lors du chargement des types de spots: {ex.Message}");
            }
        }

        /// <summary>
        /// Charge les spots depuis l'API Supabase
        /// </summary>
        private async Task LoadSpotsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📍 Chargement des spots...");

                // Récupérer les spots approuvés si le filtre est activé
                var supabaseSpots = ShowOnlyApproved 
                    ? await _spotService.GetApprovedSpotsAsync()
                    : await _spotService.GetSpotsByLocationAsync(Convert.ToDecimal(MapLatitude), Convert.ToDecimal(MapLongitude), DEFAULT_SEARCH_RADIUS_KM);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Spots.Clear();
                    foreach (var supabaseSpot in supabaseSpots)
                    {
                        var domainSpot = _spotService.ConvertToDomainModel(supabaseSpot);
                        Spots.Add(domainSpot);
                    }

                    // Mettre à jour l'état vide
                    IsEmptyState = !Spots.Any();
                    if (IsEmptyState)
                    {
                        EmptyStateMessage = ShowOnlyApproved 
                            ? "Aucun spot approuvé trouvé"
                            : "Aucun spot trouvé dans cette zone";
                    }
                });

                // Mettre à jour les pins de la carte
                UpdateMapPins();

                System.Diagnostics.Debug.WriteLine($"✅ {supabaseSpots.Count} spots chargés");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lors du chargement des spots: {ex.Message}");
                await _dialogService.ShowToastAsync("Erreur lors du chargement des spots");
            }
        }

        /// <summary>
        /// Obtient la position de l'utilisateur
        /// </summary>
        private async Task GetUserLocationAsync()
        {
            try
            {
                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        UserLocation = new Location(Convert.ToDouble(location.Latitude), Convert.ToDouble(location.Longitude));
                        MapLatitude = Convert.ToDouble(location.Latitude);
                        MapLongitude = Convert.ToDouble(location.Longitude);
                        IsLocationAvailable = true;
                    });

                    System.Diagnostics.Debug.WriteLine($"✅ Position utilisateur: {location.Latitude}, {location.Longitude}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Position utilisateur non disponible, utilisation de la position par défaut");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lors de l'obtention de la position: {ex.Message}");
            }
        }

        /// <summary>
        /// Met à jour les pins sur la carte
        /// </summary>
        private void UpdateMapPins()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MapPins.Clear();

                    foreach (var spot in Spots)
                    {
                        if (SelectedSpotType != null && spot.TypeId != SelectedSpotType.Id)
                            continue;

                        var pin = new Pin
                        {
                            Label = spot.Name,
                            Address = spot.Description ?? "",
                            Type = PinType.Place,
                            Location = new Location(Convert.ToDouble(spot.Latitude), Convert.ToDouble(spot.Longitude))
                        };

                        MapPins.Add(pin);
                    }
                });

                System.Diagnostics.Debug.WriteLine($"✅ {MapPins.Count} pins mis à jour sur la carte");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lors de la mise à jour des pins: {ex.Message}");
            }
        }

        /// <summary>
        /// Commande pour rafraîchir la carte
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            try
            {
                IsRefreshing = true;
                await LoadSpotsAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// Commande de recherche de spots
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadSpotsAsync();
                return;
            }

            try
            {
                IsLoading = true;
                var searchResults = await _spotService.SearchSpotsAsync(SearchText);
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Spots.Clear();
                    foreach (var supabaseSpot in searchResults)
                    {
                        var domainSpot = _spotService.ConvertToDomainModel(supabaseSpot);
                        Spots.Add(domainSpot);
                    }
                    IsEmptyState = !Spots.Any();
                });

                UpdateMapPins();
                System.Diagnostics.Debug.WriteLine($"✅ Recherche terminée: {searchResults.Count} résultats pour '{SearchText}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lors de la recherche: {ex.Message}");
                await _dialogService.ShowToastAsync("Erreur lors de la recherche");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Commande pour centrer sur la position utilisateur
        /// </summary>
        [RelayCommand]
        private async Task CenterOnUserLocationAsync()
        {
            try
            {
                await GetUserLocationAsync();
                if (IsLocationAvailable)
                {
                    await LoadSpotsAsync(); // Recharger les spots pour cette nouvelle position
                    await _dialogService.ShowToastAsync("Carte centrée sur votre position");
                }
                else
                {
                    await _dialogService.ShowToastAsync("Position non disponible");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lors du centrage: {ex.Message}");
            }
        }

        /// <summary>
        /// Commande pour afficher les détails d'un spot
        /// </summary>
        [RelayCommand]
        private async Task ViewSpotDetailsAsync(Spot spot)
        {
            try
            {
                if (spot == null) return;
                
                // TODO: Naviguer vers la page de détails quand elle sera recréée
                await _dialogService.ShowToastAsync($"Détails du spot: {spot.Name}");
                System.Diagnostics.Debug.WriteLine($"📍 Affichage des détails pour: {spot.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lors de l'affichage des détails: {ex.Message}");
            }
        }

        /// <summary>
        /// Commande pour ajouter un nouveau spot
        /// </summary>
        [RelayCommand]
        private async Task AddSpotAsync()
        {
            try
            {
                if (!_authenticationService.IsAuthenticated)
                {
                    await _dialogService.ShowToastAsync("Vous devez être connecté pour ajouter un spot");
                    return;
                }

                // TODO: Naviguer vers la page d'ajout de spot quand elle sera recréée
                await _dialogService.ShowToastAsync("Ajout de spot temporairement indisponible");
                System.Diagnostics.Debug.WriteLine("📍 Demande d'ajout de spot");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lors de l'ajout de spot: {ex.Message}");
            }
        }

        /// <summary>
        /// Gère le changement de filtre de type de spot
        /// </summary>
        partial void OnSelectedSpotTypeChanged(SpotType? value)
        {
            UpdateMapPins();
        }

        /// <summary>
        /// Gère le changement de filtre spots approuvés uniquement
        /// </summary>
        partial void OnShowOnlyApprovedChanged(bool value)
        {
            _ = LoadSpotsAsync(); // Recharger les spots avec le nouveau filtre
        }
    }
}