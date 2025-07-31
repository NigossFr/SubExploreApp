using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using SubExplore.Models.Enums;


namespace SubExplore.Helpers.Converters
{
    public class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if we have a parameter for conditional text (like password visibility toggle)
            if (parameter is string parameterString && parameterString.Contains('|'))
            {
                var parts = parameterString.Split('|');
                if (parts.Length == 2)
                {
                    // Return first part if value is true, second part if false
                    bool boolValue = value is bool b && b;
                    return boolValue ? parts[0] : parts[1];
                }
            }
            
            // Default behavior: check if string is not empty
            return !string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringEqualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Si égaux, retourner la couleur primaire, sinon la couleur secondaire
            if (value == null || parameter == null) return Colors.Transparent;

            bool isEqual = value.ToString() == parameter.ToString();
            return isEqual ? Application.Current.Resources["Primary"] : Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IntEqualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;

            bool isEqual = int.Parse(value.ToString()) == int.Parse(parameter.ToString());
            return isEqual;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IntGreaterThanOrEqualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;

            bool isGreaterOrEqual = int.Parse(value.ToString()) >= int.Parse(parameter.ToString());
            return isGreaterOrEqual ? Application.Current.Resources["Primary"] : Application.Current.Resources["Secondary"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IntGreaterThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;

            bool isGreater = int.Parse(value.ToString()) > int.Parse(parameter.ToString());
            return isGreater;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IntLessThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;

            bool isLess = int.Parse(value.ToString()) < int.Parse(parameter.ToString());
            return isLess;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SelectedItemToBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Colors.White;

            // Si l'item est sélectionné, utiliser sa couleur, sinon blanc
            bool isSelected = value.Equals(parameter);
            return isSelected ? ((dynamic)value).ColorCode : Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SelectedItemToTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Colors.Black;

            // Si l'item est sélectionné, texte blanc, sinon noir
            bool isSelected = value.Equals(parameter);
            return isSelected ? Colors.White : Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }

