// ========================================
// ENHANCED AUTHENTICATION SERVICE - 100% API SUPABASE
// ========================================
// Service d'authentification complet utilisant les services Supabase natifs

using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;
using SubExplore.Models.Enums;
using BCrypt.Net;

namespace SubExplore.Services.Implementations
{
    // Interface moved to Services/Interfaces/IEnhancedAuthenticationService.cs

    public class EnhancedAuthenticationService : ISimpleAuthenticationService
    {
        private readonly ISupabaseClientService _supabaseClient;
        private readonly ISupabaseUserService _userService;
        private readonly ILogger<EnhancedAuthenticationService> _logger;
        private readonly ISettingsService _settingsService;
        
        private bool _isInitialized = false;
        private User? _currentUser = null;

        public bool IsAuthenticated => _isInitialized && _currentUser != null;
        public User? CurrentUser => _currentUser;

        public async Task<User?> GetCurrentUserAsync()
        {
            if (!IsAuthenticated)
                return null;

            try
            {
                await RefreshCurrentUserAsync();
                return _currentUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération de l'utilisateur actuel");
                return _currentUser;
            }
        }

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

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                _logger.LogInformation("✅ EnhancedAuthenticationService déjà initialisé");
                return;
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
                        throw new InvalidOperationException("Impossible d'initialiser le client Supabase");
                    }
                }

                // Vérifier s'il y a une session locale sauvegardée
                await TryRestoreSessionAsync();

                _isInitialized = true;
                _logger.LogInformation("✅ EnhancedAuthenticationService initialisé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'initialisation du service d'authentification");
                throw;
            }
        }

        public async Task<bool> LoginSimpleAsync(string email, string password)
        {
            try
            {
                _logger.LogInformation($"🔐 Tentative de connexion pour: {email}");

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Email et mot de passe requis");
                    return false;
                }

                // Récupérer l'utilisateur par email
                var supabaseUser = await _userService.GetUserByEmailAsync(email.ToLower());
                if (supabaseUser == null)
                {
                    _logger.LogWarning($"🚫 Utilisateur non trouvé: {email}");
                    return false;
                }

                // Vérifier le mot de passe
                if (string.IsNullOrWhiteSpace(supabaseUser.PasswordHash) || 
                    !BCrypt.Net.BCrypt.Verify(password, supabaseUser.PasswordHash))
                {
                    _logger.LogWarning($"🚫 Mot de passe incorrect pour: {email}");
                    return false;
                }

                // Mettre à jour la dernière connexion
                await _userService.UpdateLastLoginAsync(supabaseUser.Id);

                // Convertir vers le modèle domain
                _currentUser = _userService.ConvertToDomainModel(supabaseUser);

                // Sauvegarder la session localement
                await SaveSessionAsync(_currentUser);

                _logger.LogInformation($"✅ Connexion réussie pour: {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la connexion: {email}");
                return false;
            }
        }

        public async Task<bool> RegisterSimpleAsync(string email, string password, string firstName, string lastName)
        {
            try
            {
                _logger.LogInformation($"📝 Tentative d'inscription pour: {email}");

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    _logger.LogWarning("Tous les champs sont requis");
                    return false;
                }

                // Vérifier si l'email existe déjà
                if (await _userService.EmailExistsAsync(email))
                {
                    _logger.LogWarning($"Cet email est déjà utilisé: {email}");
                    return false;
                }


                // Hasher le mot de passe
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                // Créer le nouvel utilisateur
                var newSupabaseUser = new Models.Supabase.SupabaseUser
                {
                    Email = email.ToLower(),
                    PasswordHash = passwordHash,
                    Username = null,
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

                _logger.LogInformation($"✅ Inscription réussie pour: {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de l'inscription: {email}");
                return false;
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