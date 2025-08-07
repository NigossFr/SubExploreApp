using SubExplore.ViewModels.Auth;

namespace SubExplore.Views.Auth;

public partial class UltraSimpleLoginPage : ContentPage
{
    public UltraSimpleLoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}