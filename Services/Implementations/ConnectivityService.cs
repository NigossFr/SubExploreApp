using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Networking;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    public class ConnectivityService : IConnectivityService
    {
        public bool IsConnected => Connectivity.NetworkAccess == NetworkAccess.Internet;

        public bool IsUsingCellularNetwork => Connectivity.ConnectionProfiles.Contains(ConnectionProfile.Cellular);

        // Définir l'événement avec le type correct
        public event EventHandler<SubExplore.Services.Interfaces.ConnectivityChangedEventArgs> ConnectivityChanged;

        public ConnectivityService()
        {
            // S'abonner aux changements de connectivité du système
            Microsoft.Maui.Networking.Connectivity.ConnectivityChanged += HandleConnectivityChanged;
        }

        // Gestionnaire des événements du système qui convertit en nos propres événements
        private void HandleConnectivityChanged(object sender, Microsoft.Maui.Networking.ConnectivityChangedEventArgs e)
        {
            // Créer nos propres arguments d'événement
            var args = new SubExplore.Services.Interfaces.ConnectivityChangedEventArgs
            {
                IsConnected = IsConnected,
                IsUsingCellularNetwork = IsUsingCellularNetwork
            };

            // Déclencher notre propre événement
            OnConnectivityChanged(args);
        }

        // Méthode protégée qui déclenche l'événement
        protected virtual void OnConnectivityChanged(SubExplore.Services.Interfaces.ConnectivityChangedEventArgs e)
        {
            ConnectivityChanged?.Invoke(this, e);
        }

        // Ajoutons une méthode de nettoyage pour être propre
        public void Dispose()
        {
            // Se désabonner des événements système pour éviter les fuites de mémoire
            Microsoft.Maui.Networking.Connectivity.ConnectivityChanged -= HandleConnectivityChanged;
        }
    }
}