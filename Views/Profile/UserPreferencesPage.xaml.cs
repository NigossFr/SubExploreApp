using SubExplore.ViewModels.Profile;

namespace SubExplore.Views.Profile;

public partial class UserPreferencesPage : ContentPage
{
    public UserPreferencesPage(UserPreferencesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is UserPreferencesViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}