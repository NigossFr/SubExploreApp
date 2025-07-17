using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace SubExplore.Helpers.Converters
{
    public class ResponsiveHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string parameterString)
                return value;

            var screenHeight = DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density;
            
            // Parse parameter in format "percentage:min:max" (e.g., "0.3:150:300")
            var parts = parameterString.Split(':');
            if (parts.Length >= 1 && double.TryParse(parts[0], out double percentage))
            {
                var calculatedHeight = screenHeight * percentage;
                
                // Apply min/max constraints if provided
                if (parts.Length >= 2 && double.TryParse(parts[1], out double minHeight))
                {
                    calculatedHeight = Math.Max(calculatedHeight, minHeight);
                }
                
                if (parts.Length >= 3 && double.TryParse(parts[2], out double maxHeight))
                {
                    calculatedHeight = Math.Min(calculatedHeight, maxHeight);
                }
                
                return calculatedHeight;
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ResponsiveWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string parameterString)
                return value;

            var screenWidth = DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density;
            
            // Parse parameter in format "percentage:min:max" (e.g., "0.8:200:400")
            var parts = parameterString.Split(':');
            if (parts.Length >= 1 && double.TryParse(parts[0], out double percentage))
            {
                var calculatedWidth = screenWidth * percentage;
                
                // Apply min/max constraints if provided
                if (parts.Length >= 2 && double.TryParse(parts[1], out double minWidth))
                {
                    calculatedWidth = Math.Max(calculatedWidth, minWidth);
                }
                
                if (parts.Length >= 3 && double.TryParse(parts[2], out double maxWidth))
                {
                    calculatedWidth = Math.Min(calculatedWidth, maxWidth);
                }
                
                return calculatedWidth;
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ResponsiveFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string parameterString)
                return value;

            var screenWidth = DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density;
            
            // Base font size calculation on screen width
            // Small screens: <400, Medium: 400-600, Large: >600
            double baseFontSize = screenWidth switch
            {
                < 400 => 12,
                < 600 => 14,
                _ => 16
            };
            
            // Apply multiplier if provided
            if (double.TryParse(parameterString, out double multiplier))
            {
                return baseFontSize * multiplier;
            }
            
            return baseFontSize;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ResponsiveMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string parameterString)
                return new Thickness(10);

            var screenWidth = DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density;
            
            // Parse parameter in format "small:medium:large" (e.g., "5:10:15")
            var parts = parameterString.Split(':');
            
            double margin = screenWidth switch
            {
                < 400 when parts.Length >= 1 && double.TryParse(parts[0], out double small) => small,
                < 600 when parts.Length >= 2 && double.TryParse(parts[1], out double medium) => medium,
                _ when parts.Length >= 3 && double.TryParse(parts[2], out double large) => large,
                _ => 10
            };
            
            return new Thickness(margin);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ResponsiveColumnsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string parameterString)
                return 1;

            var screenWidth = DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density;
            
            // Parse parameter in format "small:medium:large" (e.g., "1:2:3")
            var parts = parameterString.Split(':');
            
            int columns = screenWidth switch
            {
                < 400 when parts.Length >= 1 && int.TryParse(parts[0], out int small) => small,
                < 600 when parts.Length >= 2 && int.TryParse(parts[1], out int medium) => medium,
                _ when parts.Length >= 3 && int.TryParse(parts[2], out int large) => large,
                _ => 1
            };
            
            return columns;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class OrientationBasedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string parameterString)
                return value;

            var orientation = DeviceDisplay.Current.MainDisplayInfo.Orientation;
            
            // Parse parameter in format "portrait:landscape" (e.g., "vertical:horizontal")
            var parts = parameterString.Split(':');
            
            return orientation == DisplayOrientation.Portrait 
                ? (parts.Length >= 1 ? parts[0] : "vertical")
                : (parts.Length >= 2 ? parts[1] : "horizontal");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}