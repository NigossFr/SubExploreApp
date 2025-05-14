using System;
using System.Globalization;
using Microsoft.Maui.Devices.Sensors; // Nécessaire pour Location
using SubExplore.Models.Domain; // Nécessaire pour le type Spot

namespace SubExplore.Helpers.Converters
{
    public class SpotToMapPositionConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Vérifie si la valeur fournie est bien un objet Spot
            if (value is Spot spot)
            {
                try
                {
                    // Convertit directement les propriétés decimal (non-nullable) en double
                    double lat = System.Convert.ToDouble(spot.Latitude);
                    double lon = System.Convert.ToDouble(spot.Longitude);

                    // Retourne un objet Location (de Microsoft.Maui.Devices.Sensors)
                    return new Location(lat, lon);
                }
                catch (Exception ex)
                {
                    // Log l'erreur si la conversion échoue
                    System.Diagnostics.Debug.WriteLine($"Erreur dans SpotToMapPositionConverter: {ex.Message}");
                    // Retourne null ou une valeur par défaut en cas d'erreur
                    return null;
                    // Alternative si un retour non-null est absolument requis :
                    // return new Location(0, 0);
                }
            }

            // Si la valeur n'est pas un objet Spot ou est null, retourne null/défaut
            return null;
            // Alternative : return new Location(0, 0);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // La conversion inverse n'est généralement pas nécessaire ici
            throw new NotImplementedException();
        }
    }
}