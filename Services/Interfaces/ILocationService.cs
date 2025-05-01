using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    public interface ILocationService
    {
        /// <summary>
        /// Obtient la position actuelle de l'utilisateur.
        /// </summary>
        /// <param name="accuracyInMeters">Précision souhaitée en mètres</param>
        /// <returns>Un objet contenant les coordonnées ou null si indisponible</returns>
        Task<LocationCoordinates> GetCurrentLocationAsync(double accuracyInMeters = 100);

        /// <summary>
        /// Calcule la distance entre deux coordonnées en mètres.
        /// </summary>
        /// <returns>Distance en mètres</returns>
        double CalculateDistance(double lat1, double lon1, double lat2, double lon2);

        /// <summary>
        /// Vérifie si le service de localisation est activé sur l'appareil.
        /// </summary>
        Task<bool> IsLocationServiceEnabledAsync();

        /// <summary>
        /// Demande l'autorisation d'accéder à la localisation de l'utilisateur.
        /// </summary>
        /// <returns>True si l'autorisation est accordée, sinon False</returns>
        Task<bool> RequestLocationPermissionAsync();
    }
}
