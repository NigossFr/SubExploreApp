using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Configuration;
using SubExplore.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Secure configuration service implementation with environment-based credential management
    /// </summary>
    public class SecureConfigurationService : ISecureConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ISecureSettingsService _secureSettings;
        private readonly ILogger<SecureConfigurationService> _logger;
        
        private JwtConfiguration? _jwtConfig;
        private readonly object _lockObject = new object();

        public SecureConfigurationService(
            IConfiguration configuration,
            ISecureSettingsService secureSettings,
            ILogger<SecureConfigurationService> logger)
        {
            _configuration = configuration;
            _secureSettings = secureSettings;
            _logger = logger;
        }

        public async Task<JwtConfiguration> GetJwtConfigurationAsync()
        {
            if (_jwtConfig == null)
            {
                lock (_lockObject)
                {
                    if (_jwtConfig == null)
                    {
                        _jwtConfig = LoadJwtConfiguration().GetAwaiter().GetResult();
                    }
                }
            }

            return _jwtConfig;
        }

        public async Task<string> GetDatabaseConnectionStringAsync()
        {
            try
            {
                // Try to get from environment variables first (most secure)
                var connectionString = await GetConnectionStringFromEnvironment();
                if (!string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogDebug("Using database connection string from environment variables");
                    return connectionString;
                }

                // Fallback to configuration file with platform detection
                connectionString = GetPlatformSpecificConnectionString();
                if (!string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogDebug("Using database connection string from configuration file");
                    return connectionString;
                }

                throw new InvalidOperationException("No valid database connection string found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving database connection string");
                throw;
            }
        }

        public async Task<T> GetConfigurationAsync<T>(string sectionName) where T : class, new()
        {
            try
            {
                var config = new T();
                _configuration.GetSection(sectionName).Bind(config);
                
                // Validate if the type implements validation
                if (config is IValidatableObject validatable)
                {
                    var context = new ValidationContext(config);
                    var results = validatable.Validate(context);
                    if (results.Any())
                    {
                        var errors = string.Join(", ", results.Select(r => r.ErrorMessage));
                        throw new InvalidOperationException($"Configuration validation failed for {sectionName}: {errors}");
                    }
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration section: {SectionName}", sectionName);
                throw;
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing secure configuration service");

                // Validate and load JWT configuration
                var jwtConfig = await GetJwtConfigurationAsync();
                var (isValid, errors) = jwtConfig.Validate();
                
                if (!isValid)
                {
                    _logger.LogError("JWT configuration validation failed: {Errors}", string.Join(", ", errors));
                    throw new InvalidOperationException($"JWT configuration is invalid: {string.Join(", ", errors)}");
                }

                // Validate database connection
                var connectionString = await GetDatabaseConnectionStringAsync();
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Database connection string is not configured");
                }

                _logger.LogInformation("Secure configuration service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize secure configuration service");
                throw;
            }
        }

        public async Task<(bool IsValid, List<string> Errors)> ValidateConfigurationAsync()
        {
            var errors = new List<string>();

            try
            {
                // Validate JWT configuration
                var jwtConfig = await GetJwtConfigurationAsync();
                var (jwtValid, jwtErrors) = jwtConfig.Validate();
                if (!jwtValid)
                {
                    errors.AddRange(jwtErrors);
                }

                // Validate database connection
                try
                {
                    var connectionString = await GetDatabaseConnectionStringAsync();
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        errors.Add("Database connection string is not configured");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Database connection validation failed: {ex.Message}");
                }

                // Additional security checks
                if (jwtConfig.AccessTokenExpirationMinutes > 60)
                {
                    errors.Add("Access token expiration exceeds recommended 1 hour limit");
                }

                if (jwtConfig.RefreshTokenExpirationDays > 30)
                {
                    errors.Add("Refresh token expiration exceeds recommended 30-day limit");
                }

                return (errors.Count == 0, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating configuration");
                errors.Add($"Configuration validation error: {ex.Message}");
                return (false, errors);
            }
        }

        public async Task<string> GenerateAndStoreJwtSecretAsync()
        {
            try
            {
                // Generate a cryptographically secure 512-bit key
                using var rng = RandomNumberGenerator.Create();
                var keyBytes = new byte[64]; // 512 bits
                rng.GetBytes(keyBytes);
                var secretKey = Convert.ToBase64String(keyBytes);

                // Store securely
                await _secureSettings.SetSecureAsync("JwtSecretKey", secretKey);
                await _secureSettings.SetSecureAsync("JwtSecretCreatedAt", DateTime.UtcNow);

                _logger.LogInformation("New JWT secret key generated and stored securely");
                
                // Clear cached config to force reload
                _jwtConfig = null;
                
                return secretKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT secret key");
                throw;
            }
        }

        public async Task<bool> RotateJwtSecretAsync()
        {
            try
            {
                _logger.LogInformation("Starting JWT secret rotation");

                // Store the old key for grace period (optional - for token migration)
                var currentConfig = await GetJwtConfigurationAsync();
                await _secureSettings.SetSecureAsync("JwtSecretKeyPrevious", currentConfig.SecretKey);
                await _secureSettings.SetSecureAsync("JwtSecretRotatedAt", DateTime.UtcNow);

                // Generate new key
                await GenerateAndStoreJwtSecretAsync();

                _logger.LogInformation("JWT secret rotation completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT secret rotation failed");
                return false;
            }
        }

        private async Task<JwtConfiguration> LoadJwtConfiguration()
        {
            try
            {
                var config = new JwtConfiguration();

                // Load from configuration file first
                _configuration.GetSection("Jwt").Bind(config);

                // Override with secure storage if available
                var secureKey = await _secureSettings.GetSecureAsync<string>("JwtSecretKey");
                if (!string.IsNullOrEmpty(secureKey))
                {
                    config.SecretKey = secureKey;
                }
                else if (string.IsNullOrEmpty(config.SecretKey))
                {
                    // Generate a new secret key if none exists
                    _logger.LogWarning("No JWT secret key found, generating new one");
                    config.SecretKey = await GenerateAndStoreJwtSecretAsync();
                }

                // Environment variable overrides (highest priority)
                var envSecret = Environment.GetEnvironmentVariable("SUBEXPLORE_JWT_SECRET");
                if (!string.IsNullOrEmpty(envSecret))
                {
                    config.SecretKey = envSecret;
                    _logger.LogDebug("Using JWT secret from environment variable");
                }

                var envIssuer = Environment.GetEnvironmentVariable("SUBEXPLORE_JWT_ISSUER");
                if (!string.IsNullOrEmpty(envIssuer))
                {
                    config.Issuer = envIssuer;
                }

                var envAudience = Environment.GetEnvironmentVariable("SUBEXPLORE_JWT_AUDIENCE");
                if (!string.IsNullOrEmpty(envAudience))
                {
                    config.Audience = envAudience;
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading JWT configuration");
                throw;
            }
        }

        private async Task<string?> GetConnectionStringFromEnvironment()
        {
            try
            {
                var host = Environment.GetEnvironmentVariable("SUBEXPLORE_DB_HOST");
                var database = Environment.GetEnvironmentVariable("SUBEXPLORE_DB_NAME");
                var user = Environment.GetEnvironmentVariable("SUBEXPLORE_DB_USER");
                var password = Environment.GetEnvironmentVariable("SUBEXPLORE_DB_PASSWORD");
                var port = Environment.GetEnvironmentVariable("SUBEXPLORE_DB_PORT") ?? "3306";

                if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(database) && 
                    !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
                {
                    return $"server={host};port={port};database={database};user={user};password={password};CharSet=utf8mb4;";
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading database credentials from environment");
                return null;
            }
        }

        private string GetPlatformSpecificConnectionString()
        {
            string connectionStringKey = "DefaultConnection";

#if ANDROID
            if (Microsoft.Maui.Devices.DeviceInfo.Current.DeviceType == Microsoft.Maui.Devices.DeviceType.Virtual)
            {
                connectionStringKey = "AndroidEmulatorConnection";
            }
            else
            {
                connectionStringKey = "AndroidDeviceConnection";
            }
#elif IOS
            if (Microsoft.Maui.Devices.DeviceInfo.Current.DeviceType == Microsoft.Maui.Devices.DeviceType.Virtual)
            {
                connectionStringKey = "iOSSimulatorConnection";
            }
            else
            {
                connectionStringKey = "iOSDeviceConnection";
            }
#elif WINDOWS
            connectionStringKey = "DefaultConnection";
#endif

            var connectionString = _configuration.GetConnectionString(connectionStringKey);
            
            // Fallback to default if platform-specific not found
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = _configuration.GetConnectionString("DefaultConnection");
            }

            return connectionString ?? string.Empty;
        }
    }
}