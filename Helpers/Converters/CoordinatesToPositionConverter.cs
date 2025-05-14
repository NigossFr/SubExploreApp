using System;
using System.Globalization;
using Microsoft.Maui.Devices.Sensors; // Pour Location
using SubExplore.ViewModels.Spot; 

namespace SubExplore.Helpers.Converters
{
    public class CoordinatesToPositionConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Vérifie si la valeur est du bon type de ViewModel
            if (value is SpotLocationViewModel vm)
            {
                try
                {
                    // Convertit directement les propriétés decimal (non-nullable) en double
                    double lat = System.Convert.ToDouble(vm.Latitude);
                    double lon = System.Convert.ToDouble(vm.Longitude);

                    // Retourne l'objet Location
                    return new Location(lat, lon);
                }
                catch (Exception ex)
                {
                    // Log l'erreur en cas de problème pendant la conversion
                    System.Diagnostics.Debug.WriteLine($"Erreur dans CoordinatesToPositionConverter lors de la conversion: {ex.Message}");
                    // Retourne null ou une valeur par défaut appropriée si la conversion échoue
                    return null;
                    // Alternative : retourner une position par défaut si la liaison l'exige absolument
                    // return new Location(0, 0);
                }
            }

            // Si la valeur n'est pas un SpotLocationViewModel ou est null, retourne null/défaut
            return null;
            // Alternative : return new Location(0, 0);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Généralement non implémenté pour les conversions unidirectionnelles
            throw new NotImplementedException();
        }
    }
}