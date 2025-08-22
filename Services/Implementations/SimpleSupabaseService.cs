// ========================================
// SERVICE SUPABASE SIMPLIFIÉ
// ========================================
// Service 100% API - Plus de mode hybride

using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    public interface ISimpleSupabaseService
    {
        /// <summary>
        /// Initialise l'API une seule fois au démarrage
        /// </summary>
        Task<bool> InitializeAsync();
        
        /// <summary>
        /// Vérifie si l'API est prête
        /// </summary>
        bool IsReady { get; }
        
        /// <summary>
        /// Obtient le service API
        /// </summary>
        ISupabaseApiService GetApiService();
        
        /// <summary>
        /// Status de l'API
        /// </summary>
        string GetStatus();
    }
    
    public class SimpleSupabaseService : ISimpleSupabaseService
    {
        private readonly ISupabaseApiService _apiService;
        private readonly ISupabaseConfigurationService _configService;
        private readonly ILogger<SimpleSupabaseService> _logger;
        
        private bool _isInitialized = false;
        
        public bool IsReady => _isInitialized;
        
        public SimpleSupabaseService(
            ISupabaseApiService apiService,
            ISupabaseConfigurationService configService,
            ILogger<SimpleSupabaseService> logger)
        {
            _apiService = apiService;
            _configService = configService;
            _logger = logger;
        }
        
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
                return true;
                
            try
            {
                _logger.LogInformation("🚀 Initialisation API Supabase...");
                
                // Validate configuration first
                if (!await _configService.ValidateConfigurationAsync())
                {
                    _logger.LogError("❌ Configuration Supabase invalide");
                    return false;
                }

                // Get secure configuration
                // L'initialisation est maintenant automatique via ISupabaseClientService
                var connectionTest = await _apiService.TestConnectionAsync();
                
                if (connectionTest)
                {
                    _isInitialized = true;
                    _logger.LogInformation("✅ API Supabase initialisée et fonctionnelle");
                    return true;
                }
                else
                {
                    _logger.LogError("❌ Test de connexion API échoué");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'initialisation Supabase");
                return false;
            }
        }
        
        public ISupabaseApiService GetApiService()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("API Supabase non initialisée. Appelez InitializeAsync() d'abord.");
                
            return _apiService;
        }
        
        public string GetStatus()
        {
            if (_isInitialized)
            {
                return "✅ API Prête (Configuration sécurisée)";
            }
            return "❌ Non initialisée";
        }
    }
}