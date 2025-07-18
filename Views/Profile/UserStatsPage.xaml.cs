using SubExplore.ViewModels.Profile;

namespace SubExplore.Views.Profile;

public partial class UserStatsPage : ContentPage
{
    public UserStatsPage(UserStatsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is UserStatsViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}