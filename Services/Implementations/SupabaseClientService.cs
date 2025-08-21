// ========================================
// SUPABASE CLIENT SERVICE IMPLEMENTATION
// ========================================
// Implémentation du service client Supabase unifié

using Microsoft.Extensions.Logging;
using Supabase;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service client Supabase unifié
    /// Gère l'initialisation et fournit l'accès au client Supabase
    /// </summary>
    public class SupabaseClientService : ISupabaseClientService
    {
        private readonly ISupabaseConfigurationService _configurationService;
        private readonly ILogger<SupabaseClientService> _logger;
        private Client? _client;
        private bool _isInitialized = false;
        
        public bool IsReady => _isInitialized && _client != null;
        
        public SupabaseClientService(
            ISupabaseConfigurationService configurationService,
            ILogger<SupabaseClientService> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }
        
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized && _client != null)
            {
                _logger.LogInformation("✅ Client Supabase déjà initialisé");
                return true;
            }
            
            try
            {
                _logger.LogInformation("🔧 Initialisation du client Supabase...");
                
                var url = await _configurationService.GetSupabaseUrlAsync();
                var anonKey = await _configurationService.GetSupabaseAnonKeyAsync();
                
                var options = new SupabaseOptions
                {
                    AutoConnectRealtime = true,
                    AutoRefreshToken = true
                };
                
                _client = new Client(url, anonKey, options);
                await _client.InitializeAsync();
                
                _isInitialized = true;
                _logger.LogInformation("✅ Client Supabase initialisé avec succès");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'initialisation du client Supabase");
                return false;
            }
        }
        
        public Client GetClient()
        {
            if (_client == null)
            {
                throw new InvalidOperationException("Le client Supabase n'est pas initialisé. Appelez InitializeAsync() d'abord.");
            }
            
            return _client;
        }
        
        public async Task<Client> GetClientAsync()
        {
            if (_client == null)
            {
                var initialized = await InitializeAsync();
                if (!initialized || _client == null)
                {
                    throw new InvalidOperationException("Impossible d'initialiser le client Supabase.");
                }
            }
            
            return _client;
        }
        
        public string GetConnectionStatus()
        {
            if (!_isInitialized)
                return "❌ Non initialisé";
                
            if (_client == null)
                return "❌ Client non disponible";
                
            return "✅ Connecté et prêt";
        }
    }
}
