using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    public class LocationService : ILocationService
    {
        public async Task<LocationCoordinates> GetCurrentLocationAsync(double accuracyInMeters = 100)
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                        return null;
                }

                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var location = await Geolocation.GetLocationAsync(request);

                if (location == null)
                    return null;

                return new LocationCoordinates
                {
                    // Conversion explicite de double vers decimal
                    Latitude = (decimal)location.Latitude,
                    Longitude = (decimal)location.Longitude,
                    Accuracy = location.Accuracy ?? 0
                };
            }
            catch (Exception ex)
            {
                // Log l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur de géolocalisation: {ex.Message}");
                return null;
            }
        }

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Conversion degrés -> radians
            double toRadians(double degrees) => degrees * Math.PI / 180.0;

            // Rayon de la Terre en mètres
            const double earthRadius = 6371000;

            // Formule de Haversine
            var dLat = toRadians(lat2 - lat1);
            var dLon = toRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(toRadians(lat1)) * Math.Cos(toRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadius * c; // Distance en mètres
        }

        public async Task<bool> IsLocationServiceEnabledAsync()
        {
            try
            {
                // Only check if location services are enabled on device (without requesting permission)
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                
                // Return true if location services are available (either granted or not determined yet)
                // This indicates that the user could potentially use location services
                return status != PermissionStatus.Disabled;
            }
            catch (FeatureNotSupportedException)
            {
                // Géolocalisation non prise en charge sur l'appareil
                System.Diagnostics.Debug.WriteLine("[LocationService] Geolocation not supported on device");
                return false;
            }
            catch (FeatureNotEnabledException)
            {
                // Localisation désactivée sur l'appareil
                System.Diagnostics.Debug.WriteLine("[LocationService] Location services disabled on device");
                return false;
            }
            catch (Exception ex)
            {
                // Autres erreurs
                System.Diagnostics.Debug.WriteLine($"[LocationService] Error checking location service: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RequestLocationPermissionAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status == PermissionStatus.Granted)
                    return true;

                if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    // Sur iOS, une fois refusée, l'app ne peut plus demander directement
                    return false;
                }

                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                return status == PermissionStatus.Granted;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
