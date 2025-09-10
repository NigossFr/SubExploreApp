using SubExplore.ViewModels.Spots;
using Microsoft.Extensions.Logging;

namespace SubExplore.Views.Spots;

public partial class AddSpotPage : ContentPage
{
    private readonly SimpleApiAddSpotViewModel _viewModel;
    private readonly ILogger<AddSpotPage> _logger;

    public AddSpotPage(SimpleApiAddSpotViewModel viewModel, ILogger<AddSpotPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            _logger.LogInformation("🔄 Initializing Add Spot page...");
            await _viewModel.InitializeAsync(new Dictionary<string, object>());
            _logger.LogInformation("✅ Add Spot page initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error initializing Add Spot page");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _logger.LogInformation("📤 Add Spot page disappeared");
    }
}