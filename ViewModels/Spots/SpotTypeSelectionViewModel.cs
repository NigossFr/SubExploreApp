using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Spots
{
    /// <summary>
    /// ViewModel for spot type selection page
    /// Allows users to choose between Practice Spot, Organization, or Business creation
    /// </summary>
    public partial class SpotTypeSelectionViewModel : ViewModelBase
    {
        private readonly ILogger<SpotTypeSelectionViewModel> _logger;

        #region Properties

        [ObservableProperty]
        private bool _hasLocationData;

        [ObservableProperty]
        private string _locationDescription = string.Empty;

        [ObservableProperty]
        private double _latitude;

        [ObservableProperty]
        private double _longitude;

        [ObservableProperty]
        private string _mode = "create";

        #endregion

        #region Constructor

        public SpotTypeSelectionViewModel(
            IDialogService? dialogService = null,
            INavigationService? navigationService = null,
            ILogger<SpotTypeSelectionViewModel>? logger = null) : base(dialogService, navigationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Title = "Ajouter un Spot";
        }

        #endregion

        #region Initialization

        public override async Task InitializeAsync(IDictionary<string, object> parameters)
        {
            try
            {
                _logger.LogInformation("🎯 Initializing SpotTypeSelectionViewModel...");

                // Check for location parameters
                if (parameters.ContainsKey("Latitude") && parameters.ContainsKey("Longitude"))
                {
                    if (parameters["Latitude"] is double lat && parameters["Longitude"] is double lon)
                    {
                        Latitude = lat;
                        Longitude = lon;
                        HasLocationData = true;
                        LocationDescription = $"Lat: {lat:F4}, Lon: {lon:F4}";
                        _logger.LogInformation($"🎯 Location provided: {LocationDescription}");
                    }
                }

                if (parameters.ContainsKey("Mode") && parameters["Mode"] is string mode)
                {
                    Mode = mode;
                    _logger.LogInformation($"🎯 Mode: {mode}");
                }

                _logger.LogInformation("✅ SpotTypeSelectionViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error initializing SpotTypeSelectionViewModel");
                ShowError("Erreur lors de l'initialisation de la page");
            }
        }

        #endregion

        #region Commands

        [RelayCommand]
        private async Task CreatePracticeSpotAsync()
        {
            try
            {
                _logger.LogInformation("🎯 Creating practice spot...");

                if (NavigationService == null)
                {
                    ShowError("Service de navigation non disponible");
                    return;
                }

                // Navigate to practice spot creation with location if available
                if (HasLocationData)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["Latitude"] = Latitude,
                        ["Longitude"] = Longitude,
                        ["Mode"] = Mode
                    };

                    await NavigationService.NavigateToAsync<RefactoredAddSpotViewModel>(parameters);
                }
                else
                {
                    await NavigationService.NavigateToAsync<RefactoredAddSpotViewModel>();
                }

                _logger.LogInformation("✅ Navigated to practice spot creation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating practice spot");
                ShowError("Erreur lors de la navigation vers la création de spot");
            }
        }

        [RelayCommand]
        private async Task CreateOrganizationAsync()
        {
            try
            {
                _logger.LogInformation("🎯 Creating organization...");

                if (NavigationService == null)
                {
                    ShowError("Service de navigation non disponible");
                    return;
                }

                // Navigate to organization creation with location if available
                if (HasLocationData)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["Latitude"] = Latitude,
                        ["Longitude"] = Longitude,
                        ["Mode"] = Mode
                    };

                    await NavigationService.NavigateToAsync<ViewModels.Organizations.OrganizationAddViewModel>(parameters);
                }
                else
                {
                    await NavigationService.NavigateToAsync<ViewModels.Organizations.OrganizationAddViewModel>();
                }

                _logger.LogInformation("✅ Navigated to organization creation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating organization");
                ShowError("Erreur lors de la navigation vers la création d'organisation");
            }
        }

        [RelayCommand]
        private async Task CreateBusinessAsync()
        {
            try
            {
                _logger.LogInformation("🎯 Creating business...");

                if (NavigationService == null)
                {
                    ShowError("Service de navigation non disponible");
                    return;
                }

                // Navigate to business creation with location if available
                if (HasLocationData)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["Latitude"] = Latitude,
                        ["Longitude"] = Longitude,
                        ["Mode"] = Mode
                    };

                    await NavigationService.NavigateToAsync<ViewModels.Businesses.BusinessAddViewModel>(parameters);
                }
                else
                {
                    await NavigationService.NavigateToAsync<ViewModels.Businesses.BusinessAddViewModel>();
                }

                _logger.LogInformation("✅ Navigated to business creation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating business");
                ShowError("Erreur lors de la navigation vers la création de commerce");
            }
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            try
            {
                _logger.LogInformation("🎯 Cancelling spot creation...");

                if (NavigationService != null)
                {
                    await NavigationService.GoBackAsync();
                }

                _logger.LogInformation("✅ Cancelled spot creation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cancelling spot creation");
            }
        }

        #endregion

        #region Helper Methods

        private void ShowError(string message)
        {
            if (DialogService != null)
            {
                _ = DialogService.ShowAlertAsync("Erreur", message, "OK");
            }
        }

        #endregion
    }
}