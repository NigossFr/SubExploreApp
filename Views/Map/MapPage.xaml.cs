using SubExplore.ViewModels.Map;

namespace SubExplore.Views.Map
{
    public partial class MapPage : ContentPage
    {
        public MapPage(MapViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}