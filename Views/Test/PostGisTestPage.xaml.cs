using SubExplore.ViewModels.Test;

namespace SubExplore.Views.Test;

public partial class PostGisTestPage : ContentPage
{
    public PostGisTestPage(PostGisTestViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}