using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    public interface IConnectivityService
    {
        /// <summary>
        /// Indique si l'appareil est actuellement connecté à Internet
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Indique si la connexion actuelle est une connexion réseau cellulaire
        /// </summary>
        bool IsUsingCellularNetwork { get; }

        /// <summary>
        /// Se produit lorsque la connectivité réseau change
        /// </summary>
        event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;
    }

    public class ConnectivityChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public bool IsUsingCellularNetwork { get; set; }
    }
}
