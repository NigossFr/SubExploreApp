// ========================================
// SERVICE D'INITIALISATION DE L'APPLICATION
// ========================================
// Service pour initialiser l'application avec l'API Supabase

using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service d'initialisation de l'application
    /// Gère l'initialisation des services critiques comme Supabase
    /// </summary>
    public interface IAppInitializationService
    {
        /// <summary>
        /// Initialise tous les services critiques de l'application
        /// </summary>
        Task<bool> InitializeAsync();
        
        /// <summary>
        /// Vérifie si l'application est prête
        /// </summary>
        bool IsReady { get; }
        
        /// <summary>
        /// Obtient le statut d'initialisation
        /// </summary>
        string GetInitializationStatus();
    }
    
    public class AppInitializationService : IAppInitializationService
    {
        private readonly ISupabaseClientService _supabaseClientService;
        private readonly ILogger<AppInitializationService> _logger;
        private bool _isInitialized = false;
        private readonly List<string> _initializationErrors = new();
        
        public bool IsReady => _isInitialized;
        
        public AppInitializationService(
            ISupabaseClientService supabaseClientService,
            ILogger<AppInitializationService> logger)
        {
            _supabaseClientService = supabaseClientService;
            _logger = logger;
        }
        
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
            {
                _logger.LogInformation("✅ Application déjà initialisée");
                return true;
            }
            
            _initializationErrors.Clear();
            _logger.LogInformation("🚀 Initialisation de l'application SubExplore...");
            
            try
            {
                // 1. Initialiser le client Supabase
                _logger.LogInformation("📡 Initialisation du client Supabase...");
                var supabaseInitialized = await _supabaseClientService.InitializeAsync();
                
                if (!supabaseInitialized)
                {
                    _initializationErrors.Add("Échec de l'initialisation Supabase");
                    _logger.LogError("❌ Échec de l'initialisation du client Supabase");
                }
                else
                {
                    _logger.LogInformation("✅ Client Supabase initialisé avec succès");
                }
                
                // 2. Vérifier le statut de la connexion
                _logger.LogInformation("🔍 Vérification du statut de la connexion à l'API Supabase...");
                var connectionTest = _supabaseClientService.IsReady;
                
                if (!connectionTest)
                {
                    _initializationErrors.Add("Test de connexion Supabase échoué");
                    _logger.LogWarning("⚠️ Test de connexion Supabase échoué, mais continuons...");
                }
                else
                {
                    _logger.LogInformation("✅ Connexion à l'API Supabase confirmée");
                }
                
                // 3. Finaliser l'initialisation
                if (_initializationErrors.Count == 0)
                {
                    _isInitialized = true;
                    _logger.LogInformation("🎉 Application SubExplore initialisée avec succès !");
                    _logger.LogInformation("   Mode: API Supabase uniquement");
                    _logger.LogInformation("   Status: {Status}", _supabaseClientService.GetConnectionStatus());
                    return true;
                }
                else
                {
                    _logger.LogWarning("⚠️ Application initialisée avec {Count} erreur(s)", _initializationErrors.Count);
                    foreach (var error in _initializationErrors)
                    {
                        _logger.LogWarning("   - {Error}", error);
                    }
                    
                    // Pour le développement, continuons même avec des erreurs
                    _isInitialized = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur critique lors de l'initialisation de l'application");
                _initializationErrors.Add($"Erreur critique: {ex.Message}");
                return false;
            }
        }
        
        public string GetInitializationStatus()
        {
            if (!_isInitialized)
            {
                return "❌ Non initialisée";
            }
            
            if (_initializationErrors.Count == 0)
            {
                return "✅ Initialisée avec succès";
            }
            
            return $"⚠️ Initialisée avec {_initializationErrors.Count} erreur(s)";
        }
    }
}