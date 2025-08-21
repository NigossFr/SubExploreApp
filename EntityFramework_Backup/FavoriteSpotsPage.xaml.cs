using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using SubExplore.ViewModels.Favorites;

namespace SubExplore.Views.Favorites
{
    /// <summary>
    /// Code-behind for FavoriteSpotsPage
    /// </summary>
    public partial class FavoriteSpotsPage : ContentPage
    {
        private readonly FavoriteSpotsViewModel? _viewModel;

        /// <summary>
        /// Parameterless constructor for direct usage in AppShell
        /// </summary>
        public FavoriteSpotsPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the FavoriteSpotsPage with ViewModel
        /// </summary>
        public FavoriteSpotsPage(FavoriteSpotsViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        /// <summary>
        /// Handle page appearing event
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                // Use existing ViewModel if available, otherwise get from DI
                FavoriteSpotsViewModel? viewModel = _viewModel;
                
                if (viewModel == null && BindingContext is FavoriteSpotsViewModel existingVm)
                {
                    viewModel = existingVm;
                }
                
                if (viewModel == null)
                {
                    // Get ViewModel from DI container
                    var serviceProvider = Handler?.MauiContext?.Services;
                    if (serviceProvider != null)
                    {
                        viewModel = serviceProvider.GetService<FavoriteSpotsViewModel>();
                        if (viewModel != null)
                        {
                            BindingContext = viewModel;
                        }
                    }
                }
                
                if (viewModel != null)
                {
                    await viewModel.InitializeAsync().ConfigureAwait(false);
                }
                else
                {
                    // Debug: Log if ViewModel is null
                    System.Diagnostics.Debug.WriteLine("[ERROR] FavoriteSpotsPage: ViewModel is null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] FavoriteSpotsPage OnAppearing: {ex.Message}");
            }
        }
    }
}