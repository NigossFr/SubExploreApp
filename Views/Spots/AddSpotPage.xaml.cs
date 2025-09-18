using SubExplore.ViewModels.Spots;
using Microsoft.Extensions.Logging;
using System.Web;

namespace SubExplore.Views.Spots;

public partial class AddSpotPage : ContentPage
{
    private readonly RefactoredAddSpotViewModel _viewModel;
    private readonly ILogger<AddSpotPage> _logger;

    public AddSpotPage(RefactoredAddSpotViewModel viewModel, ILogger<AddSpotPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = _viewModel;

        // Force visible diagnostic that won't be dropped
        System.Diagnostics.Debug.WriteLine("🚨 FORCE DEBUG: AddSpotPage CONSTRUCTOR called, ViewModel type: " + (_viewModel?.GetType().Name ?? "NULL"));
        _logger.LogInformation("🔍 DIAGNOSTIC: AddSpotPage CONSTRUCTOR called, ViewModel type: {Type}", _viewModel?.GetType().Name ?? "NULL");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogDebug("AddSpotPage OnAppearing called");

        try
        {
            // Récupérer les paramètres de navigation depuis les query parameters - COPIÉ de OrganizationAddPage
            var parameters = new Dictionary<string, object>();

            // Gérer les paramètres de navigation depuis Shell
            if (Shell.Current.CurrentState?.Location?.OriginalString?.Contains("Latitude") == true)
            {
                var uri = new Uri(Shell.Current.CurrentState.Location.OriginalString);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

                if (double.TryParse(query["Latitude"], out var lat))
                    parameters["Latitude"] = lat;

                if (double.TryParse(query["Longitude"], out var lon))
                    parameters["Longitude"] = lon;

                if (!string.IsNullOrEmpty(query["Mode"]))
                    parameters["Mode"] = query["Mode"];
            }

            await _viewModel.InitializeAsync(parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR: AddSpotPage OnAppearing failed");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _logger.LogInformation("📤 Add Spot page disappeared");
    }
}