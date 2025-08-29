using SubExplore.ViewModels.Spots;

namespace SubExplore.Views.Spots;

public partial class SpotEditPage : ContentPage, IQueryAttributable
{
    private readonly SpotEditViewModel _viewModel;
    private string _spotIdFromQuery = null;

    public SpotEditPage(SpotEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (!string.IsNullOrEmpty(_spotIdFromQuery))
        {
            await _viewModel.InitializeAsync(_spotIdFromQuery);
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query != null && query.ContainsKey("spotId"))
        {
            _spotIdFromQuery = query["spotId"]?.ToString();
        }
    }
}