// ========================================
// ENHANCED AUTHENTICATION SERVICE - 100% API SUPABASE
// ========================================
// Service d'authentification complet utilisant les services Supabase natifs

using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using BCrypt.Net;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service d'authentification avancé pour l'API Supabase
    /// Utilise les services natifs Supabase pour toutes les opérations
    /// </summary>
    public interface IEnhancedAuthenticationService
    {
        /// <summary>
        /// Événement déclenché lors du changement d'état d'authentification
        /// </summary>
        event EventHandler<AuthenticationStateChangedEventArgs>? StateChanged;
        
        /// <summary>
        /// Initialise le service d'authentification
        /// </summary>
        Task<bool> InitializeAsync();
        
        /// <summary>
        /// Indique si l'utilisateur est authentifié
        /// </summary>
        bool IsAuthenticated { get; }
        
        /// <summary>
        /// Obtient l'utilisateur actuel
        /// </summary>
        User? CurrentUser { get; }
        
        /// <summary>
        /// Connexion avec email et mot de passe
        /// </summary>
        Task<AuthenticationResult> LoginAsync(string email, string password);
        
        /// <summary>
        /// Inscription d'un nouvel utilisateur
        /// </summary>
        Task<AuthenticationResult> RegisterAsync(string email, string password, string firstName, string lastName, string? username = null);
        
        /// <summary>
        /// Déconnexion
        /// </summary>
        Task LogoutAsync();
        
        /// <summary>
        /// Réinitialisation du mot de passe
        /// </summary>
        Task<bool> RequestPasswordResetAsync(string email);
        
        /// <summary>
        /// Mise à jour du profil utilisateur
        /// </summary>
        Task<bool> UpdateProfileAsync(User user);
        
        /// <summary>
        /// Vérification de l'email
        /// </summary>
        Task<bool> VerifyEmailAsync(Guid userId);
        
        /// <summary>
        /// Rafraîchit les données de l'utilisateur actuel
        /// </summary>
        Task<bool> RefreshCurrentUserAsync();
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
    }

    public class EnhancedAuthenticationService : IEnhancedAuthenticationService
    {
        private readonly ISupabaseClientService _supabaseClient;
        private readonly ISupabaseUserService _userService;
        private readonly ILogger<EnhancedAuthenticationService> _logger;
        private readonly ISettingsService _settingsService;
        
        private bool _isInitialized = false;
        private User? _currentUser = null;

        public event EventHandler<AuthenticationStateChangedEventArgs>? StateChanged;

        public bool IsAuthenticated => _isInitialized && _currentUser != null;
        public User? CurrentUser => _currentUser;

        public EnhancedAuthenticationService(
            ISupabaseClientService supabaseClient,
            ISupabaseUserService userService,
            ILogger<EnhancedAuthenticationService> logger,
            ISettingsService settingsService)
        {
            _supabaseClient = supabaseClient;
            _userService = userService;
            _logger = logger;
            _settingsService = settingsService;
        }

        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
            {
                _logger.LogInformation("✅ EnhancedAuthenticationService déjà initialisé");
                return true;
            }

            try
            {
                _logger.LogInformation("🔐 Initialisation du service d'authentification avancé...");

                // Vérifier que le client Supabase est prêt
                if (!_supabaseClient.IsReady)
                {
                    _logger.LogWarning("⚠️ Client Supabase non initialisé, tentative d'initialisation...");
                    var clientReady = await _supabaseClient.InitializeAsync();
                    if (!clientReady)
                    {
                        _logger.LogError("❌ Impossible d'initialiser le client Supabase");
                        return false;
                    }
                }

                // Vérifier s'il y a une session locale sauvegardée
                await TryRestoreSessionAsync();

                _isInitialized = true;
                _logger.LogInformation("✅ EnhancedAuthenticationService initialisé avec succès");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'initialisation du service d'authentification");
                return false;
            }
        }

        public async Task<AuthenticationResult> LoginAsync(string email, string password)
        {
            try
            {
                _logger.LogInformation($"🔐 Tentative de connexion pour: {email}");

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Email et mot de passe requis"
                    };
                }

                // Récupérer l'utilisateur par email
                var supabaseUser = await _userService.GetUserByEmailAsync(email.ToLower());
                if (supabaseUser == null)
                {
                    _logger.LogWarning($"🚫 Utilisateur non trouvé: {email}");
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Identifiants invalides"
                    };
                }

                // Vérifier le mot de passe
                if (string.IsNullOrWhiteSpace(supabaseUser.PasswordHash) || 
                    !BCrypt.Net.BCrypt.Verify(password, supabaseUser.PasswordHash))
                {
                    _logger.LogWarning($"🚫 Mot de passe incorrect pour: {email}");
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Identifiants invalides"
                    };
                }

                // Mettre à jour la dernière connexion
                await _userService.UpdateLastLoginAsync(supabaseUser.Id);

                // Convertir vers le modèle domain
                _currentUser = _userService.ConvertToDomainModel(supabaseUser);

                // Sauvegarder la session localement
                await SaveSessionAsync(_currentUser);

                // Déclencher l'événement de changement d'état
                StateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs 
                { 
                    IsAuthenticated = true, 
                    User = _currentUser 
                });

                _logger.LogInformation($"✅ Connexion réussie pour: {email}");
                return new AuthenticationResult
                {
                    Success = true,
                    User = _currentUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la connexion: {email}");
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Erreur interne du serveur"
                };
            }
        }

        public async Task<AuthenticationResult> RegisterAsync(string email, string password, string firstName, string lastName, string? username = null)
        {
            try
            {
                _logger.LogInformation($"📝 Tentative d'inscription pour: {email}");

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Tous les champs sont requis"
                    };
                }

                // Vérifier si l'email existe déjà
                if (await _userService.EmailExistsAsync(email))
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Cet email est déjà utilisé"
                    };
                }

                // Vérifier si le nom d'utilisateur existe déjà
                if (!string.IsNullOrWhiteSpace(username) && await _userService.UsernameExistsAsync(username))
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Ce nom d'utilisateur est déjà pris"
                    };
                }

                // Hasher le mot de passe
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                // Créer le nouvel utilisateur
                var newSupabaseUser = new Models.Supabase.SupabaseUser
                {
                    Email = email.ToLower(),
                    PasswordHash = passwordHash,
                    Username = username?.ToLower(),
                    FirstName = firstName,
                    LastName = lastName,
                    AccountType = AccountType.Standard.ToString(),
                    SubscriptionStatus = SubscriptionStatus.Free.ToString(),
                    IsEmailConfirmed = false,
                    Permissions = (int)UserPermissions.CreateSpots
                };

                var createdUser = await _userService.CreateUserAsync(newSupabaseUser);
                _currentUser = _userService.ConvertToDomainModel(createdUser);

                // Sauvegarder la session localement
                await SaveSessionAsync(_currentUser);

                // Déclencher l'événement de changement d'état
                StateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs 
                { 
                    IsAuthenticated = true, 
                    User = _currentUser 
                });

                _logger.LogInformation($"✅ Inscription réussie pour: {email}");
                return new AuthenticationResult
                {
                    Success = true,
                    User = _currentUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de l'inscription: {email}");
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Erreur interne du serveur"
                };
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                _logger.LogInformation("🚪 Déconnexion en cours...");

                _currentUser = null;

                // Effacer la session locale
                await ClearSessionAsync();

                // Déclencher l'événement de changement d'état
                StateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs 
                { 
                    IsAuthenticated = false, 
                    User = null 
                });

                _logger.LogInformation("✅ Déconnexion réussie");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la déconnexion");
            }
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            try
            {
                // TODO: Implémenter l'envoi d'email de réinitialisation
                _logger.LogInformation($"📧 Demande de réinitialisation de mot de passe pour: {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la demande de réinitialisation: {email}");
                return false;
            }
        }

        public async Task<bool> UpdateProfileAsync(User user)
        {
            try
            {
                if (_currentUser?.Id != user.Id)
                {
                    return false;
                }

                var supabaseUser = _userService.ConvertFromDomainModel(user);
                var updatedUser = await _userService.UpdateUserAsync(supabaseUser);
                _currentUser = _userService.ConvertToDomainModel(updatedUser);

                // Mettre à jour la session locale
                await SaveSessionAsync(_currentUser);

                _logger.LogInformation($"✅ Profil mis à jour pour: {user.Email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la mise à jour du profil: {user.Email}");
                return false;
            }
        }

        public async Task<bool> VerifyEmailAsync(Guid userId)
        {
            try
            {
                var result = await _userService.ConfirmEmailAsync(userId);
                if (result && _currentUser?.Id == userId)
                {
                    _currentUser.IsEmailConfirmed = true;
                    await SaveSessionAsync(_currentUser);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la vérification de l'email: {userId}");
                return false;
            }
        }

        public async Task<bool> RefreshCurrentUserAsync()
        {
            try
            {
                if (_currentUser == null)
                    return false;

                var supabaseUser = await _userService.GetUserByIdAsync(_currentUser.Id);
                if (supabaseUser == null)
                    return false;

                _currentUser = _userService.ConvertToDomainModel(supabaseUser);
                await SaveSessionAsync(_currentUser);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du rafraîchissement de l'utilisateur");
                return false;
            }
        }

        private async Task TryRestoreSessionAsync()
        {
            try
            {
                var savedUserId = await _settingsService.GetAsync<string>("CurrentUserId");
                if (string.IsNullOrWhiteSpace(savedUserId) || !Guid.TryParse(savedUserId, out var userId))
                    return;

                var supabaseUser = await _userService.GetUserByIdAsync(userId);
                if (supabaseUser != null)
                {
                    _currentUser = _userService.ConvertToDomainModel(supabaseUser);
                    _logger.LogInformation($"✅ Session restaurée pour: {_currentUser.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la restauration de la session");
            }
        }

        private async Task SaveSessionAsync(User user)
        {
            try
            {
                await _settingsService.SetAsync("CurrentUserId", user.Id.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la sauvegarde de la session");
            }
        }

        private async Task ClearSessionAsync()
        {
            try
            {
                await _settingsService.RemoveAsync("CurrentUserId");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'effacement de la session");
            }
        }
    }
}