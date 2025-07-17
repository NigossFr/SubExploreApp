using System;
using System.Globalization;
using Microsoft.Maui.Devices.Sensors; // Pour Location
using SubExplore.ViewModels.Spots; 

namespace SubExplore.Helpers.Converters
{
    public class CoordinatesToPositionConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"CoordinatesToPositionConverter: Called with value: {value?.GetType().Name ?? "null"}");
                
                // Handle null value immediately
                if (value == null)
                {
                    System.Diagnostics.Debug.WriteLine("CoordinatesToPositionConverter: Value is null, returning default location");
                    return GetDefaultLocation();
                }
                
                decimal latitude = 0m;
                decimal longitude = 0m;
                
                // Handle different ViewModel types
                switch (value)
                {
                    case SpotLocationViewModel spotLocationVm:
                        latitude = spotLocationVm.Latitude;
                        longitude = spotLocationVm.Longitude;
                        System.Diagnostics.Debug.WriteLine($"CoordinatesToPositionConverter: Using SpotLocationViewModel coordinates: {latitude}, {longitude}");
                        break;
                    case AddSpotViewModel addSpotVm:
                        latitude = addSpotVm.Latitude;
                        longitude = addSpotVm.Longitude;
                        System.Diagnostics.Debug.WriteLine($"CoordinatesToPositionConverter: Using AddSpotViewModel coordinates: {latitude}, {longitude}");
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"CoordinatesToPositionConverter: Unsupported value type: {value?.GetType().Name}");
                        return GetDefaultLocation();
                }
                
                // Validate coordinates
                if (latitude == 0m && longitude == 0m)
                {
                    System.Diagnostics.Debug.WriteLine("CoordinatesToPositionConverter: Coordinates are 0,0 - using default location");
                    return GetDefaultLocation();
                }
                
                // Convert to double and create Location
                double lat = System.Convert.ToDouble(latitude);
                double lon = System.Convert.ToDouble(longitude);
                
                // Validate coordinate ranges
                if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                {
                    System.Diagnostics.Debug.WriteLine($"CoordinatesToPositionConverter: Invalid coordinates: {lat}, {lon} - using default location");
                    return GetDefaultLocation();
                }

                var location = new Location(lat, lon);
                System.Diagnostics.Debug.WriteLine($"CoordinatesToPositionConverter: Created Location object: {location.Latitude}, {location.Longitude}");
                return location;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CoordinatesToPositionConverter conversion error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"CoordinatesToPositionConverter: Stack trace: {ex.StackTrace}");
                return GetDefaultLocation();
            }
        }

        private Location GetDefaultLocation()
        {
            // Default location: Marseille, France
            return new Location(43.2965, 5.3698);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Généralement non implémenté pour les conversions unidirectionnelles
            throw new NotImplementedException();
        }
    }
}