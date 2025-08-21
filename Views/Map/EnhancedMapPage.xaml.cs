using SubExplore.ViewModels.Map;

namespace SubExplore.Views.Map
{
    /// <summary>
    /// Page de carte avancée utilisant EnhancedMapViewModel
    /// </summary>
    public partial class EnhancedMapPage : ContentPage
    {
        private readonly EnhancedMapViewModel _viewModel;

        public EnhancedMapPage(EnhancedMapViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                await _viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] EnhancedMapPage OnAppearing: {ex.Message}");
            }
        }
    }
}