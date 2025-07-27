using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for retrieving weather information and forecasts
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>
        /// Get current weather conditions for specific coordinates
        /// </summary>
        /// <param name="latitude">Latitude coordinate</param>
        /// <param name="longitude">Longitude coordinate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current weather information</returns>
        Task<WeatherInfo?> GetCurrentWeatherAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get weather forecast for specific coordinates
        /// </summary>
        /// <param name="latitude">Latitude coordinate</param>
        /// <param name="longitude">Longitude coordinate</param>
        /// <param name="days">Number of days to forecast (default: 3)</param>
        /// <param name="includeHourly">Include hourly forecast (default: true)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Weather forecast</returns>
        Task<WeatherForecast?> GetWeatherForecastAsync(decimal latitude, decimal longitude, int days = 3, bool includeHourly = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get weather conditions suitable for diving activities
        /// </summary>
        /// <param name="latitude">Latitude coordinate</param>
        /// <param name="longitude">Longitude coordinate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Diving-specific weather assessment</returns>
        Task<DivingWeatherConditions?> GetDivingConditionsAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if weather service is available and configured
        /// </summary>
        /// <returns>True if service is available</returns>
        Task<bool> IsServiceAvailableAsync();

        /// <summary>
        /// Get weather data freshness status
        /// </summary>
        /// <param name="latitude">Latitude coordinate</param>
        /// <param name="longitude">Longitude coordinate</param>
        /// <returns>Data freshness information</returns>
        Task<WeatherDataFreshness> GetDataFreshnessAsync(decimal latitude, decimal longitude);
    }

    /// <summary>
    /// Diving-specific weather conditions assessment
    /// </summary>
    public class DivingWeatherConditions
    {
        public WeatherInfo CurrentWeather { get; set; } = new();
        public string OverallCondition { get; set; } = string.Empty; // Excellent, Good, Fair, Poor, Dangerous
        public string Assessment { get; set; } = string.Empty; // Detailed assessment
        public List<string> Warnings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public int SafetyScore { get; set; } // 0-100
        public bool IsSafeForDiving { get; set; }
        public bool IsSafeForSnorkeling { get; set; }
        public string BestTimeToday { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Get safety level based on score
        /// </summary>
        public string GetSafetyLevel()
        {
            return SafetyScore switch
            {
                >= 80 => "Très sûr",
                >= 60 => "Sûr",
                >= 40 => "Attention requise",
                >= 20 => "Risqué",
                _ => "Dangereux"
            };
        }

        /// <summary>
        /// Get safety color for UI display
        /// </summary>
        public string GetSafetyColor()
        {
            return SafetyScore switch
            {
                >= 80 => "#4CAF50", // Green
                >= 60 => "#8BC34A", // Light Green
                >= 40 => "#FFC107", // Amber
                >= 20 => "#FF9800", // Orange
                _ => "#F44336" // Red
            };
        }
    }

    /// <summary>
    /// Weather data freshness information
    /// </summary>
    public class WeatherDataFreshness
    {
        public DateTime LastUpdated { get; set; }
        public bool IsStale { get; set; }
        public TimeSpan Age { get; set; }
        public string Status { get; set; } = string.Empty; // Fresh, Stale, Very Old, Unavailable
        public bool NeedsRefresh { get; set; }

        /// <summary>
        /// Get freshness status color
        /// </summary>
        public string GetStatusColor()
        {
            return Status switch
            {
                "Fresh" => "#4CAF50",
                "Stale" => "#FFC107",
                "Very Old" => "#FF9800",
                _ => "#F44336"
            };
        }
    }
}