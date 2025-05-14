using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.ViewModels.Map;

namespace SubExplore.Views.Map
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MapPage : ContentPage
    {
        private MapViewModel _viewModel => BindingContext as MapViewModel;

        public MapPage(MapViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        private void OnMapClicked(object sender, MapClickedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.MapClickedCommand.Execute(e);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Initialiser le ViewModel
            if (_viewModel != null)
            {
                await _viewModel.InitializeAsync();
            }
        }
    }
}