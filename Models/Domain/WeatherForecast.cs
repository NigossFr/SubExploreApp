using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SubExplore.Models.Domain
{
    /// <summary>
    /// Weather forecast for multiple days
    /// </summary>
    public class WeatherForecast
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public List<DailyWeather> DailyForecasts { get; set; } = new();
        public List<HourlyWeather> HourlyForecasts { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public DateTime ValidUntil { get; set; }
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Get today's forecast
        /// </summary>
        public DailyWeather? GetTodayForecast()
        {
            var today = DateTime.Today;
            return DailyForecasts.FirstOrDefault(d => d.Date.Date == today);
        }

        /// <summary>
        /// Get next few hours forecast (useful for diving planning)
        /// </summary>
        public List<HourlyWeather> GetNextHoursForecast(int hours = 6)
        {
            var now = DateTime.Now;
            return HourlyForecasts
                .Where(h => h.DateTime >= now && h.DateTime <= now.AddHours(hours))
                .OrderBy(h => h.DateTime)
                .ToList();
        }

        /// <summary>
        /// Check if forecast data is still valid
        /// </summary>
        public bool IsValid => DateTime.UtcNow <= ValidUntil;
    }

    /// <summary>
    /// Daily weather forecast
    /// </summary>
    public class DailyWeather
    {
        public DateTime Date { get; set; }
        public double MinTemperature { get; set; }
        public double MaxTemperature { get; set; }
        public string Condition { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconCode { get; set; } = string.Empty;
        public int ChanceOfRain { get; set; }
        public double Precipitation { get; set; }
        public double WindSpeed { get; set; }
        public int WindDirection { get; set; }
        public double UvIndex { get; set; }
        public DateTime? Sunrise { get; set; }
        public DateTime? Sunset { get; set; }

        /// <summary>
        /// Get diving suitability for this day
        /// </summary>
        public string GetDivingSuitability()
        {
            var score = 0;
            
            if (WindSpeed <= 15) score += 2;
            else if (WindSpeed <= 25) score += 1;
            
            if (ChanceOfRain <= 30) score += 2;
            else if (ChanceOfRain <= 60) score += 1;
            
            if (MinTemperature >= 15 && MaxTemperature <= 35) score += 1;
            
            return score switch
            {
                >= 4 => "Idéal",
                >= 2 => "Bon",
                >= 1 => "Acceptable",
                _ => "Déconseillé"
            };
        }
    }

    /// <summary>
    /// Hourly weather forecast
    /// </summary>
    public class HourlyWeather
    {
        public DateTime DateTime { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public string Condition { get; set; } = string.Empty;
        public string IconCode { get; set; } = string.Empty;
        public int ChanceOfRain { get; set; }
        public double Precipitation { get; set; }
        public double WindSpeed { get; set; }
        public int WindDirection { get; set; }
        public int Humidity { get; set; }
        public double Visibility { get; set; }
        public double? WaveHeight { get; set; }

        /// <summary>
        /// Get wind direction text
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
    }
}