using System;
using System.ComponentModel.DataAnnotations;

namespace SubExplore.Models.Domain
{
    /// <summary>
    /// Weather information for a specific location and time
    /// </summary>
    public class WeatherInfo
    {
        /// <summary>
        /// Location coordinates
        /// </summary>
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        /// <summary>
        /// Current weather conditions
        /// </summary>
        public double Temperature { get; set; } // Celsius
        public double FeelsLike { get; set; } // Celsius
        public int Humidity { get; set; } // Percentage
        public double Pressure { get; set; } // hPa
        public double Visibility { get; set; } // km
        public int CloudCover { get; set; } // Percentage

        /// <summary>
        /// Wind information
        /// </summary>
        public double WindSpeed { get; set; } // km/h
        public int WindDirection { get; set; } // degrees
        public string WindDirectionText { get; set; } = string.Empty; // N, NE, E, etc.

        /// <summary>
        /// Weather description
        /// </summary>
        public string Condition { get; set; } = string.Empty; // Clear, Cloudy, Rainy, etc.
        public string Description { get; set; } = string.Empty; // Detailed description
        public string IconCode { get; set; } = string.Empty; // Weather icon identifier

        /// <summary>
        /// Precipitation
        /// </summary>
        public double Precipitation { get; set; } // mm
        public int ChanceOfRain { get; set; } // Percentage

        /// <summary>
        /// UV and Sun information
        /// </summary>
        public double UvIndex { get; set; }
        public DateTime? Sunrise { get; set; }
        public DateTime? Sunset { get; set; }

        /// <summary>
        /// Water-specific conditions (important for diving/snorkeling)
        /// </summary>
        public double? WaterTemperature { get; set; } // Celsius
        public double? WaveHeight { get; set; } // meters
        public string? WaterVisibility { get; set; } // Excellent, Good, Fair, Poor
        public string? SeaConditions { get; set; } // Calm, Slight, Moderate, Rough

        /// <summary>
        /// Timestamp information
        /// </summary>
        public DateTime LastUpdated { get; set; }
        public DateTime ValidUntil { get; set; }

        /// <summary>
        /// Data source information
        /// </summary>
        public string Source { get; set; } = string.Empty; // API source name
        public bool IsHistorical { get; set; } = false;

        /// <summary>
        /// Get wind direction text from degrees
        /// </summary>
        public string GetWindDirectionText()
        {
            if (WindDirection >= 348.75 || WindDirection < 11.25) return "N";
            if (WindDirection >= 11.25 && WindDirection < 33.75) return "NNE";
            if (WindDirection >= 33.75 && WindDirection < 56.25) return "NE";
            if (WindDirection >= 56.25 && WindDirection < 78.75) return "ENE";
            if (WindDirection >= 78.75 && WindDirection < 101.25) return "E";
            if (WindDirection >= 101.25 && WindDirection < 123.75) return "ESE";
            if (WindDirection >= 123.75 && WindDirection < 146.25) return "SE";
            if (WindDirection >= 146.25 && WindDirection < 168.75) return "SSE";
            if (WindDirection >= 168.75 && WindDirection < 191.25) return "S";
            if (WindDirection >= 191.25 && WindDirection < 213.75) return "SSW";
            if (WindDirection >= 213.75 && WindDirection < 236.25) return "SW";
            if (WindDirection >= 236.25 && WindDirection < 258.75) return "WSW";
            if (WindDirection >= 258.75 && WindDirection < 281.25) return "W";
            if (WindDirection >= 281.25 && WindDirection < 303.75) return "WNW";
            if (WindDirection >= 303.75 && WindDirection < 326.25) return "NW";
            if (WindDirection >= 326.25 && WindDirection < 348.75) return "NNW";
            return "N";
        }

        /// <summary>
        /// Get UV index description
        /// </summary>
        public string GetUvIndexDescription()
        {
            return UvIndex switch
            {
                <= 2 => "Faible",
                <= 5 => "Modéré",
                <= 7 => "Élevé",
                <= 10 => "Très élevé",
                _ => "Extrême"
            };
        }

        /// <summary>
        /// Get diving conditions assessment based on weather data
        /// </summary>
        public string GetDivingConditionsAssessment()
        {
            var score = 0;
            
            // Wind conditions (most important for surface conditions)
            if (WindSpeed <= 10) score += 3;
            else if (WindSpeed <= 20) score += 2;
            else if (WindSpeed <= 30) score += 1;
            
            // Wave height (if available)
            if (WaveHeight.HasValue)
            {
                if (WaveHeight <= 0.5) score += 3;
                else if (WaveHeight <= 1.0) score += 2;
                else if (WaveHeight <= 1.5) score += 1;
            }
            else
            {
                // Estimate from wind
                if (WindSpeed <= 15) score += 2;
                else if (WindSpeed <= 25) score += 1;
            }
            
            // Visibility
            if (Visibility >= 10) score += 2;
            else if (Visibility >= 5) score += 1;
            
            // Rain
            if (ChanceOfRain <= 20) score += 2;
            else if (ChanceOfRain <= 50) score += 1;
            
            return score switch
            {
                >= 8 => "Excellentes",
                >= 6 => "Bonnes",
                >= 4 => "Correctes",
                >= 2 => "Difficiles",
                _ => "Mauvaises"
            };
        }

        /// <summary>
        /// Check if weather data is still valid (not expired)
        /// </summary>
        public bool IsValid => DateTime.UtcNow <= ValidUntil;

        /// <summary>
        /// Check if weather data needs refresh (older than 30 minutes)
        /// </summary>
        public bool NeedsRefresh => DateTime.UtcNow.Subtract(LastUpdated).TotalMinutes > 30;
    }
}