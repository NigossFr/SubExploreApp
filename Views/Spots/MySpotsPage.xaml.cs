using SubExplore.ViewModels.Spots;
using Microsoft.Extensions.Logging;

namespace SubExplore.Views.Spots;

public partial class MySpotsPage : ContentPage
{
    private readonly MySpotsViewModel _viewModel;
    private readonly ILogger<MySpotsPage> _logger;

    public MySpotsPage(MySpotsViewModel viewModel, ILogger<MySpotsPage> logger)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogInformation("🔍 MySpotsPage constructor called");
        
        InitializeComponent();
        BindingContext = _viewModel;
        
        _logger.LogInformation("🔍 MySpotsPage constructor completed, BindingContext set");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        _logger.LogInformation("🔍 MySpotsPage.OnAppearing called");
        
        try
        {
            _logger.LogInformation("🔍 Calling _viewModel.InitializeAsync()");
            await _viewModel.InitializeAsync();
            _logger.LogInformation("🔍 _viewModel.InitializeAsync() completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔍 Error in MySpotsPage.OnAppearing while initializing ViewModel");
        }
    }

    protected override void OnDisappearing()
    {
        _logger.LogInformation("🔍 MySpotsPage.OnDisappearing called");
        base.OnDisappearing();
    }
}