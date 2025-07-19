using SubExplore.ViewModels.Auth;

namespace SubExplore.Views.Auth;

public partial class TestLoginPage : ContentPage
{
    public TestLoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}