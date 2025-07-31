using System.Globalization;
using SubExplore.Models.Enums;

namespace SubExplore.Helpers.Converters
{
    /// <summary>
    /// Converter for tab background colors
    /// </summary>
    public class TabBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int selectedIndex && parameter is string tabIndexStr && int.TryParse(tabIndexStr, out int tabIndex))
            {
                return selectedIndex == tabIndex ? Application.Current?.Resources["Primary"] : Application.Current?.Resources["Gray100"];
            }
            return Application.Current?.Resources["Gray100"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for tab text colors
    /// </summary>
    public class TabTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int selectedIndex && parameter is string tabIndexStr && int.TryParse(tabIndexStr, out int tabIndex))
            {
                return selectedIndex == tabIndex ? Colors.White : Colors.Black;
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for int to bool comparison
    /// </summary>
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue && parameter is string paramStr && int.TryParse(paramStr, out int paramValue))
            {
                return intValue == paramValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for enum to bool comparison
    /// </summary>
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;

            var enumValue = value.ToString();
            var paramValue = parameter.ToString();

            return string.Equals(enumValue, paramValue, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to check if object is not null
    /// </summary>
    public class IsNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to invert boolean values
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    /// <summary>
    /// Converter for validation status to color
    /// </summary>
    public class ValidationStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SpotValidationStatus status)
            {
                return status switch
                {
                    SpotValidationStatus.Pending => Colors.Orange,
                    SpotValidationStatus.UnderReview => Colors.Blue,
                    SpotValidationStatus.Approved => Colors.Green,
                    SpotValidationStatus.Rejected => Colors.Red,
                    SpotValidationStatus.SafetyReview => Colors.Purple,
                    _ => Colors.Gray
                };
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for validation status to text
    /// </summary>
    public class ValidationStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SpotValidationStatus status)
            {
                return status switch
                {
                    SpotValidationStatus.Pending => "En attente",
                    SpotValidationStatus.UnderReview => "En cours de révision",
                    SpotValidationStatus.Approved => "Approuvé",
                    SpotValidationStatus.Rejected => "Rejeté",
                    SpotValidationStatus.SafetyReview => "Révision de sécurité",
                    _ => "Inconnu"
                };
            }
            return "Inconnu";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}