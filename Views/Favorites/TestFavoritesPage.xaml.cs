using Microsoft.Maui.Controls;

namespace SubExplore.Views.Favorites
{
    /// <summary>
    /// Simple test page for favorites navigation
    /// </summary>
    public partial class TestFavoritesPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the TestFavoritesPage
        /// </summary>
        public TestFavoritesPage()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("[DEBUG] TestFavoritesPage: Constructor called");
        }

        /// <summary>
        /// Handle back button click
        /// </summary>
        private async void OnBackClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] TestFavoritesPage Back: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle page appearing event
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            System.Diagnostics.Debug.WriteLine("[DEBUG] TestFavoritesPage: OnAppearing called");
        }
    }
}