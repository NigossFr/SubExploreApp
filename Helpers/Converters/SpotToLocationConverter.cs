using Microsoft.Maui.Maps;
using SubExplore.Models.Domain;
using System.Globalization;

namespace SubExplore.Helpers.Converters
{
    public class SpotToLocationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Spot spot)
            {
                try
                {
                    double lat = System.Convert.ToDouble(spot.Latitude);
                    double lon = System.Convert.ToDouble(spot.Longitude);
                    
                    // Validate coordinates
                    if (double.IsNaN(lat) || double.IsInfinity(lat) || lat < -90 || lat > 90)
                        return new Location(0, 0); // Default fallback
                        
                    if (double.IsNaN(lon) || double.IsInfinity(lon) || lon < -180 || lon > 180)
                        return new Location(0, 0); // Default fallback
                    
                    return new Location(lat, lon);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] SpotToLocationConverter failed for spot {spot.Name}: {ex.Message}");
                    return new Location(0, 0); // Default fallback
                }
            }
            
            return new Location(0, 0); // Default fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}