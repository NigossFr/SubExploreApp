namespace SubExplore.Views.Favorites
{
    public partial class FavoritesPage : ContentPage
    {
        public FavoritesPage()
        {
            InitializeComponent();
        }

        private async void OnExploreMapClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("///map");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FavoritesPage] Navigation error: {ex.Message}");
            }
        }
    }
}