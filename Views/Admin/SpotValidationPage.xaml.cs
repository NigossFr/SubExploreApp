using SubExplore.ViewModels.Admin;

namespace SubExplore.Views.Admin;

public partial class SpotValidationPage : ContentPage
{
    public SpotValidationPage(SpotValidationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is SpotValidationViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}