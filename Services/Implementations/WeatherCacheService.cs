using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// In-memory caching service for weather data
    /// </summary>
    public class WeatherCacheService : IWeatherCacheService
    {
        private readonly ILogger<WeatherCacheService> _logger;
        private readonly ConcurrentDictionary<string, WeatherInfo> _currentWeatherCache;
        private readonly ConcurrentDictionary<string, WeatherForecast> _forecastCache;
        private readonly ConcurrentDictionary<string, DivingWeatherConditions> _divingConditionsCache;
        private readonly Timer _cleanupTimer;
        
        // Statistics
        private long _totalRequests;
        private long _cacheHits;
        private DateTime _lastCleanup;

        public WeatherCacheService(ILogger<WeatherCacheService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentWeatherCache = new ConcurrentDictionary<string, WeatherInfo>();
            _forecastCache = new ConcurrentDictionary<string, WeatherForecast>();
            _divingConditionsCache = new ConcurrentDictionary<string, DivingWeatherConditions>();
            _lastCleanup = DateTime.UtcNow;

            // Setup cleanup timer to run every 15 minutes
            _cleanupTimer = new Timer(async _ => await CleanupExpiredEntries(), null, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
        }

        /// <summary>
        /// Get cached current weather for coordinates
        /// </summary>
        public async Task<WeatherInfo?> GetCachedCurrentWeatherAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                Interlocked.Increment(ref _totalRequests);
                
                var key = GenerateLocationKey(latitude, longitude);
                
                if (_currentWeatherCache.TryGetValue(key, out var weatherInfo))
                {
                    if (weatherInfo.IsValid)
                    {
                        Interlocked.Increment(ref _cacheHits);
                        _logger.LogDebug("Cache hit for current weather at {Lat}, {Lon}", latitude, longitude);
                        return weatherInfo;
                    }
                    else
                    {
                        // Remove expired entry
                        _currentWeatherCache.TryRemove(key, out _);
                        _logger.LogDebug("Removed expired weather cache entry for {Lat}, {Lon}", latitude, longitude);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cached weather data");
                return null;
            }
        }

        /// <summary>
        /// Cache current weather data
        /// </summary>
        public async Task SetCurrentWeatherCacheAsync(WeatherInfo weatherInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                if (weatherInfo == null)
                {
                    _logger.LogWarning("Attempted to cache null weather info");
                    return;
                }

                var key = GenerateLocationKey(weatherInfo.Latitude, weatherInfo.Longitude);
                _currentWeatherCache.AddOrUpdate(key, weatherInfo, (k, existing) => weatherInfo);
                
                _logger.LogDebug("Cached current weather for {Lat}, {Lon}", weatherInfo.Latitude, weatherInfo.Longitude);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching weather data");
            }
        }

        /// <summary>
        /// Get cached weather forecast for coordinates
        /// </summary>
        public async Task<WeatherForecast?> GetCachedForecastAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                Interlocked.Increment(ref _totalRequests);
                
                var key = GenerateLocationKey(latitude, longitude);
                
                if (_forecastCache.TryGetValue(key, out var forecast))
                {
                    if (forecast.IsValid)
                    {
                        Interlocked.Increment(ref _cacheHits);
                        _logger.LogDebug("Cache hit for forecast at {Lat}, {Lon}", latitude, longitude);
                        return forecast;
                    }
                    else
                    {
                        // Remove expired entry
                        _forecastCache.TryRemove(key, out _);
                        _logger.LogDebug("Removed expired forecast cache entry for {Lat}, {Lon}", latitude, longitude);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cached forecast data");
                return null;
            }
        }

        /// <summary>
        /// Cache forecast data
        /// </summary>
        public async Task SetForecastCacheAsync(WeatherForecast forecast, CancellationToken cancellationToken = default)
        {
            try
            {
                if (forecast == null)
                {
                    _logger.LogWarning("Attempted to cache null forecast");
                    return;
                }

                var key = GenerateLocationKey(forecast.Latitude, forecast.Longitude);
                _forecastCache.AddOrUpdate(key, forecast, (k, existing) => forecast);
                
                _logger.LogDebug("Cached forecast for {Lat}, {Lon}", forecast.Latitude, forecast.Longitude);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching forecast data");
            }
        }

        /// <summary>
        /// Get cached diving conditions
        /// </summary>
        public async Task<DivingWeatherConditions?> GetCachedDivingConditionsAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                Interlocked.Increment(ref _totalRequests);
                
                var key = GenerateLocationKey(latitude, longitude);
                
                if (_divingConditionsCache.TryGetValue(key, out var conditions))
                {
                    if (conditions.CurrentWeather.IsValid)
                    {
                        Interlocked.Increment(ref _cacheHits);
                        _logger.LogDebug("Cache hit for diving conditions at {Lat}, {Lon}", latitude, longitude);
                        return conditions;
                    }
                    else
                    {
                        // Remove expired entry
                        _divingConditionsCache.TryRemove(key, out _);
                        _logger.LogDebug("Removed expired diving conditions cache entry for {Lat}, {Lon}", latitude, longitude);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cached diving conditions");
                return null;
            }
        }

        /// <summary>
        /// Cache diving conditions
        /// </summary>
        public async Task SetDivingConditionsCacheAsync(DivingWeatherConditions conditions, CancellationToken cancellationToken = default)
        {
            try
            {
                if (conditions?.CurrentWeather == null)
                {
                    _logger.LogWarning("Attempted to cache null diving conditions");
                    return;
                }

                var key = GenerateLocationKey(conditions.CurrentWeather.Latitude, conditions.CurrentWeather.Longitude);
                _divingConditionsCache.AddOrUpdate(key, conditions, (k, existing) => conditions);
                
                _logger.LogDebug("Cached diving conditions for {Lat}, {Lon}", conditions.CurrentWeather.Latitude, conditions.CurrentWeather.Longitude);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching diving conditions");
            }
        }

        /// <summary>
        /// Clear expired weather cache entries
        /// </summary>
        public async Task ClearExpiredCacheAsync(CancellationToken cancellationToken = default)
        {
            await CleanupExpiredEntries();
        }

        /// <summary>
        /// Clear all weather cache
        /// </summary>
        public async Task ClearAllCacheAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var removedWeather = _currentWeatherCache.Count;
                var removedForecast = _forecastCache.Count;
                var removedDiving = _divingConditionsCache.Count;

                _currentWeatherCache.Clear();
                _forecastCache.Clear();
                _divingConditionsCache.Clear();

                _logger.LogInformation("Cleared all weather cache: {Weather} weather entries, {Forecast} forecast entries, {Diving} diving conditions entries", 
                    removedWeather, removedForecast, removedDiving);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing weather cache");
            }
        }

        /// <summary>
        /// Check if weather data exists in cache for coordinates
        /// </summary>
        public async Task<bool> HasFreshWeatherDataAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = GenerateLocationKey(latitude, longitude);
                
                if (_currentWeatherCache.TryGetValue(key, out var weatherInfo))
                {
                    return weatherInfo.IsValid && !weatherInfo.NeedsRefresh;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking fresh weather data");
                return false;
            }
        }

        /// <summary>
        /// Get cache statistics for monitoring
        /// </summary>
        public async Task<WeatherCacheStats> GetCacheStatsAsync()
        {
            try
            {
                var totalRequests = Interlocked.Read(ref _totalRequests);
                var cacheHits = Interlocked.Read(ref _cacheHits);
                
                var expiredEntries = 0;
                expiredEntries += _currentWeatherCache.Values.Count(w => !w.IsValid);
                expiredEntries += _forecastCache.Values.Count(f => !f.IsValid);
                expiredEntries += _divingConditionsCache.Values.Count(d => !d.CurrentWeather.IsValid);

                // Rough memory estimation (simplified)
                var memoryUsage = (_currentWeatherCache.Count * 1024) + 
                                 (_forecastCache.Count * 4096) + 
                                 (_divingConditionsCache.Count * 2048);

                return new WeatherCacheStats
                {
                    CurrentWeatherEntries = _currentWeatherCache.Count,
                    ForecastEntries = _forecastCache.Count,
                    DivingConditionsEntries = _divingConditionsCache.Count,
                    ExpiredEntries = expiredEntries,
                    HitRate = totalRequests > 0 ? (double)cacheHits / totalRequests : 0,
                    LastCleanup = _lastCleanup,
                    TotalMemoryUsage = memoryUsage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache statistics");
                return new WeatherCacheStats();
            }
        }

        /// <summary>
        /// Generate a consistent cache key for coordinates
        /// </summary>
        private static string GenerateLocationKey(decimal latitude, decimal longitude)
        {
            // Round to 3 decimal places for reasonable cache grouping (approx 100m precision)
            var roundedLat = Math.Round(latitude, 3);
            var roundedLon = Math.Round(longitude, 3);
            return $"{roundedLat:F3},{roundedLon:F3}";
        }

        /// <summary>
        /// Internal cleanup method for expired entries
        /// </summary>
        private async Task CleanupExpiredEntries()
        {
            try
            {
                var removedWeather = 0;
                var removedForecast = 0;
                var removedDiving = 0;

                // Clean current weather cache
                var expiredWeatherKeys = _currentWeatherCache
                    .Where(kvp => !kvp.Value.IsValid)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredWeatherKeys)
                {
                    if (_currentWeatherCache.TryRemove(key, out _))
                        removedWeather++;
                }

                // Clean forecast cache
                var expiredForecastKeys = _forecastCache
                    .Where(kvp => !kvp.Value.IsValid)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredForecastKeys)
                {
                    if (_forecastCache.TryRemove(key, out _))
                        removedForecast++;
                }

                // Clean diving conditions cache
                var expiredDivingKeys = _divingConditionsCache
                    .Where(kvp => !kvp.Value.CurrentWeather.IsValid)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredDivingKeys)
                {
                    if (_divingConditionsCache.TryRemove(key, out _))
                        removedDiving++;
                }

                _lastCleanup = DateTime.UtcNow;

                if (removedWeather + removedForecast + removedDiving > 0)
                {
                    _logger.LogInformation("Cache cleanup removed {Weather} weather, {Forecast} forecast, {Diving} diving condition entries", 
                        removedWeather, removedForecast, removedDiving);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }
}