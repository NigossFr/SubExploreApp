// ========================================
// SIMPLE AUTHENTICATION SERVICE - API SUPABASE ONLY
// ========================================
// Service d'authentification simplifié qui utilise uniquement l'API Supabase

using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service d'authentification simplifié pour l'API Supabase uniquement
    /// Ne dépend pas des repositories Entity Framework
    /// </summary>
    public class SimpleAuthenticationService : ISimpleAuthenticationService
    {
        private readonly ISupabaseClientService _supabaseClient;
        private readonly ILogger<SimpleAuthenticationService> _logger;
        private bool _isInitialized = false;
        private User? _currentUser = null;
        
        public bool IsAuthenticated => _isInitialized && _currentUser != null;
        public User? CurrentUser => _currentUser;
        
        
        public SimpleAuthenticationService(
            ISupabaseClientService supabaseClient,
            ILogger<SimpleAuthenticationService> logger)
        {
            _supabaseClient = supabaseClient;
            _logger = logger;
        }
        
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                _logger.LogInformation("✅ SimpleAuthenticationService déjà initialisé");
                return;
            }
            
            try
            {
                _logger.LogInformation("🔐 Initialisation du service d'authentification simplifié...");
                
                // Vérifier que le client Supabase est prêt
                if (!_supabaseClient.IsReady)
                {
                    _logger.LogWarning("⚠️ Client Supabase non initialisé, tentative d'initialisation...");
                    var clientReady = await _supabaseClient.InitializeAsync();
                    if (!clientReady)
                    {
                        _logger.LogError("❌ Impossible d'initialiser le client Supabase");
                        return;
                    }
                }
                
                // Vérifier s'il y a une session existante
                var client = _supabaseClient.GetClient();
                if (client.Auth.CurrentSession != null)
                {
                    _logger.LogInformation("✅ Session utilisateur existante trouvée");
                    // ✅ UTILISATION DE L'ID UTILISATEUR SUPABASE RÉEL
                    _currentUser = new User
                    {
                        Id = Guid.Parse(client.Auth.CurrentUser.Id),
                        Email = client.Auth.CurrentUser?.Email ?? "unknown@supabase.com",
                        FirstName = "Supabase",
                        LastName = "User",
                        IsEmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };
                }
                
                _isInitialized = true;
                _logger.LogInformation("✅ Service d'authentification simplifié initialisé avec succès");
                _logger.LogInformation($"   État: {(IsAuthenticated ? "Connecté" : "Non connecté")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'initialisation du service d'authentification");
            }
        }
        
        public async Task<bool> LoginSimpleAsync(string email, string password)
        {
            try
            {
                _logger.LogInformation("🔑 Tentative de connexion pour: {Email}", email);
                
                var client = _supabaseClient.GetClient();
                var session = await client.Auth.SignIn(email, password);
                
                if (session != null && session.User != null)
                {
                    _logger.LogInformation("✅ Connexion réussie pour: {Email}", email);
                    
                    // ✅ UTILISATION DE L'ID UTILISATEUR SUPABASE RÉEL
                    _currentUser = new User
                    {
                        Id = Guid.Parse(session.User.Id),
                        Email = session.User.Email ?? email,
                        FirstName = "Supabase",
                        LastName = "User",
                        IsEmailConfirmed = session.User.EmailConfirmedAt != null,
                        CreatedAt = session.User.CreatedAt
                    };
                    
                    return true;
                }
                else
                {
                    _logger.LogWarning("❌ Connexion échouée pour: {Email}", email);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la connexion pour: {Email}", email);
                return false;
            }
        }
        
        public async Task LogoutAsync()
        {
            try
            {
                _logger.LogInformation("🚪 Déconnexion de l'utilisateur...");
                
                var client = _supabaseClient.GetClient();
                await client.Auth.SignOut();
                
                _currentUser = null;
                _logger.LogInformation("✅ Déconnexion réussie");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la déconnexion");
            }
        }
        
        /// <summary>
        /// Obtient l'utilisateur actuel de manière asynchrone
        /// </summary>
        /// <returns>L'utilisateur connecté ou null si non connecté</returns>
        public async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                // S'assurer que le service est initialisé
                if (!_isInitialized)
                {
                    await InitializeAsync();
                }
                
                // Retourner l'utilisateur actuel
                return _currentUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération de l'utilisateur actuel");
                return null;
            }
        }
        
        public async Task<bool> RegisterSimpleAsync(string email, string password, string firstName, string lastName)
        {
            try
            {
                _logger.LogInformation("📝 Tentative d'inscription pour: {Email}", email);
                
                var client = _supabaseClient.GetClient();
                var session = await client.Auth.SignUp(email, password);
                
                if (session != null && session.User != null)
                {
                    _logger.LogInformation("✅ Inscription réussie pour: {Email}", email);
                    
                    // TODO: Ajouter les informations utilisateur dans une table Supabase
                    // Pour le moment, on crée juste l'authentification
                    
                    return true;
                }
                else
                {
                    _logger.LogWarning("❌ Inscription échouée pour: {Email}", email);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'inscription pour: {Email}", email);
                return false;
            }
        }
    }
}