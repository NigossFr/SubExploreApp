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
    public partial class SpotDetailsViewModel : ViewModelBase
    {
        private readonly ISpotRepository _spotRepository;
        private readonly ISpotMediaRepository _spotMediaRepository;
        private readonly IUserRepository _userRepository;

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

        private int _spotId;

        public int SpotId
        {
            get => _spotId;
            set => SetProperty(ref _spotId, value);
        }

        // Propriétés lisibles pour la vue mais pas liées directement à MapPosition
        public double? DisplayLatitude => Spot != null ? (double?)Spot.Latitude : null;
        public double? DisplayLongitude => Spot != null ? (double?)Spot.Longitude : null;

        public SpotDetailsViewModel(
            ISpotRepository spotRepository,
            ISpotMediaRepository spotMediaRepository,
            IUserRepository userRepository,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _spotRepository = spotRepository;
            _spotMediaRepository = spotMediaRepository;
            _userRepository = userRepository;

            SpotMedias = new ObservableCollection<SpotMedia>();
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

            IsLoading = true;

            try
            {
                // Charger le spot
                Spot = await _spotRepository.GetByIdAsync(SpotId);

                if (Spot == null)
                {
                    await DialogService.ShowAlertAsync("Erreur", "Le spot demandé n'existe pas.", "OK");
                    await NavigationService.GoBackAsync();
                    return;
                }

                Title = Spot.Name;

                // Charger les médias du spot
                var medias = await _spotMediaRepository.GetBySpotIdAsync(SpotId);
                SpotMedias.Clear();
                foreach (var media in medias)
                {
                    SpotMedias.Add(media);
                }

                // Charger le nom du créateur
                if (Spot.CreatorId > 0)
                {
                    var creator = await _userRepository.GetByIdAsync(Spot.CreatorId);
                    CreatorName = creator?.Username ?? "Utilisateur inconnu";
                }
                else
                {
                    CreatorName = "Utilisateur inconnu";
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", $"Une erreur est survenue lors du chargement du spot : {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
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
    }
}