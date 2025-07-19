using SubExplore.ViewModels.Auth;

namespace SubExplore.Views.Auth;

public partial class DiagnosticLoginPage : ContentPage
{
    public DiagnosticLoginPage(LoginViewModel viewModel)
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