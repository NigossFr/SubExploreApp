using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service de configuration pour les tests offline sans Supabase
    /// Permet de tester l'application sans connexion Supabase valide
    /// </summary>
    public class OfflineTestConfigurationService : ISupabaseConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OfflineTestConfigurationService> _logger;

        public OfflineTestConfigurationService(
            IConfiguration configuration,
            ILogger<OfflineTestConfigurationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            _logger.LogInformation("⚠️ OfflineTestConfigurationService initialized - OFFLINE MODE for testing without Supabase");
        }

        public async Task<string> GetDatabaseConnectionStringAsync()
        {
            _logger.LogInformation("📊 GetDatabaseConnectionStringAsync appelé - Mode OFFLINE");
            
            // Retourner une chaîne de connexion vide pour forcer le mode offline
            return await Task.FromResult("");
        }

        public async Task<string> GetSupabaseUrlAsync()
        {
            _logger.LogWarning("🔧 Mode OFFLINE: Retour d'une URL Supabase de test");
            // URL de test qui ne fonctionnera pas mais permettra à l'app de démarrer
            return await Task.FromResult("https://offline-test-mode.supabase.co");
        }

        public async Task<string> GetSupabaseAnonKeyAsync()
        {
            _logger.LogWarning("🔧 Mode OFFLINE: Retour d'une clé anonyme de test");
            // Clé de test qui ne fonctionnera pas mais permettra à l'app de démarrer
            return await Task.FromResult("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test-offline-mode.offline-test");
        }

        public async Task<string> GetSupabaseServiceRoleKeyAsync()
        {
            _logger.LogWarning("🔧 Mode OFFLINE: Pas de clé Service Role en mode test");
            return await Task.FromResult("");
        }

        public async Task<bool> ValidateConfigurationAsync()
        {
            _logger.LogWarning("⚠️ Mode OFFLINE: Configuration invalide par design");
            return await Task.FromResult(false);
        }

        public async Task<string> GetConfigurationStatusAsync()
        {
            var status = new System.Text.StringBuilder();
            status.AppendLine("=== OFFLINE TEST CONFIGURATION STATUS ===");
            status.AppendLine("Mode: OFFLINE TEST (No Supabase connection)");
            status.AppendLine("⚠️ Cette configuration est UNIQUEMENT pour les tests offline");
            status.AppendLine("Pour une connexion Supabase réelle, vous devez :");
            status.AppendLine("1. Créer un projet Supabase sur https://supabase.com");
            status.AppendLine("2. Obtenir votre URL et clé API depuis Project Settings > API");
            status.AppendLine("3. Remplacer les valeurs dans appsettings.json");
            status.AppendLine();
            status.AppendLine("Status actuel :");
            status.AppendLine("Database: ❌ OFFLINE MODE");
            status.AppendLine("URL: 🔧 TEST MODE");
            status.AppendLine("Anonymous Key: 🔧 TEST MODE");
            status.AppendLine("Configuration Valid: ❌ NO (by design)");
            
            return await Task.FromResult(status.ToString());
        }

        public async Task RefreshConfigurationAsync()
        {
            _logger.LogInformation("🔧 Mode OFFLINE: Refresh configuration (no-op)");
            await Task.CompletedTask;
        }
    }
}