    public class CollectionCountToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0 ? 150 : 50;
            }
            return 50;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class BoolToTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected)
            {
                return isSelected ? Colors.White : Colors.Black;
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // IsNotNullConverter is defined in ValidationConverters.cs to avoid duplication

    /// <summary>
    /// Converter for checking if a string is not null or empty
    /// </summary>
    public class IsNotNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for boolean to object selection with parameter
    /// </summary>
    public class BoolToObjectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string parameterString)
            {
                var options = parameterString.Split('|');
                if (options.Length == 2)
                {
                    return boolValue ? options[0] : options[1];
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for priority to visual style
    /// </summary>
    public class PriorityToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int priority)
            {
                return priority switch
                {
                    <= 3 => "HighPriorityIndicator",
                    <= 6 => "MediumPriorityIndicator",
                    _ => "LowPriorityIndicator"
                };
            }
            return "LowPriorityIndicator";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for weather conditions and icon codes to weather icons
    /// Maps both OpenWeatherMap icon codes and condition text to appropriate emoji icons
    /// Supports both single value conversion and multi-binding with fallback
    /// </summary>
    public class WeatherIconConverter : IValueConverter, IMultiValueConverter
    {
        private static readonly Dictionary<string, string> IconCodeToEmoji = new()
        {
            // Clear sky
            { "01d", "☀️" }, // clear sky day
            { "01n", "🌙" }, // clear sky night
            
            // Few clouds
            { "02d", "🌤️" }, // few clouds day
            { "02n", "☁️" }, // few clouds night
            
            // Scattered/broken clouds
            { "03d", "⛅" }, // scattered clouds day
            { "03n", "☁️" }, // scattered clouds night
            { "04d", "☁️" }, // broken clouds day
            { "04n", "☁️" }, // broken clouds night
            
            // Shower rain
            { "09d", "🌦️" }, // shower rain day
            { "09n", "🌧️" }, // shower rain night
            
            // Rain
            { "10d", "🌦️" }, // rain day
            { "10n", "🌧️" }, // rain night
            
            // Thunderstorm
            { "11d", "⛈️" }, // thunderstorm day
            { "11n", "⛈️" }, // thunderstorm night
            
            // Snow
            { "13d", "🌨️" }, // snow day
            { "13n", "❄️" }, // snow night
            
            // Mist/Atmosphere
            { "50d", "🌫️" }, // mist day
            { "50n", "🌫️" }, // mist night
        };

        private static readonly Dictionary<string, string> ConditionToEmoji = new()
        {
            // Clear conditions
            { "clear", "☀️" },
            { "sunny", "☀️" },
            
            // Cloudy conditions
            { "clouds", "☁️" },
            { "cloudy", "☁️" },
            { "overcast", "☁️" },
            { "partly cloudy", "⛅" },
            { "few clouds", "🌤️" },
            { "scattered clouds", "⛅" },
            { "broken clouds", "☁️" },
            
            // Rain conditions
            { "rain", "🌧️" },
            { "rainy", "🌧️" },
            { "drizzle", "🌦️" },
            { "shower", "🌦️" },
            { "light rain", "🌦️" },
            { "moderate rain", "🌧️" },
            { "heavy rain", "🌧️" },
            
            // Thunderstorm
            { "thunderstorm", "⛈️" },
            { "storm", "⛈️" },
            { "thunder", "⛈️" },
            
            // Snow
            { "snow", "🌨️" },
            { "snowy", "❄️" },
            { "blizzard", "🌨️" },
            { "sleet", "🌨️" },
            
            // Atmosphere
            { "mist", "🌫️" },
            { "fog", "🌫️" },
            { "haze", "🌫️" },
            { "dust", "🌪️" },
            { "sand", "🌪️" },
            { "ash", "🌋" },
            { "squall", "🌪️" },
            { "tornado", "🌪️" },
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertToWeatherIcon(value?.ToString());
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Multi-binding: try icon code first, then condition as fallback
            if (values != null && values.Length > 0)
            {
                // Try icon code first (usually first binding)
                for (int i = 0; i < values.Length; i++)
                {
                    var result = ConvertToWeatherIcon(values[i]?.ToString());
                    if (result != "❓") // If we found a valid icon, use it
                    {
                        return result;
                    }
                }
            }
            
            return "❓"; // Unknown weather condition
        }

        private static string ConvertToWeatherIcon(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return "❓"; // Unknown weather icon

            string normalizedInput = input.ToLowerInvariant();

            // First try to match by OpenWeatherMap icon code (exact match)
            if (IconCodeToEmoji.ContainsKey(normalizedInput))
            {
                return IconCodeToEmoji[normalizedInput];
            }

            // Then try to match by condition name (exact match)
            if (ConditionToEmoji.ContainsKey(normalizedInput))
            {
                return ConditionToEmoji[normalizedInput];
            }

            // Finally try partial matching for conditions
            foreach (var condition in ConditionToEmoji.Keys)
            {
                if (normalizedInput.Contains(condition))
                {
                    return ConditionToEmoji[condition];
                }
            }

            // Default fallback for common patterns not covered above
            if (normalizedInput.Contains("sun"))
                return "☀️";
            if (normalizedInput.Contains("cloud"))
                return "☁️";
            if (normalizedInput.Contains("rain") || normalizedInput.Contains("precipitation"))
                return "🌧️";
            if (normalizedInput.Contains("storm"))
                return "⛈️";
            if (normalizedInput.Contains("snow") || normalizedInput.Contains("ice"))
                return "❄️";
            if (normalizedInput.Contains("fog") || normalizedInput.Contains("mist"))
                return "🌫️";
            if (normalizedInput.Contains("wind"))
                return "💨";

            return "❓"; // Unknown weather condition
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for DifficultyLevel enum to French display text
    /// </summary>
    public class DifficultyLevelToFrenchConverter : IValueConverter
    {
        private static readonly Dictionary<DifficultyLevel, string> DifficultyTranslations = new()
        {
            { DifficultyLevel.Beginner, "Débutant" },
            { DifficultyLevel.Intermediate, "Intermédiaire" },
            { DifficultyLevel.Advanced, "Avancé" },
            { DifficultyLevel.Expert, "Expert" },
            { DifficultyLevel.TechnicalOnly, "Technique uniquement" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DifficultyLevel difficulty)
            {
                return DifficultyTranslations.TryGetValue(difficulty, out string translation) 
                    ? translation 
                    : difficulty.ToString();
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                var pair = DifficultyTranslations.FirstOrDefault(kvp => kvp.Value == text);
                return pair.Key != default ? pair.Key : DifficultyLevel.Beginner;
            }
            return DifficultyLevel.Beginner;
        }
    }

    /// <summary>
    /// Converter for CurrentStrength enum to French display text
    /// </summary>
    public class CurrentStrengthToFrenchConverter : IValueConverter
    {
        private static readonly Dictionary<CurrentStrength, string> CurrentStrengthTranslations = new()
        {
            { CurrentStrength.None, "Aucun" },
            { CurrentStrength.Weak, "Léger" },
            { CurrentStrength.Moderate, "Modéré" },
            { CurrentStrength.Strong, "Fort" },
            { CurrentStrength.Extreme, "Extrême" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CurrentStrength currentStrength)
            {
                return CurrentStrengthTranslations.TryGetValue(currentStrength, out string translation) 
                    ? translation 
                    : currentStrength.ToString();
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                var pair = CurrentStrengthTranslations.FirstOrDefault(kvp => kvp.Value == text);
                return pair.Key != default ? pair.Key : CurrentStrength.Weak;
            }
            return CurrentStrength.Weak;
        }
    }

}
