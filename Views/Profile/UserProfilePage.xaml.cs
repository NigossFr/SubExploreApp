using SubExplore.ViewModels.Profile;

namespace SubExplore.Views.Profile;

public partial class UserProfilePage : ContentPage
{
    public UserProfilePage(UserProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is UserProfileViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}