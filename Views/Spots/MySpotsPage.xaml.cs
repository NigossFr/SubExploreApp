namespace SubExplore.Views.Spots
{
    public partial class MySpotsPage : ContentPage
    {
        public MySpotsPage()
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
                System.Diagnostics.Debug.WriteLine($"[MySpotsPage] Navigation error: {ex.Message}");
            }
        }
    }
}