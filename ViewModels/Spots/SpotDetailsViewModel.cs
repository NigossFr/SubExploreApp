using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

// Importations très spécifiques sans ambiguïté
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;


namespace SubExplore.ViewModels.Spots
{
    [QueryProperty(nameof(SpotIdString), "id")]
    public partial class SpotDetailsViewModel : ViewModelBase
    {
        private readonly ISpotRepository _spotRepository;
        private readonly ISpotMediaRepository _spotMediaRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISpotService _spotService;

        [ObservableProperty]
        private Models.Domain.Spot _spot;

        [ObservableProperty]
        private ObservableCollection<SpotMedia> _spotMedias;

        // Pas de stockage de Position/Location dans le ViewModel
        // Ces valeurs seront calculées à la demande

        [ObservableProperty]
        private string _creatorName;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private SpotStatistics _spotStatistics;

        [ObservableProperty] 
        private SpotSafetyReport _safetyReport;

        [ObservableProperty]
        private double _averageRating;

        [ObservableProperty]
        private int _reviewCount;

        [ObservableProperty]
        private bool _isFavorite;

        [ObservableProperty]
        private IEnumerable<Models.Domain.Spot> _similarSpots;

        private int _spotId;

        public int SpotId
        {
            get => _spotId;
            set => SetProperty(ref _spotId, value);
        }

        // Query property for Shell navigation
        public string SpotIdString
        {
            get => SpotId.ToString();
            set 
            {
                if (int.TryParse(value, out int spotId))
                {
                    SpotId = spotId;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsViewModel: SpotIdString set to {value}, parsed SpotId: {SpotId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] SpotDetailsViewModel: Invalid SpotIdString value: {value}");
                }
            }
        }

        // Propriétés lisibles pour la vue mais pas liées directement à MapPosition
        public double? DisplayLatitude => Spot != null ? (double?)Spot.Latitude : null;
        public double? DisplayLongitude => Spot != null ? (double?)Spot.Longitude : null;

        public SpotDetailsViewModel(
            ISpotRepository spotRepository,
            ISpotMediaRepository spotMediaRepository,
            IUserRepository userRepository,
            ISpotService spotService,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _spotRepository = spotRepository;
            _spotMediaRepository = spotMediaRepository;
            _userRepository = userRepository;
            _spotService = spotService;

            SpotMedias = new ObservableCollection<SpotMedia>();
            SimilarSpots = new List<Models.Domain.Spot>();
            Title = "Détails du spot";
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            if (parameter is int spotId)
            {
                SpotId = spotId;
            }

            await LoadSpotAsync();
        }

