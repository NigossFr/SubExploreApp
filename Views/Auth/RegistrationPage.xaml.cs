using SubExplore.ViewModels.Auth;

namespace SubExplore.Views.Auth;

public partial class RegistrationPage : ContentPage
{
    public RegistrationPage(RegistrationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is RegistrationViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}