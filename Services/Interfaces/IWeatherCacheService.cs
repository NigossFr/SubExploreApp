using System;
using System.Threading;
using System.Threading.Tasks;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Caching service for weather data to minimize API calls and improve performance
    /// </summary>
    public interface IWeatherCacheService
    {
        /// <summary>
        /// Get cached current weather for coordinates
        /// </summary>
        /// <param name="latitude">Latitude coordinate</param>
        /// <param name="longitude">Longitude coordinate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cached weather info or null if not found/expired</returns>
        Task<WeatherInfo?> GetCachedCurrentWeatherAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cache current weather data
        /// </summary>
        /// <param name="weatherInfo">Weather information to cache</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SetCurrentWeatherCacheAsync(WeatherInfo weatherInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get cached weather forecast for coordinates
        /// </summary>
        /// <param name="latitude">Latitude coordinate</param>
        /// <param name="longitude">Longitude coordinate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cached forecast or null if not found/expired</returns>
        Task<WeatherForecast?> GetCachedForecastAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cache forecast data
        /// </summary>
        /// <param name="forecast">Forecast to cache</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SetForecastCacheAsync(WeatherForecast forecast, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get cached diving conditions
        /// </summary>
        /// <param name="latitude">Latitude coordinate</param>
        /// <param name="longitude">Longitude coordinate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cached diving conditions or null if not found/expired</returns>
        Task<DivingWeatherConditions?> GetCachedDivingConditionsAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cache diving conditions
        /// </summary>
        /// <param name="conditions">Diving conditions to cache</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SetDivingConditionsCacheAsync(DivingWeatherConditions conditions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear expired weather cache entries
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ClearExpiredCacheAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear all weather cache
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ClearAllCacheAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if weather data exists in cache for coordinates
        /// </summary>
        /// <param name="latitude">Latitude coordinate</param>
        /// <param name="longitude">Longitude coordinate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if fresh data exists in cache</returns>
        Task<bool> HasFreshWeatherDataAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get cache statistics for monitoring
        /// </summary>
        /// <returns>Cache usage statistics</returns>
        Task<WeatherCacheStats> GetCacheStatsAsync();
    }

    /// <summary>
    /// Weather cache statistics
    /// </summary>
    public class WeatherCacheStats
    {
        public int CurrentWeatherEntries { get; set; }
        public int ForecastEntries { get; set; }
        public int DivingConditionsEntries { get; set; }
        public int ExpiredEntries { get; set; }
        public double HitRate { get; set; }
        public DateTime LastCleanup { get; set; }
        public long TotalMemoryUsage { get; set; } // bytes
    }
}