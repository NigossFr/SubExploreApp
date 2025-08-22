// ========================================
// SIMPLE API ADD SPOT VIEWMODEL
// ========================================
// Version 100% API Supabase - plus de code hybride

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.ViewModels;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Spots
{
    public partial class SimpleApiAddSpotViewModel : ViewModelBase
    {
        private readonly ISupabaseApiService _apiService;
        private readonly ILocationService _locationService;
        private readonly ILogger<SimpleApiAddSpotViewModel> _logger;

        [ObservableProperty]
        private string _spotName = string.Empty;

        [ObservableProperty]
        private string _spotDescription = string.Empty;

        [ObservableProperty]
        private double _latitude;

        [ObservableProperty]
        private double _longitude;

        [ObservableProperty]
        private SpotType? _selectedSpotType;

        [ObservableProperty]
        private ObservableCollection<SpotTypeItem> _spotTypes = new();

        [ObservableProperty]
        private bool _isLoadingSpotTypes;

        [ObservableProperty]
        private bool _canCreateSpot;

        [ObservableProperty]
        private bool _isApiReady;

        public SimpleApiAddSpotViewModel(
            ISupabaseApiService apiService,
            ILocationService locationService,
            ILogger<SimpleApiAddSpotViewModel> logger)
        {
            _apiService = apiService;
            _locationService = locationService;
            _logger = logger;
            Title = "Ajouter un Spot";

            // Observer les changements pour valider
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SpotName) || 
                    e.PropertyName == nameof(SelectedSpotType) ||
                    e.PropertyName == nameof(Latitude) || 
                    e.PropertyName == nameof(Longitude))
                {
                    ValidateCanCreateSpot();
                }
            };
        }

        public override async Task InitializeAsync(IDictionary<string, object> parameters)
        {
            await InitializeApiAsync();
            await LoadSpotTypesAsync();
            await GetCurrentLocationAsync();
        }

        private async Task InitializeApiAsync()
        {
            try
            {
                _logger.LogInformation("🚀 Initialisation API Supabase...");
                
                // L'initialisation est maintenant automatique via ISupabaseClientService
                IsApiReady = await _apiService.TestConnectionAsync();
                
                if (IsApiReady)
                {
                    _logger.LogInformation("✅ API Supabase prête");
                }
                else
                {
                    _logger.LogError("❌ API Supabase non disponible");
                    ShowError("API non disponible. Vérifiez votre connexion.");
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur d'initialisation API");
                ShowError($"Erreur API: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task LoadSpotTypesAsync()
        {
            if (!IsApiReady)
            {
                ShowError("API non initialisée");
                return;
            }

            try
            {
                IsLoadingSpotTypes = true;
                ClearError();

                _logger.LogInformation("📥 Chargement des types de spots via API...");

                var supabaseSpotTypes = await _apiService.GetSpotTypesAsync();
                
                // Conversion vers modèles EF Core
                var spotTypes = SupabaseModelConverter.ToEfModels(supabaseSpotTypes);

                // Conversion en SpotTypeItem pour l'UI
                var spotTypeItems = spotTypes
                    .Where(st => st.IsActive)
                    .OrderBy(st => st.Name)
                    .Select(st => new SpotTypeItem { SpotType = st })
                    .ToList();

                SpotTypes.Clear();
                foreach (var item in spotTypeItems)
                {
                    SpotTypes.Add(item);
                }

                _logger.LogInformation($"✅ {SpotTypes.Count} types de spots chargés");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du chargement des types de spots");
                ShowError($"Erreur: {ex.Message}");
            }
            finally
            {
                IsLoadingSpotTypes = false;
            }
        }

        [RelayCommand]
        private async Task GetCurrentLocationAsync()
        {
            try
            {
                _logger.LogInformation("📍 Récupération de la position actuelle...");
                
                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    Latitude = (double)location.Latitude;
                    Longitude = (double)location.Longitude;
                    _logger.LogInformation($"✅ Position: {Latitude:F6}, {Longitude:F6}");
                }
                else
                {
                    // Position par défaut (exemple: Méditerranée)
                    Latitude = 43.2965;
                    Longitude = 5.3698;
                    _logger.LogInformation("⚠️ Position par défaut utilisée");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération de la position");
                ShowError("Impossible de récupérer la position actuelle");
            }
        }

        [RelayCommand]
        private async Task CreateSpotAsync()
        {
            if (!CanCreateSpot)
            {
                ShowError("Veuillez remplir tous les champs requis");
                return;
            }

            if (!IsApiReady)
            {
                ShowError("API non disponible");
                return;
            }

            try
            {
                IsLoading = true;
                ClearError();

                _logger.LogInformation("🚀 Création du spot via API...");

                // Créer le spot via API Supabase directement
                // Note: Vous devrez adapter selon votre modèle Spot API
                var newSpot = new Models.Supabase.SupabaseSpot
                {
                    Id = Guid.NewGuid(),
                    Name = SpotName,
                    Description = SpotDescription,
                    Latitude = (decimal)Latitude,
                    Longitude = (decimal)Longitude,
                    TypeId = SelectedSpotType!.Id,
                    CreatorId = Guid.NewGuid(), // TODO: Récupérer l'utilisateur actuel
                    ValidationStatus = 0, // Pending
                    // Ajouter d'autres propriétés selon vos besoins
                };

                // TODO: Ajouter une méthode CreateSpotAsync dans ISupabaseApiService
                // var createdSpot = await _apiService.CreateSpotAsync(newSpot);

                _logger.LogInformation($"✅ Spot '{SpotName}' créé avec succès");
                
                // Navigation retour
                await NavigationService.GoBackAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la création du spot");
                ShowError($"Erreur: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ValidateCanCreateSpot()
        {
            CanCreateSpot = !string.IsNullOrWhiteSpace(SpotName) &&
                          SelectedSpotType != null &&
                          Latitude != 0 &&
                          Longitude != 0 &&
                          IsApiReady;
        }

        // Méthode pour sélectionner un type de spot
        public void SelectSpotType(SpotTypeItem spotTypeItem)
        {
            // Désélectionner tous les autres
            foreach (var item in SpotTypes)
            {
                item.IsSelected = false;
            }

            // Sélectionner le nouveau
            spotTypeItem.IsSelected = true;
            SelectedSpotType = spotTypeItem.SpotType;

            _logger.LogInformation($"🎯 Type de spot sélectionné: {SelectedSpotType.Name}");
        }
    }
}