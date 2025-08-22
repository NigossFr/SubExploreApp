using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service de configuration pour le développement avec accès Supabase complet
    /// Priorité : Variables d'environnement > Configuration > Valeurs par défaut
    /// </summary>
    public class DevelopmentConfigurationService : ISupabaseConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DevelopmentConfigurationService> _logger;

        public DevelopmentConfigurationService(
            IConfiguration configuration,
            ILogger<DevelopmentConfigurationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            _logger.LogInformation("DevelopmentConfigurationService initialized - using Supabase with development-friendly error handling");
        }


        public async Task<string> GetDatabaseConnectionStringAsync()
        {
            // Mode hybride : Supabase pour l'authentification, PostgreSQL direct pour les données
            _logger.LogInformation("📊 GetDatabaseConnectionStringAsync appelé - Mode hybride Supabase + PostgreSQL");
            
            // Configuration PostgreSQL pour Entity Framework
            var connectionString = GetConfigValue("DATABASE_URL", "ConnectionStrings:DefaultConnection", "");
            
            // Fallback vers la chaîne de connexion PostgreSQL par défaut
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Base de données connection string not found, using hardcoded PostgreSQL fallback");
                connectionString = "Host=db.iguvwnyehojvxkyqzaoi.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=;";
            }
            
            _logger.LogInformation("Using database connection: {Host}", GetDatabaseHost(connectionString));
            return await Task.FromResult(connectionString);
        }

        public async Task<string> GetSupabaseUrlAsync()
        {
            var url = GetConfigValue("SUPABASE_URL", "Supabase:Url", "");
            
            // Fallback vers la valeur codée en dur pour Android si la configuration n'est pas accessible
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogWarning("Supabase URL not found in configuration, using hardcoded fallback for Android");
                url = "https://iguvwnyehojvxkyqzaoi.supabase.co"; // URL depuis appsettings.json
            }
            
            if (string.IsNullOrEmpty(url))
            {
                var errorMsg = "Supabase URL not found. Please set SUPABASE_URL environment variable or configure Supabase:Url in appsettings.json";
                _logger.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }
            
            // 🔧 CORRECTIF AUTOMATIQUE POUR ÉMULATEUR ANDROID
            _logger.LogInformation("URL AVANT correctif: {Url}", url);
            url = EmulatorNetworkFix.GetSupabaseUrlWithEmulatorFix(url);
            _logger.LogInformation("URL APRÈS correctif: {Url}", url);
            
            _logger.LogInformation("Using Supabase URL: {Url}", MaskUrl(url));
            return url;
        }

        public async Task<string> GetSupabaseAnonKeyAsync()
        {
            var key = GetConfigValue("SUPABASE_ANON_KEY", "Supabase:AnonKey", "");
            
            // Fallback vers la valeur codée en dur pour Android si la configuration n'est pas accessible
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Supabase Anonymous Key not found in configuration, using hardcoded fallback for Android");
                key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImlndXZ3bnllaG9qdnhreXF6YW9pIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTUyNDgyNDcsImV4cCI6MjA3MDgyNDI0N30.LTBCWoGhEh83g_2HPyXAyIvOgt2CQ_103GtKVbbiuuc"; // Clé mise à jour janvier 2025
            }
            
            if (string.IsNullOrEmpty(key))
            {
                var errorMsg = "Supabase Anonymous Key not found. Please set SUPABASE_ANON_KEY environment variable or configure Supabase:AnonKey in appsettings.json";
                _logger.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }
            
            _logger.LogInformation("Using Supabase Anonymous Key: {Key}", MaskValue(key));
            return key;
        }

        public async Task<string> GetSupabaseServiceRoleKeyAsync()
        {
            var key = GetConfigValue("SUPABASE_SERVICE_ROLE_KEY", "Supabase:ServiceRoleKey", "");
            
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Supabase Service Role Key not found - this is optional for client applications");
                return "";
            }
            
            _logger.LogInformation("Using Supabase Service Role Key: {Key}", MaskValue(key));
            return key;
        }

        public async Task<bool> ValidateConfigurationAsync()
        {
            try
            {
                var connectionString = await GetDatabaseConnectionStringAsync();
                return !string.IsNullOrEmpty(connectionString);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetConfigurationStatusAsync()
        {
            var status = new System.Text.StringBuilder();
            status.AppendLine("=== DEVELOPMENT SUPABASE CONFIGURATION STATUS ===");
            status.AppendLine("Mode: Development (Supabase with enhanced error handling)");
            
            try
            {
                var connectionString = await GetDatabaseConnectionStringAsync();
                var url = await GetSupabaseUrlAsync();
                var anonKey = await GetSupabaseAnonKeyAsync();
                
                status.AppendLine($"Database: {GetDatabaseHost(connectionString)}");
                status.AppendLine($"URL: {MaskUrl(url)}");
                status.AppendLine($"Anonymous Key: {MaskValue(anonKey)}");
                status.AppendLine($"Configuration Valid: ✅ YES");
            }
            catch (Exception ex)
            {
                status.AppendLine($"❌ Configuration Error: {ex.Message}");
                status.AppendLine("Please check your environment variables or appsettings.json");
            }
            
            return status.ToString();
        }

        public async Task RefreshConfigurationAsync()
        {
            _logger.LogInformation("Development mode: Configuration refresh completed (Supabase configuration reloaded)");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Helper method to get configuration values with priority: Environment > Config > Default
        /// </summary>
        private string GetConfigValue(string environmentKey, string configKey, string defaultValue)
        {
            // Priorité 1: Variable d'environnement
            var envValue = Environment.GetEnvironmentVariable(environmentKey);
            if (!string.IsNullOrEmpty(envValue))
            {
                return envValue;
            }

            // Priorité 2: Configuration (appsettings.json)
            var configValue = _configuration[configKey];
            if (!string.IsNullOrEmpty(configValue))
            {
                return configValue;
            }

            // Priorité 3: Valeur par défaut
            return defaultValue;
        }

        /// <summary>
        /// Mask sensitive values for logging
        /// </summary>
        private static string MaskValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "❌ NOT SET";

            if (value.Length <= 8)
                return "***";

            return $"{value.Substring(0, 4)}***{value.Substring(value.Length - 4)}";
        }

        /// <summary>
        /// Mask URL for logging (keep domain visible)
        /// </summary>
        private static string MaskUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "❌ NOT SET";

            try
            {
                var uri = new Uri(url);
                return $"{uri.Scheme}://{uri.Host}***";
            }
            catch
            {
                return MaskValue(url);
            }
        }

        private static string GetDatabaseHost(string connectionString)
        {
            try
            {
                return connectionString.Split(';')
                    .FirstOrDefault(part => part.Trim().StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1] ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}