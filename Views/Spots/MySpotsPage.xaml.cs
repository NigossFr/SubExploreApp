using SubExplore.ViewModels.Spots;

namespace SubExplore.Views.Spots;

public partial class MySpotsPage : ContentPage
{
    public MySpotsPage(MySpotsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}