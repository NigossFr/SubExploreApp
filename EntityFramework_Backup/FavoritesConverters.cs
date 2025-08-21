using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace SubExplore.Helpers.Converters
{
    /// <summary>
    /// Converter for priority filter display
    /// </summary>
    public class PriorityFilterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int priority)
            {
                return $"🎯 P{priority}";
            }
            return "🎯 Toutes";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for notification filter display
    /// </summary>
    public class NotificationFilterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Activées" => "🔔 Activées",
                "Désactivées" => "🔕 Désactivées",
                _ => "🔔 Toutes"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for spot difficulty to color
    /// </summary>
    public class DifficultyToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string difficulty)
            {
                return difficulty.ToLowerInvariant() switch
                {
                    "débutant" or "facile" => Colors.Green,
                    "intermédiaire" or "moyen" => Colors.Orange,
                    "avancé" or "difficile" => Colors.Red,
                    "expert" or "très difficile" => Colors.DarkRed,
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
    /// Converter for favorite count to display text
    /// </summary>
    public class FavoriteCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count switch
                {
                    0 => "Aucun favori",
                    1 => "1 favori",
                    _ => $"{count} favoris"
                };
            }
            return "Aucun favori";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for time since added to favorites
    /// </summary>
    public class TimeSinceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var timeSpan = DateTime.Now - dateTime;
                
                if (timeSpan.TotalDays >= 1)
                {
                    return $"il y a {(int)timeSpan.TotalDays} jour{((int)timeSpan.TotalDays > 1 ? "s" : "")}";
                }
                else if (timeSpan.TotalHours >= 1)
                {
                    return $"il y a {(int)timeSpan.TotalHours} heure{((int)timeSpan.TotalHours > 1 ? "s" : "")}";
                }
                else if (timeSpan.TotalMinutes >= 1)
                {
                    return $"il y a {(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes > 1 ? "s" : "")}";
                }
                else
                {
                    return "à l'instant";
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for priority to visual indicator
    /// </summary>
    public class PriorityToIndicatorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int priority)
            {
                return priority switch
                {
                    1 or 2 => "🔴",  // High priority
                    3 or 4 or 5 => "🟡",  // Medium priority
                    _ => "🟢"  // Low priority
                };
            }
            return "⚪";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}