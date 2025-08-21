using Microsoft.Maui.Controls.Maps; 
using Microsoft.Maui.Maps;          
using SubExplore.ViewModels.Spots;
using System.ComponentModel; 

namespace SubExplore.Views.Spots
{
    public partial class SpotDetailsPage : ContentPage
    {
        private SpotDetailsViewModel _viewModel;

        public SpotDetailsPage(SpotDetailsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _viewModel = viewModel;
            
            // �couter les changements de propri�t� sur le ViewModel
            // Surtout IsLoading pour savoir quand les donn�es sont pr�tes
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        // N'oubliez pas de vous d�sabonner pour �viter les fuites m�moire
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsPage.OnAppearing: Starting, ViewModel: {_viewModel != null}");
                
                if (_viewModel != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsPage.OnAppearing: Current SpotId: {_viewModel.SpotId}");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsPage.OnAppearing: ViewModel Type: {_viewModel.GetType().Name}");
                }
                
                // The ViewModel will handle the query parameter via its QueryProperty
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsPage.OnAppearing: Calling InitializeAsync(null)");
                await _viewModel.InitializeAsync(null);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotDetailsPage.OnAppearing: InitializeAsync completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SpotDetailsPage OnAppearing failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        // M�thode pour mettre � jour la carte apr�s le chargement des donn�es
        private void UpdateMap()
        {
            if (_viewModel == null || spotMap == null)
            {
                System.Diagnostics.Debug.WriteLine("ViewModel ou Map non pr�ts pour UpdateMap.");
                return;
            }

            // Nettoyez les pins actuels
            spotMap.Pins.Clear();

            // Appelez la m�thode CreatePin() du ViewModel pour obtenir le Pin
            Pin? spotPin = _viewModel.CreatePin(); // Utilise Pin? pour le nullable

            // Si un pin valide a �t� cr��, ajoutez-le � la carte
            if (spotPin != null)
            {
                spotMap.Pins.Add(spotPin);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Pin ajouté: {spotPin.Label} à {spotPin.Location.Latitude}, {spotPin.Location.Longitude}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Impossible de cr�er le Pin pour UpdateMap.");
                // Optionnel : afficher un message ou ne rien faire
            }

            // Appelez la m�thode GetMapSpan() du ViewModel pour obtenir la r�gion
            MapSpan? mapRegion = _viewModel.GetMapSpan(2.0);

            // Si une r�gion valide a �t� cr��e, d�placez la carte
            if (mapRegion != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Centrage carte vers {mapRegion.Center.Latitude}, {mapRegion.Center.Longitude} avec rayon {mapRegion.Radius.Kilometers}km");
                spotMap.MoveToRegion(mapRegion);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] MoveToRegion appelé avec succès");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Impossible de cr�er le MapSpan pour UpdateMap.");
                // Optionnel : centrer sur une position par d�faut si Spot est null mais la carte doit s'afficher
                // spotMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(0, 0), Distance.FromKilometers(5)));
            }
        }

        // Gestionnaire pour r�agir aux changements dans le ViewModel
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // V�rifie si la propri�t� IsLoading a chang� ET qu'elle est maintenant false
            // ET que le Spot est charg� (important !)
            if (e.PropertyName == nameof(SpotDetailsViewModel.IsLoading) &&
                _viewModel != null && !_viewModel.IsLoading && _viewModel.Spot != null)
            {
                // Les donn�es sont charg�es, mettez � jour l'interface utilisateur (la carte)
                // Assurez-vous que cela s'ex�cute sur le thread UI si n�cessaire
                MainThread.BeginInvokeOnMainThread(async () => 
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] PropertyChanged: Exécution UpdateMap sur thread UI");
                    
                    // Petit délai pour s'assurer que la carte est complètement chargée
                    await Task.Delay(500);
                    UpdateMap();
                    
                    // Si le premier essai ne fonctionne pas, réessayons après un délai plus long
                    await Task.Delay(1000);
                    UpdateMap();
                });
            }
            // Vous pourriez aussi �couter les changements sur _viewModel.Spot directement
            // if (e.PropertyName == nameof(SpotDetailsViewModel.Spot) && _viewModel?.Spot != null) ...
        }
    }
}