        private async Task LoadSpotAsync()
        {
            if (SpotId <= 0)
                return;

            // Add null safety guards
            if (_spotService == null)
            {
                System.Diagnostics.Debug.WriteLine("[ERROR] LoadSpotAsync: _spotService is null");
                return;
            }

            IsLoading = true;

            try
            {
                // Load spot with enhanced service
                Spot = await _spotService.GetSpotWithFullDetailsAsync(SpotId);

                if (Spot == null)
                {
                    if (DialogService != null)
                    {
                        await DialogService.ShowAlertAsync("Erreur", "Le spot demandé n'existe pas.", "OK");
                    }
                    if (NavigationService != null)
                    {
                        await NavigationService.GoBackAsync();
                    }
                    return;
                }

                Title = Spot.Name;

                // Load media (now included in full details)
                SpotMedias.Clear();
                if (Spot.Media != null)
                {
                    foreach (var media in Spot.Media)
                    {
                        SpotMedias.Add(media);
                    }
                }

                // Load creator name
                if (Spot.CreatorId > 0 && _userRepository != null)
                {
                    var creator = await _userRepository.GetByIdAsync(Spot.CreatorId);
                    CreatorName = creator?.Username ?? "Utilisateur inconnu";
                }
                else
                {
                    CreatorName = "Utilisateur inconnu";
                }

                // Load enhanced spot data
                await LoadEnhancedSpotDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotAsync failed: {ex.Message}");
                if (DialogService != null)
                {
                    await DialogService.ShowAlertAsync("Erreur", $"Une erreur est survenue lors du chargement du spot : {ex.Message}", "OK");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadEnhancedSpotDataAsync()
        {
            try
            {
                // Add null safety guard for enhanced data loading
                if (_spotService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[WARNING] LoadEnhancedSpotDataAsync: _spotService is null, skipping enhanced data");
                    return;
                }

                // Load statistics
                SpotStatistics = await _spotService.GetSpotStatisticsAsync(SpotId);

                // Load safety report
                SafetyReport = await _spotService.GenerateSafetyReportAsync(SpotId);

                // Load rating data
                AverageRating = await _spotService.GetSpotAverageRatingAsync(SpotId);
                ReviewCount = await _spotService.GetSpotReviewCountAsync(SpotId);

                // Load similar spots
                SimilarSpots = await _spotService.GetSimilarSpotsAsync(SpotId);

                // Check if spot is favorite (if user is logged in)
                // TODO: Get current user ID from authentication service
                // IsFavorite = await _spotService.IsSpotFavoriteAsync(SpotId, currentUserId);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the main loading
                System.Diagnostics.Debug.WriteLine($"Error loading enhanced spot data: {ex.Message}");
            }
        }

        // Méthode utilitaire pour créer un Pin
        // À utiliser UNIQUEMENT depuis le code-behind
        public Pin CreatePin()
        {
            if (Spot == null) return null;

            try
            {
                // Conversion explicite de decimal à double
                double lat = (double)Spot.Latitude;
                double lon = (double)Spot.Longitude;

                // Utilisation explicite du bon type pour la propriété Location
                return new Pin
                {
                    Label = Spot.Name,
                    Address = $"Profondeur: {Spot.MaxDepth?.ToString() ?? "N/A"}m, Difficulté: {Spot.DifficultyLevel}",
                    Type = PinType.Place,
                    Location = new Location(lat, lon) // Type Location explicite
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la création du pin: {ex.Message}");
                return null;
            }
        }

        // Méthode utilitaire pour créer un MapSpan
        // À utiliser UNIQUEMENT depuis le code-behind
        public Microsoft.Maui.Maps.MapSpan GetMapSpan(double radiusKm = 1.0)
        {
            if (Spot == null) return null;

            try
            {
                double lat = (double)Spot.Latitude;
                double lon = (double)Spot.Longitude;

                // Utilisation très explicite de Distance et MapSpan
                var center = new Location(lat, lon);
                var distance = Microsoft.Maui.Maps.Distance.FromKilometers(radiusKm);
                return Microsoft.Maui.Maps.MapSpan.FromCenterAndRadius(center, distance);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la création du MapSpan: {ex.Message}");
                return null;
            }
        }

        // Getter pour les coordonnées sans conversion en objet Position
        public (double Latitude, double Longitude)? GetCoordinates()
        {
            if (Spot == null) return null;

            try
            {
                double lat = (double)Spot.Latitude;
                double lon = (double)Spot.Longitude;
                return (lat, lon);
            }
            catch
            {
                return null;
            }
        }

        [RelayCommand]
        private async Task ShareSpot()
        {
            try
            {
                string message = $"Découvre ce spot de {Spot.Type?.Name ?? "plongée"} : {Spot.Name}\n";
                message += $"Profondeur : {Spot.MaxDepth}m, Difficulté : {Spot.DifficultyLevel}\n";
                message += $"Localisation : {Spot.Latitude}, {Spot.Longitude}\n";
                message += "Partagé depuis l'application SubExplore";

                await Share.RequestAsync(new ShareTextRequest
                {
                    Title = $"Partager le spot {Spot.Name}",
                    Text = message
                });
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Impossible de partager le spot : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ReportSpot()
        {
            bool confirm = await DialogService.ShowConfirmationAsync(
                "Signalement",
                "Souhaitez-vous signaler ce spot pour un problème ?",
                "Oui",
                "Non");

            if (confirm)
            {
                await DialogService.ShowAlertAsync(
                    "Signalement",
                    "Merci pour votre signalement. Un modérateur va examiner ce spot.",
                    "OK");
            }
        }

        [RelayCommand]
        private async Task OpenInExternalMap()
        {
            if (Spot == null) return;
            try
            {
                double lat = (double)Spot.Latitude;
                double lon = (double)Spot.Longitude;

                // Location est de Microsoft.Maui.Devices.Sensors
                var location = new Location(lat, lon);
                var options = new MapLaunchOptions // MapLaunchOptions est dans Microsoft.Maui.ApplicationModel si MAUI < 8, sinon Microsoft.Maui.Devices.Sensors
                {
                    Name = Spot.Name,
                    NavigationMode = NavigationMode.None // NavigationMode est dans Microsoft.Maui.ApplicationModel
                };
                // Map.OpenAsync est dans Microsoft.Maui.Maps
                await Microsoft.Maui.ApplicationModel.Map.OpenAsync(location, options);

            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Impossible d'ouvrir la carte: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task Back()
        {
            await NavigationService.GoBackAsync();
        }

        [RelayCommand]
        private async Task ToggleFavorite()
        {
            try
            {
                if (Spot == null) return;

                // TODO: Get current user ID from authentication service
                // var currentUserId = await _authenticationService.GetCurrentUserIdAsync();
                // if (currentUserId.HasValue)
                // {
                //     IsFavorite = await _spotService.ToggleFavoriteSpotAsync(SpotId, currentUserId.Value);
                // }

                await DialogService.ShowAlertAsync("Info", "Fonctionnalité en cours de développement", "OK");
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Impossible de modifier les favoris: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ViewSimilarSpots()
        {
            try
            {
                if (SimilarSpots?.Any() == true)
                {
                    // TODO: Navigate to similar spots view
                    await DialogService.ShowAlertAsync("Info", $"Spots similaires trouvés: {SimilarSpots.Count()}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Erreur lors de la navigation: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task RecordVisit()
        {
            try
            {
                if (Spot == null) return;

                // TODO: Get current user ID from authentication service
                // var currentUserId = await _authenticationService.GetCurrentUserIdAsync();
                // if (currentUserId.HasValue)
                // {
                //     await _spotService.RecordSpotVisitAsync(SpotId, currentUserId.Value);
                //     await DialogService.ShowToastAsync("Visite enregistrée!");
                //     
                //     // Refresh statistics
                //     SpotStatistics = await _spotService.GetSpotStatisticsAsync(SpotId);
                // }

                await DialogService.ShowAlertAsync("Info", "Visite enregistrée (fonctionnalité en développement)", "OK");
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Impossible d'enregistrer la visite: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ViewSafetyReport()
        {
            try
            {
                if (SafetyReport == null) return;

                var message = $"Score de sécurité: {SafetyReport.SafetyScore}/100\n\n";
                
                if (SafetyReport.SafetyWarnings.Any())
                {
                    message += "⚠️ Avertissements:\n";
                    message += string.Join("\n", SafetyReport.SafetyWarnings.Select(w => $"• {w}"));
                    message += "\n\n";
                }

                if (SafetyReport.RequiredEquipment.Any())
                {
                    message += "🎯 Équipement requis:\n";
                    message += string.Join("\n", SafetyReport.RequiredEquipment.Select(e => $"• {e}"));
                }

                await DialogService.ShowAlertAsync("Rapport de sécurité", message, "OK");
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Impossible d'afficher le rapport: {ex.Message}", "OK");
            }
        }

        // Back command for header navigation is auto-generated as BackCommand
    }
}