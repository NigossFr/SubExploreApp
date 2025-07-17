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
            try
            {
                await _viewModel.InitializeAsync(new Dictionary<string, object>());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] DatabaseTestPage OnAppearing failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
                }
            }
        }
    }
}