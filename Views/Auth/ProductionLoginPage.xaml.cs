using SubExplore.ViewModels.Auth;

namespace SubExplore.Views.Auth;

public partial class ProductionLoginPage : ContentPage
{
    public ProductionLoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is LoginViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}