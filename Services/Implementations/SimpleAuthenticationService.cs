// ========================================
// SIMPLE AUTHENTICATION SERVICE - API SUPABASE ONLY
// ========================================
// Service d'authentification simplifié qui utilise uniquement l'API Supabase

using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using SubExplore.Models.Domain;

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
                    // TODO: Récupérer les informations utilisateur depuis Supabase
                    // Pour le moment, on crée un utilisateur temporaire
                    _currentUser = new User
                    {
                        Id = Guid.NewGuid(),
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
        
        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                _logger.LogInformation("🔑 Tentative de connexion pour: {Email}", email);
                
                var client = _supabaseClient.GetClient();
                var session = await client.Auth.SignIn(email, password);
                
                if (session != null && session.User != null)
                {
                    _logger.LogInformation("✅ Connexion réussie pour: {Email}", email);
                    
                    // Créer un utilisateur temporaire basé sur la session Supabase
                    _currentUser = new User
                    {
                        Id = Guid.NewGuid(), // TODO: Obtenir l'ID réel depuis Supabase
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
        
        public async Task<bool> RegisterAsync(string email, string password, string firstName, string lastName)
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