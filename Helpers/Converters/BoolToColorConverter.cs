using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Maui.Graphics;

namespace SubExplore.Helpers.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isSelected)
            {
                if (parameter is string colorCode && !string.IsNullOrEmpty(colorCode) && isSelected)
                {
                    try
                    {
                        return Color.FromArgb(colorCode);
                    }
                    catch
                    {
                        // Fallback to default color if parsing fails
                        return isSelected ? Colors.Green : Colors.White;
                    }
                }
                return isSelected ? Colors.Green : Colors.White;
            }
            return Colors.White;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
