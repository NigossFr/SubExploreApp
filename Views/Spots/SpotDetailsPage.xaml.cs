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


        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Mettre � jour la carte si les donn�es sont d�j� charg�es
            // (peut arriver si on revient sur la page)
            if (_viewModel != null && !_viewModel.IsLoading && _viewModel.Spot != null)
            {
                UpdateMap();
            }
            // Si le ViewModel est toujours en cours de chargement, l'�v�nement PropertyChanged
            // s'en chargera via ViewModel_PropertyChanged.
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
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Impossible de cr�er le Pin pour UpdateMap.");
                // Optionnel : afficher un message ou ne rien faire
            }

            // Appelez la m�thode GetMapSpan() du ViewModel pour obtenir la r�gion
            MapSpan? mapRegion = _viewModel.GetMapSpan(); // Utilise MapSpan? pour le nullable

            // Si une r�gion valide a �t� cr��e, d�placez la carte
            if (mapRegion != null)
            {
                spotMap.MoveToRegion(mapRegion);
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
                MainThread.BeginInvokeOnMainThread(() => UpdateMap());
            }
            // Vous pourriez aussi �couter les changements sur _viewModel.Spot directement
            // if (e.PropertyName == nameof(SpotDetailsViewModel.Spot) && _viewModel?.Spot != null) ...
        }
    }
}