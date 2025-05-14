using SubExplore.ViewModels.Spot;

namespace SubExplore.Views.Spot
{
    public partial class AddSpotPage : ContentPage
    {
        private readonly AddSpotViewModel _viewModel;

        public AddSpotPage(AddSpotViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}