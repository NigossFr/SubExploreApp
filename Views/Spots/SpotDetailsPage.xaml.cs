using Microsoft.Maui.Controls.Maps; 
using Microsoft.Maui.Maps;          
using SubExplore.ViewModels.Spot;
using System.ComponentModel; 

namespace SubExplore.Views.Spot
{
    public partial class SpotDetailsPage : ContentPage
    {
        private SpotDetailsViewModel _viewModel;

        public SpotDetailsPage(SpotDetailsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _viewModel = viewModel;

            // Écouter les changements de propriété sur le ViewModel
            // Surtout IsLoading pour savoir quand les données sont prêtes
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        // N'oubliez pas de vous désabonner pour éviter les fuites mémoire
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

            // Mettre à jour la carte si les données sont déjà chargées
            // (peut arriver si on revient sur la page)
            if (_viewModel != null && !_viewModel.IsLoading && _viewModel.Spot != null)
            {
                UpdateMap();
            }
            // Si le ViewModel est toujours en cours de chargement, l'événement PropertyChanged
            // s'en chargera via ViewModel_PropertyChanged.
        }

        // Méthode pour mettre à jour la carte après le chargement des données
        private void UpdateMap()
        {
            if (_viewModel == null || spotMap == null)
            {
                System.Diagnostics.Debug.WriteLine("ViewModel ou Map non prêts pour UpdateMap.");
                return;
            }

            // Nettoyez les pins actuels
            spotMap.Pins.Clear();

            // Appelez la méthode CreatePin() du ViewModel pour obtenir le Pin
            Pin? spotPin = _viewModel.CreatePin(); // Utilise Pin? pour le nullable

            // Si un pin valide a été créé, ajoutez-le à la carte
            if (spotPin != null)
            {
                spotMap.Pins.Add(spotPin);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Impossible de créer le Pin pour UpdateMap.");
                // Optionnel : afficher un message ou ne rien faire
            }

            // Appelez la méthode GetMapSpan() du ViewModel pour obtenir la région
            MapSpan? mapRegion = _viewModel.GetMapSpan(); // Utilise MapSpan? pour le nullable

            // Si une région valide a été créée, déplacez la carte
            if (mapRegion != null)
            {
                spotMap.MoveToRegion(mapRegion);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Impossible de créer le MapSpan pour UpdateMap.");
                // Optionnel : centrer sur une position par défaut si Spot est null mais la carte doit s'afficher
                // spotMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(0, 0), Distance.FromKilometers(5)));
            }
        }

        // Gestionnaire pour réagir aux changements dans le ViewModel
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Vérifie si la propriété IsLoading a changé ET qu'elle est maintenant false
            // ET que le Spot est chargé (important !)
            if (e.PropertyName == nameof(SpotDetailsViewModel.IsLoading) &&
                _viewModel != null && !_viewModel.IsLoading && _viewModel.Spot != null)
            {
                // Les données sont chargées, mettez à jour l'interface utilisateur (la carte)
                // Assurez-vous que cela s'exécute sur le thread UI si nécessaire
                MainThread.BeginInvokeOnMainThread(() => UpdateMap());
            }
            // Vous pourriez aussi écouter les changements sur _viewModel.Spot directement
            // if (e.PropertyName == nameof(SpotDetailsViewModel.Spot) && _viewModel?.Spot != null) ...
        }
    }
}