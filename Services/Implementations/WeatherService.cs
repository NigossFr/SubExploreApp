using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Weather service implementation using OpenWeatherMap API
    /// </summary>
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IWeatherCacheService _cacheService;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly IConnectivityService _connectivityService;
        private readonly ILogger<WeatherService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://api.openweathermap.org/data/2.5/";

        public WeatherService(
            HttpClient httpClient,
            IWeatherCacheService cacheService,
            IErrorHandlingService errorHandlingService,
            IConnectivityService connectivityService,
            IConfiguration configuration,
            ILogger<WeatherService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Get API key from configuration - in production, this should be in secure storage
            _apiKey = configuration["WeatherService:ApiKey"] ?? "demo_api_key";
            
            if (_apiKey == "demo_api_key")
            {
                _logger.LogWarning("Using demo API key for weather service. Configure 'WeatherService:ApiKey' for production use.");
            }

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SubExplore/1.0");
        }

        /// <summary>
        /// Get current weather conditions for specific coordinates
        /// </summary>
        public async Task<WeatherInfo?> GetCurrentWeatherAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check cache first
                var cachedWeather = await _cacheService.GetCachedCurrentWeatherAsync(latitude, longitude, cancellationToken).ConfigureAwait(false);
                if (cachedWeather != null && !cachedWeather.NeedsRefresh)
                {
                    _logger.LogInformation("Returning cached weather data for {Lat}, {Lon}", latitude, longitude);
                    return cachedWeather;
                }

                // Check connectivity
                if (!_connectivityService.IsConnected)
                {
                    _logger.LogWarning("No internet connection available for weather data");
                    return cachedWeather; // Return stale cache if available
                }

                _logger.LogInformation("Fetching current weather for coordinates {Lat}, {Lon}", latitude, longitude);

                var url = $"{_baseUrl}weather?lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&appid={_apiKey}&units=metric&lang=fr";
                
                var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Weather API returned {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    return cachedWeather; // Return stale cache if available
                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var weatherData = JsonSerializer.Deserialize<OpenWeatherMapCurrentResponse>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (weatherData == null)
                {
                    _logger.LogError("Failed to deserialize weather response");
                    return cachedWeather;
                }

                var weatherInfo = MapCurrentWeatherResponse(weatherData, latitude, longitude);
                
                // Cache the result
                await _cacheService.SetCurrentWeatherCacheAsync(weatherInfo, cancellationToken).ConfigureAwait(false);
                
                _logger.LogInformation("Successfully retrieved and cached weather data for {Lat}, {Lon}", latitude, longitude);
                return weatherInfo;
            }
            catch (HttpRequestException ex)
            {
                await _errorHandlingService.HandleNetworkErrorAsync(ex, nameof(GetCurrentWeatherAsync));
                _logger.LogError(ex, "Network error while fetching weather data");
                return await _cacheService.GetCachedCurrentWeatherAsync(latitude, longitude, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout while fetching weather data");
                return await _cacheService.GetCachedCurrentWeatherAsync(latitude, longitude, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(GetCurrentWeatherAsync));
                _logger.LogError(ex, "Unexpected error while fetching weather data");
                return await _cacheService.GetCachedCurrentWeatherAsync(latitude, longitude, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get weather forecast for specific coordinates
        /// </summary>
        public async Task<WeatherForecast?> GetWeatherForecastAsync(decimal latitude, decimal longitude, int days = 3, bool includeHourly = true, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check cache first
                var cachedForecast = await _cacheService.GetCachedForecastAsync(latitude, longitude, cancellationToken).ConfigureAwait(false);
                if (cachedForecast != null && cachedForecast.IsValid)
                {
                    _logger.LogInformation("Returning cached forecast data for {Lat}, {Lon}", latitude, longitude);
                    return cachedForecast;
                }

                // Check connectivity
                if (!_connectivityService.IsConnected)
                {
                    _logger.LogWarning("No internet connection available for forecast data");
                    return cachedForecast;
                }

                _logger.LogInformation("Fetching weather forecast for coordinates {Lat}, {Lon}", latitude, longitude);

                var url = $"{_baseUrl}forecast?lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&appid={_apiKey}&units=metric&lang=fr&cnt={Math.Min(days * 8, 40)}";
                
                var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Forecast API returned {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    return cachedForecast;
                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var forecastData = JsonSerializer.Deserialize<OpenWeatherMapForecastResponse>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (forecastData == null)
                {
                    _logger.LogError("Failed to deserialize forecast response");
                    return cachedForecast;
                }

                var forecast = MapForecastResponse(forecastData, latitude, longitude);
                
                // Cache the result
                await _cacheService.SetForecastCacheAsync(forecast, cancellationToken).ConfigureAwait(false);
                
                _logger.LogInformation("Successfully retrieved and cached forecast data for {Lat}, {Lon}", latitude, longitude);
                return forecast;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(GetWeatherForecastAsync));
                _logger.LogError(ex, "Error while fetching forecast data");
                return await _cacheService.GetCachedForecastAsync(latitude, longitude, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get weather conditions suitable for diving activities
        /// </summary>
        public async Task<DivingWeatherConditions?> GetDivingConditionsAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check cache first
                var cachedConditions = await _cacheService.GetCachedDivingConditionsAsync(latitude, longitude, cancellationToken).ConfigureAwait(false);
                if (cachedConditions != null && cachedConditions.CurrentWeather.IsValid)
                {
                    return cachedConditions;
                }

                // Get current weather
                var currentWeather = await GetCurrentWeatherAsync(latitude, longitude, cancellationToken).ConfigureAwait(false);
                if (currentWeather == null)
                {
                    return cachedConditions;
                }

                // Get forecast for better assessment
                var forecast = await GetWeatherForecastAsync(latitude, longitude, 1, true, cancellationToken).ConfigureAwait(false);
                
                var divingConditions = AssessDivingConditions(currentWeather, forecast);
                
                // Cache the result
                await _cacheService.SetDivingConditionsCacheAsync(divingConditions, cancellationToken).ConfigureAwait(false);
                
                return divingConditions;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(GetDivingConditionsAsync));
                return await _cacheService.GetCachedDivingConditionsAsync(latitude, longitude, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Check if weather service is available and configured
        /// </summary>
        public async Task<bool> IsServiceAvailableAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey) || _apiKey == "demo_api_key")
                {
                    return false;
                }

                if (!_connectivityService.IsConnected)
                {
                    return false;
                }

                // Test API with a simple call
                var testUrl = $"{_baseUrl}weather?lat=43.2965&lon=5.3698&appid={_apiKey}";
                var response = await _httpClient.GetAsync(testUrl).ConfigureAwait(false);
                
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get weather data freshness status
        /// </summary>
        public async Task<WeatherDataFreshness> GetDataFreshnessAsync(decimal latitude, decimal longitude)
        {
            try
            {
                var cachedWeather = await _cacheService.GetCachedCurrentWeatherAsync(latitude, longitude).ConfigureAwait(false);
                
                if (cachedWeather == null)
                {
                    return new WeatherDataFreshness
                    {
                        Status = "Unavailable",
                        IsStale = true,
                        NeedsRefresh = true,
                        Age = TimeSpan.Zero,
                        LastUpdated = DateTime.MinValue
                    };
                }

                var age = DateTime.UtcNow - cachedWeather.LastUpdated;
                var status = age.TotalMinutes switch
                {
                    <= 15 => "Fresh",
                    <= 60 => "Stale",
                    <= 360 => "Very Old",
                    _ => "Unavailable"
                };

                return new WeatherDataFreshness
                {
                    LastUpdated = cachedWeather.LastUpdated,
                    Age = age,
                    Status = status,
                    IsStale = age.TotalMinutes > 15,
                    NeedsRefresh = cachedWeather.NeedsRefresh
                };
            }
            catch (Exception ex)
            {
                await _errorHandlingService.LogExceptionAsync(ex, nameof(GetDataFreshnessAsync));
                return new WeatherDataFreshness { Status = "Error", IsStale = true, NeedsRefresh = true };
            }
        }

        private static WeatherInfo MapCurrentWeatherResponse(OpenWeatherMapCurrentResponse response, decimal latitude, decimal longitude)
        {
            var now = DateTime.UtcNow;
            
            return new WeatherInfo
            {
                Latitude = latitude,
                Longitude = longitude,
                Temperature = response.Main?.Temp ?? 0,
                FeelsLike = response.Main?.FeelsLike ?? 0,
                Humidity = response.Main?.Humidity ?? 0,
                Pressure = response.Main?.Pressure ?? 0,
                Visibility = (response.Visibility ?? 0) / 1000.0, // Convert to km
                CloudCover = response.Clouds?.All ?? 0,
                WindSpeed = (response.Wind?.Speed ?? 0) * 3.6, // Convert m/s to km/h
                WindDirection = response.Wind?.Deg ?? 0,
                Condition = response.Weather?.FirstOrDefault()?.Main ?? string.Empty,
                Description = response.Weather?.FirstOrDefault()?.Description ?? string.Empty,
                IconCode = response.Weather?.FirstOrDefault()?.Icon ?? string.Empty,
                Precipitation = response.Rain?.OneHour ?? response.Snow?.OneHour ?? 0,
                UvIndex = 0, // Not available in current weather, would need separate UV API call
                Sunrise = response.Sys?.Sunrise != null ? DateTimeOffset.FromUnixTimeSeconds(response.Sys.Sunrise.Value).DateTime : null,
                Sunset = response.Sys?.Sunset != null ? DateTimeOffset.FromUnixTimeSeconds(response.Sys.Sunset.Value).DateTime : null,
                LastUpdated = now,
                ValidUntil = now.AddMinutes(30),
                Source = "OpenWeatherMap"
            };
        }

        private static WeatherForecast MapForecastResponse(OpenWeatherMapForecastResponse response, decimal latitude, decimal longitude)
        {
            var now = DateTime.UtcNow;
            var forecast = new WeatherForecast
            {
                Latitude = latitude,
                Longitude = longitude,
                LastUpdated = now,
                ValidUntil = now.AddHours(3),
                Source = "OpenWeatherMap"
            };

            // Group hourly data by day for daily forecasts
            var dailyGroups = response.List?.GroupBy(item => 
                DateTimeOffset.FromUnixTimeSeconds(item.Dt).Date
            ).Take(5);

            if (dailyGroups != null)
            {
                foreach (var group in dailyGroups)
                {
                    var dayItems = group.ToList();
                    var firstItem = dayItems.First();
                    
                    var daily = new DailyWeather
                    {
                        Date = group.Key,
                        MinTemperature = dayItems.Min(item => item.Main?.TempMin ?? 0),
                        MaxTemperature = dayItems.Max(item => item.Main?.TempMax ?? 0),
                        Condition = firstItem.Weather?.FirstOrDefault()?.Main ?? string.Empty,
                        Description = firstItem.Weather?.FirstOrDefault()?.Description ?? string.Empty,
                        IconCode = firstItem.Weather?.FirstOrDefault()?.Icon ?? string.Empty,
                        WindSpeed = (dayItems.Average(item => item.Wind?.Speed ?? 0)) * 3.6,
                        WindDirection = firstItem.Wind?.Deg ?? 0,
                        ChanceOfRain = (int)Math.Round(dayItems.Average(item => item.Pop ?? 0) * 100),
                        Precipitation = dayItems.Sum(item => item.Rain?.ThreeHour ?? item.Snow?.ThreeHour ?? 0)
                    };
                    
                    forecast.DailyForecasts.Add(daily);
                }
            }

            // Add hourly forecasts
            if (response.List != null)
            {
                foreach (var item in response.List.Take(24)) // Next 24 hours
                {
                    var hourly = new HourlyWeather
                    {
                        DateTime = DateTimeOffset.FromUnixTimeSeconds(item.Dt).DateTime,
                        Temperature = item.Main?.Temp ?? 0,
                        FeelsLike = item.Main?.FeelsLike ?? 0,
                        Condition = item.Weather?.FirstOrDefault()?.Main ?? string.Empty,
                        IconCode = item.Weather?.FirstOrDefault()?.Icon ?? string.Empty,
                        ChanceOfRain = (int)Math.Round((item.Pop ?? 0) * 100),
                        Precipitation = item.Rain?.ThreeHour ?? item.Snow?.ThreeHour ?? 0,
                        WindSpeed = (item.Wind?.Speed ?? 0) * 3.6,
                        WindDirection = item.Wind?.Deg ?? 0,
                        Humidity = item.Main?.Humidity ?? 0,
                        Visibility = (item.Visibility ?? 0) / 1000.0
                    };
                    
                    forecast.HourlyForecasts.Add(hourly);
                }
            }

            return forecast;
        }

        private static DivingWeatherConditions AssessDivingConditions(WeatherInfo weather, WeatherForecast? forecast)
        {
            var conditions = new DivingWeatherConditions
            {
                CurrentWeather = weather,
                LastUpdated = DateTime.UtcNow
            };

            var score = 100; // Start with perfect score and deduct points
            var warnings = new List<string>();
            var recommendations = new List<string>();

            // Wind conditions (most critical for surface conditions)
            if (weather.WindSpeed > 30)
            {
                score -= 40;
                warnings.Add("Vents forts (>30 km/h) - conditions de surface difficiles");
                conditions.IsSafeForDiving = false;
                conditions.IsSafeForSnorkeling = false;
            }
            else if (weather.WindSpeed > 20)
            {
                score -= 20;
                warnings.Add("Vents modérés - prudence en surface");
                recommendations.Add("Vérifiez les conditions locales avant de plonger");
            }
            else if (weather.WindSpeed > 10)
            {
                score -= 10;
                recommendations.Add("Vents légers - bonnes conditions générales");
            }

            // Visibility
            if (weather.Visibility < 5)
            {
                score -= 25;
                warnings.Add("Visibilité réduite - navigation difficile");
            }
            else if (weather.Visibility < 10)
            {
                score -= 10;
                recommendations.Add("Visibilité moyenne - restez près du rivage");
            }

            // Precipitation
            if (weather.ChanceOfRain > 70)
            {
                score -= 20;
                warnings.Add("Forte probabilité de pluie");
                recommendations.Add("Prévoyez un équipement de protection");
            }
            else if (weather.ChanceOfRain > 40)
            {
                score -= 10;
                recommendations.Add("Risque de pluie - surveillez les conditions");
            }

            // Temperature considerations
            if (weather.Temperature < 10)
            {
                score -= 15;
                warnings.Add("Température froide - équipement thermique recommandé");
            }
            else if (weather.Temperature > 35)
            {
                score -= 10;
                recommendations.Add("Température élevée - hydratez-vous bien");
            }

            // Lightning risk (based on conditions)
            if (weather.Condition.ToLower().Contains("thunder") || weather.Condition.ToLower().Contains("storm"))
            {
                score -= 50;
                warnings.Add("DANGER: Risque d'orage - ne pas plonger");
                conditions.IsSafeForDiving = false;
                conditions.IsSafeForSnorkeling = false;
            }

            // UV Index warnings
            if (weather.UvIndex > 7)
            {
                recommendations.Add("Indice UV élevé - protection solaire essentielle");
            }

            // Determine overall safety
            conditions.SafetyScore = Math.Max(0, score);
            conditions.IsSafeForDiving = conditions.IsSafeForDiving && score >= 40;
            conditions.IsSafeForSnorkeling = conditions.IsSafeForSnorkeling && score >= 30;

            // Overall assessment
            conditions.OverallCondition = score switch
            {
                >= 80 => "Excellentes",
                >= 60 => "Bonnes",
                >= 40 => "Correctes",
                >= 20 => "Difficiles",
                _ => "Dangereuses"
            };

            conditions.Assessment = weather.GetDivingConditionsAssessment();
            conditions.Warnings = warnings;
            conditions.Recommendations = recommendations;

            // Best time today (simplified - would need hourly data for accuracy)
            if (forecast?.HourlyForecasts.Any() == true)
            {
                var bestHour = forecast.HourlyForecasts
                    .Where(h => h.DateTime.Date == DateTime.Today)
                    .OrderBy(h => h.WindSpeed)
                    .ThenBy(h => h.ChanceOfRain)
                    .FirstOrDefault();

                if (bestHour != null)
                {
                    conditions.BestTimeToday = $"Meilleure période: {bestHour.DateTime:HH:mm}";
                }
            }

            return conditions;
        }
    }

    // OpenWeatherMap API response models
    internal class OpenWeatherMapCurrentResponse
    {
        public MainData? Main { get; set; }
        public WeatherData[]? Weather { get; set; }
        public WindData? Wind { get; set; }
        public CloudData? Clouds { get; set; }
        public RainData? Rain { get; set; }
        public SnowData? Snow { get; set; }
        public SysData? Sys { get; set; }
        public int? Visibility { get; set; }
        public long Dt { get; set; }
    }

    internal class OpenWeatherMapForecastResponse
    {
        public ForecastItem[]? List { get; set; }
    }

    internal class ForecastItem
    {
        public MainData? Main { get; set; }
        public WeatherData[]? Weather { get; set; }
        public WindData? Wind { get; set; }
        public CloudData? Clouds { get; set; }
        public RainData? Rain { get; set; }
        public SnowData? Snow { get; set; }
        public int? Visibility { get; set; }
        public double? Pop { get; set; } // Probability of precipitation
        public long Dt { get; set; }
    }

    internal class MainData
    {
        public double Temp { get; set; }
        public double FeelsLike { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public int Humidity { get; set; }
        public double Pressure { get; set; }
    }

    internal class WeatherData
    {
        public string Main { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    internal class WindData
    {
        public double Speed { get; set; }
        public int Deg { get; set; }
    }

    internal class CloudData
    {
        public int All { get; set; }
    }

    internal class RainData
    {
        public double OneHour { get; set; }
        public double ThreeHour { get; set; }
    }

    internal class SnowData
    {
        public double OneHour { get; set; }
        public double ThreeHour { get; set; }
    }

    internal class SysData
    {
        public long? Sunrise { get; set; }
        public long? Sunset { get; set; }
    }
}