using SubExplore.ViewModels.Settings;

namespace SubExplore.Views.Settings
{
    public partial class DatabaseTestPage : ContentPage
    {
        private readonly DatabaseTestViewModel _viewModel;

        public DatabaseTestPage(DatabaseTestViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync(new Dictionary<string, object>());
        }
    }
}