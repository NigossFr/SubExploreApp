using SubExplore.ViewModels.Common;

namespace SubExplore.Views.Common;

public partial class NavigationBarView : ContentView
{
    public NavigationBarView()
    {
        InitializeComponent();
    }

    public NavigationBarView(NavigationBarViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}