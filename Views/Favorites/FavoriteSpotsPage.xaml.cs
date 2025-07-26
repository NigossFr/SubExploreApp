using Microsoft.Maui.Controls;
using SubExplore.ViewModels.Favorites;

namespace SubExplore.Views.Favorites
{
    /// <summary>
    /// Code-behind for FavoriteSpotsPage
    /// </summary>
    public partial class FavoriteSpotsPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the FavoriteSpotsPage
        /// </summary>
        public FavoriteSpotsPage(FavoriteSpotsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        /// <summary>
        /// Handle page appearing event
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            if (BindingContext is FavoriteSpotsViewModel viewModel)
            {
                await viewModel.InitializeAsync().ConfigureAwait(false);
            }
        }
    }
}