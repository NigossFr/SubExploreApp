using SubExplore.ViewModels.Favorites;

namespace SubExplore.Views.Favorites
{
    public partial class FavoritesPage : ContentPage
    {
        public FavoriteSpotsViewModel ViewModel { get; }

        public FavoritesPage(FavoriteSpotsViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.InitializeAsync();
        }

        private async void OnAdvancedMenuClicked(object sender, EventArgs e)
        {
            try
            {
                var action = await DisplayActionSheet(
                    "Options avancées",
                    "Annuler",
                    null,
                    "📊 Statut de synchronisation",
                    "🔄 Synchroniser maintenant",
                    "📱 Mode hors ligne",
                    "📤 Exporter mes favoris",
                    "📥 Importer des favoris",
                    "🎯 Filtrer par activité");

                switch (action)
                {
                    case "📊 Statut de synchronisation":
                        await ViewModel.ShowSyncStatusCommand.ExecuteAsync(null);
                        break;
                    case "🔄 Synchroniser maintenant":
                        await ViewModel.SyncPendingOperationsCommand.ExecuteAsync(null);
                        break;
                    case "📱 Mode hors ligne":
                        await ViewModel.ToggleOfflineModeCommand.ExecuteAsync(null);
                        break;
                    case "📤 Exporter mes favoris":
                        await ViewModel.ExportFavoritesCommand.ExecuteAsync(null);
                        break;
                    case "📥 Importer des favoris":
                        await ViewModel.ImportFavoritesCommand.ExecuteAsync(null);
                        break;
                    case "🎯 Filtrer par activité":
                        await ViewModel.ShowActivityFilterCommand.ExecuteAsync(null);
                        break;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Une erreur est survenue: {ex.Message}", "OK");
            }
        }
    }
}