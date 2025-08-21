// ========================================
// SECURE SUPABASE CONFIGURATION SERVICE
// ========================================
// Implementation for secure Supabase configuration management

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using System.Text;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Secure configuration service for Supabase credentials
    /// Prioritizes environment variables over configuration files
    /// </summary>
    public class SupabaseConfigurationService : ISupabaseConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SupabaseConfigurationService> _logger;
        private readonly Dictionary<string, string?> _configCache;
        private readonly object _lockObject = new object();

        public SupabaseConfigurationService(
            IConfiguration configuration,
            ILogger<SupabaseConfigurationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _configCache = new Dictionary<string, string?>();
        }

        /// <summary>
        /// Gets the Supabase API URL from secure configuration
        /// Priority: Environment Variable > Configuration File > Default
        /// </summary>
        public async Task<string> GetSupabaseUrlAsync()
        {
            return await GetSecureConfigValueAsync(
                environmentKey: "SUPABASE_URL",
                configKey: "Supabase:Url",
                defaultValue: string.Empty,
                isSecret: false);
        }

        /// <summary>
        /// Gets the Supabase anonymous key from secure configuration
        /// </summary>
        public async Task<string> GetSupabaseAnonKeyAsync()
        {
            return await GetSecureConfigValueAsync(
                environmentKey: "SUPABASE_ANON_KEY",
                configKey: "Supabase:AnonKey",
                defaultValue: string.Empty,
                isSecret: true);
        }

        /// <summary>
        /// Gets the Supabase service role key from secure configuration
        /// </summary>
        public async Task<string> GetSupabaseServiceRoleKeyAsync()
        {
            return await GetSecureConfigValueAsync(
                environmentKey: "SUPABASE_SERVICE_ROLE_KEY",
                configKey: "Supabase:ServiceRoleKey",
                defaultValue: string.Empty,
                isSecret: true);
        }

        /// <summary>
        /// Gets the complete database connection string from secure configuration
        /// Builds connection string from individual components for better security
        /// </summary>
        public async Task<string> GetDatabaseConnectionStringAsync()
        {
            try
            {
                // Try to get complete connection string first
                var connectionString = await GetSecureConfigValueAsync(
                    environmentKey: "SUPABASE_CONNECTION_STRING",
                    configKey: "ConnectionStrings:SupabaseConnection",
                    defaultValue: string.Empty,
                    isSecret: true);

                if (!string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogInformation("Using complete connection string from configuration");
                    return connectionString;
                }

                // Build connection string from components
                var host = await GetSecureConfigValueAsync("SUPABASE_DB_HOST", "Database:Host", "db.iguvwnyehojvxkyqzaoi.supabase.co", false);
                var port = await GetSecureConfigValueAsync("SUPABASE_DB_PORT", "Database:Port", "5432", false);
                var database = await GetSecureConfigValueAsync("SUPABASE_DB_NAME", "Database:Name", "postgres", false);
                var user = await GetSecureConfigValueAsync("SUPABASE_DB_USER", "Database:User", "postgres", false);
                var password = await GetSecureConfigValueAsync("SUPABASE_DB_PASSWORD", "Database:Password", string.Empty, true);

                if (string.IsNullOrEmpty(password))
                {
                    _logger.LogError("Database password not found in secure configuration");
                    throw new InvalidOperationException("Database password not configured");
                }

                // Get optional connection parameters
                var timeout = await GetSecureConfigValueAsync("SUPABASE_DB_TIMEOUT", "Database:Timeout", "30", false);
                var commandTimeout = await GetSecureConfigValueAsync("SUPABASE_DB_COMMAND_TIMEOUT", "Database:CommandTimeout", "30", false);
                var idleLifetime = await GetSecureConfigValueAsync("SUPABASE_DB_CONNECTION_IDLE_LIFETIME", "Database:IdleLifetime", "300", false);
                var sslMode = await GetSecureConfigValueAsync("SUPABASE_SSL_MODE", "Database:SslMode", "Require", false);
                var trustServerCert = await GetSecureConfigValueAsync("SUPABASE_TRUST_SERVER_CERTIFICATE", "Database:TrustServerCertificate", "true", false);

                var builder = new StringBuilder();
                builder.Append($"Server={host};");
                builder.Append($"Port={port};");
                builder.Append($"Database={database};");
                builder.Append($"User Id={user};");
                builder.Append($"Password={password};");
                builder.Append($"SSL Mode={sslMode};");
                builder.Append($"Trust Server Certificate={trustServerCert};");
                builder.Append($"Timeout={timeout};");
                builder.Append($"Command Timeout={commandTimeout};");
                builder.Append($"Connection Idle Lifetime={idleLifetime};");

                _logger.LogInformation("Built connection string from individual components");
                return builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build database connection string");
                throw;
            }
        }

        /// <summary>
        /// Validates that all required Supabase configuration is present
        /// </summary>
        public async Task<bool> ValidateConfigurationAsync()
        {
            try
            {
                _logger.LogInformation("Validating Supabase configuration...");

                var url = await GetSupabaseUrlAsync();
                var anonKey = await GetSupabaseAnonKeyAsync();
                var connectionString = await GetDatabaseConnectionStringAsync();

                var isValid = !string.IsNullOrEmpty(url) && 
                             !string.IsNullOrEmpty(anonKey) && 
                             !string.IsNullOrEmpty(connectionString);

                if (isValid)
                {
                    _logger.LogInformation("✅ Supabase configuration validation successful");
                }
                else
                {
                    _logger.LogError("❌ Supabase configuration validation failed - missing required values");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error validating Supabase configuration");
                return false;
            }
        }

        /// <summary>
        /// Gets configuration status for diagnostic purposes
        /// Masks sensitive values for security
        /// </summary>
        public async Task<string> GetConfigurationStatusAsync()
        {
            var status = new StringBuilder();
            status.AppendLine("=== SUPABASE CONFIGURATION STATUS ===");

            try
            {
                var url = await GetSupabaseUrlAsync();
                var anonKey = await GetSupabaseAnonKeyAsync();
                var serviceKey = await GetSupabaseServiceRoleKeyAsync();
                var connectionString = await GetDatabaseConnectionStringAsync();

                status.AppendLine($"URL: {MaskValue(url, false)}");
                status.AppendLine($"Anon Key: {MaskValue(anonKey, true)}");
                status.AppendLine($"Service Key: {MaskValue(serviceKey, true)}");
                status.AppendLine($"Connection String: {MaskConnectionString(connectionString)}");

                var isValid = await ValidateConfigurationAsync();
                status.AppendLine($"Configuration Valid: {(isValid ? "✅ YES" : "❌ NO")}");
            }
            catch (Exception ex)
            {
                status.AppendLine($"❌ Error getting configuration status: {ex.Message}");
            }

            return status.ToString();
        }

        /// <summary>
        /// Refreshes configuration from environment variables
        /// </summary>
        public async Task RefreshConfigurationAsync()
        {
            lock (_lockObject)
            {
                _configCache.Clear();
            }

            _logger.LogInformation("Configuration cache refreshed");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets a secure configuration value with priority: Environment > Config > Default
        /// </summary>
        private async Task<string> GetSecureConfigValueAsync(
            string environmentKey,
            string configKey,
            string defaultValue,
            bool isSecret)
        {
            // Check cache first
            lock (_lockObject)
            {
                if (_configCache.TryGetValue(environmentKey, out var cachedValue))
                {
                    return cachedValue ?? defaultValue;
                }
            }

            // Priority 1: Environment Variable
            var envValue = Environment.GetEnvironmentVariable(environmentKey);
            if (!string.IsNullOrEmpty(envValue))
            {
                lock (_lockObject)
                {
                    _configCache[environmentKey] = envValue;
                }

                if (!isSecret)
                {
                    _logger.LogInformation("Using environment variable for {Key}", environmentKey);
                }
                return envValue;
            }

            // Priority 2: Configuration File
            var configValue = _configuration[configKey];
            if (!string.IsNullOrEmpty(configValue))
            {
                lock (_lockObject)
                {
                    _configCache[environmentKey] = configValue;
                }

                if (!isSecret)
                {
                    _logger.LogInformation("Using configuration file value for {Key}", configKey);
                }
                return configValue;
            }

            // Priority 3: Default Value
            if (!string.IsNullOrEmpty(defaultValue))
            {
                lock (_lockObject)
                {
                    _configCache[environmentKey] = defaultValue;
                }

                if (!isSecret)
                {
                    _logger.LogWarning("Using default value for {Key}", environmentKey);
                }
                return defaultValue;
            }

            _logger.LogError("No value found for required configuration key: {Key}", environmentKey);
            return string.Empty;
        }

        /// <summary>
        /// Masks sensitive values for logging
        /// </summary>
        private static string MaskValue(string value, bool isSecret)
        {
            if (string.IsNullOrEmpty(value))
                return "❌ NOT SET";

            if (!isSecret)
                return value;

            if (value.Length <= 8)
                return "***";

            return $"{value.Substring(0, 4)}***{value.Substring(value.Length - 4)}";
        }

        /// <summary>
        /// Masks connection string for logging
        /// </summary>
        private static string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "❌ NOT SET";

            // Mask password in connection string
            var masked = connectionString.Replace(
                System.Text.RegularExpressions.Regex.Match(connectionString, @"Password=([^;]+)")?.Groups[1]?.Value ?? "",
                "***");

            return masked;
        }
    }
}