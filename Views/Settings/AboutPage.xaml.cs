using SubExplore.ViewModels.Settings;

namespace SubExplore.Views.Settings;

public partial class AboutPage : ContentPage
{
    public AboutPage(AboutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}