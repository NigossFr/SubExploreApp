using SubExplore.ViewModels.Navigation;

namespace SubExplore.Views.Navigation;

public partial class NavigationTestPage : ContentPage
{
    public NavigationTestPage(NavigationTestViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is NavigationTestViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}