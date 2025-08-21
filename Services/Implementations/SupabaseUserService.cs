// ========================================
// SERVICE USERS SUPABASE IMPLEMENTATION
// ========================================

using Microsoft.Extensions.Logging;
using Supabase;
using SubExplore.Services.Interfaces;
using SubExplore.Models.Supabase;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Implémentation du service de gestion des utilisateurs via l'API Supabase
    /// </summary>
    public class SupabaseUserService : ISupabaseUserService
    {
        private readonly ISupabaseClientService _supabaseClient;
        private readonly ILogger<SupabaseUserService> _logger;
        private readonly IRetryPolicyService? _retryPolicy;

        public SupabaseUserService(
            ISupabaseClientService supabaseClient,
            ILogger<SupabaseUserService> logger,
            IRetryPolicyService? retryPolicy = null)
        {
            _supabaseClient = supabaseClient;
            _logger = logger;
            _retryPolicy = retryPolicy;
        }

        public async Task<SupabaseUser?> GetUserByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return null;

                var client = await _supabaseClient.GetClientAsync();
                if (client == null) return null;

                var result = await client
                    .From<SupabaseUser>()
                    .Filter("email", Postgrest.Constants.Operator.Equals, email.ToLower())
                    .Single();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération de l'utilisateur par email: {email}");
                return null;
            }
        }

        public async Task<SupabaseUser?> GetUserByIdAsync(Guid userId)
        {
            try
            {
                var client = await _supabaseClient.GetClientAsync();
                if (client == null) return null;

                var result = await client
                    .From<SupabaseUser>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, userId.ToString())
                    .Single();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération de l'utilisateur: {userId}");
                return null;
            }
        }

        public async Task<SupabaseUser?> GetUserByUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return null;

                var client = await _supabaseClient.GetClientAsync();
                if (client == null) return null;

                var result = await client
                    .From<SupabaseUser>()
                    .Filter("username", Postgrest.Constants.Operator.Equals, username.ToLower())
                    .Single();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération de l'utilisateur par nom: {username}");
                return null;
            }
        }

        public async Task<SupabaseUser> CreateUserAsync(SupabaseUser user)
        {
            try
            {
                _logger.LogInformation($"👤 Création de l'utilisateur: {user.Email}");
                
                var client = await _supabaseClient.GetClientAsync();
                if (client == null)
                    throw new InvalidOperationException("Client Supabase non disponible");

                user.Id = Guid.NewGuid();
                user.Email = user.Email.ToLower();
                user.Username = user.Username?.ToLower();
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await client
                    .From<SupabaseUser>()
                    .Insert(user);

                _logger.LogInformation($"✅ Utilisateur créé avec succès: {user.Email}");
                return result.Model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la création de l'utilisateur: {user.Email}");
                throw;
            }
        }

        public async Task<SupabaseUser> UpdateUserAsync(SupabaseUser user)
        {
            try
            {
                var client = await _supabaseClient.GetClientAsync();
                if (client == null)
                    throw new InvalidOperationException("Client Supabase non disponible");

                user.UpdatedAt = DateTime.UtcNow;

                var result = await client
                    .From<SupabaseUser>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, user.Id.ToString())
                    .Update(user);

                _logger.LogInformation($"✅ Utilisateur mis à jour: {user.Email}");
                return result.Model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la mise à jour de l'utilisateur: {user.Email}");
                throw;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(Guid userId)
        {
            try
            {
                var client = await _supabaseClient.GetClientAsync();
                if (client == null) return false;

                var user = await GetUserByIdAsync(userId);
                if (user == null) return false;

                user.LastLogin = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await client
                    .From<SupabaseUser>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, userId.ToString())
                    .Update(user);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la mise à jour de la dernière connexion: {userId}");
                return false;
            }
        }

        public async Task<bool> ConfirmEmailAsync(Guid userId)
        {
            try
            {
                var client = await _supabaseClient.GetClientAsync();
                if (client == null) return false;

                var user = await GetUserByIdAsync(userId);
                if (user == null) return false;

                user.IsEmailConfirmed = true;
                user.UpdatedAt = DateTime.UtcNow;

                await client
                    .From<SupabaseUser>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, userId.ToString())
                    .Update(user);

                _logger.LogInformation($"✅ Email confirmé pour l'utilisateur: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la confirmation de l'email: {userId}");
                return false;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                var user = await GetUserByEmailAsync(email);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la vérification de l'email: {email}");
                return false;
            }
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return false;

                var user = await GetUserByUsernameAsync(username);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la vérification du nom d'utilisateur: {username}");
                return false;
            }
        }

        public async Task<bool> UpdatePasswordAsync(Guid userId, string passwordHash)
        {
            try
            {
                var client = await _supabaseClient.GetClientAsync();
                if (client == null) return false;

                var user = await GetUserByIdAsync(userId);
                if (user == null) return false;

                user.PasswordHash = passwordHash;
                user.UpdatedAt = DateTime.UtcNow;

                await client
                    .From<SupabaseUser>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, userId.ToString())
                    .Update(user);

                _logger.LogInformation($"✅ Mot de passe mis à jour pour l'utilisateur: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la mise à jour du mot de passe: {userId}");
                return false;
            }
        }

        public User ConvertToDomainModel(SupabaseUser supabaseUser)
        {
            // Conversion des enums string vers enums typés
            AccountType accountType = AccountType.Standard;
            if (Enum.TryParse<AccountType>(supabaseUser.AccountType, true, out var parsedAccountType))
            {
                accountType = parsedAccountType;
            }

            SubscriptionStatus subscriptionStatus = SubscriptionStatus.Free;
            if (Enum.TryParse<SubscriptionStatus>(supabaseUser.SubscriptionStatus, true, out var parsedSubscriptionStatus))
            {
                subscriptionStatus = parsedSubscriptionStatus;
            }

            ExpertiseLevel? expertiseLevel = null;
            if (!string.IsNullOrWhiteSpace(supabaseUser.ExpertiseLevel) &&
                Enum.TryParse<ExpertiseLevel>(supabaseUser.ExpertiseLevel, true, out var parsedExpertiseLevel))
            {
                expertiseLevel = parsedExpertiseLevel;
            }

            return new User
            {
                Id = supabaseUser.Id,
                Email = supabaseUser.Email,
                PasswordHash = supabaseUser.PasswordHash,
                Username = supabaseUser.Username,
                FirstName = supabaseUser.FirstName,
                LastName = supabaseUser.LastName,
                AvatarUrl = supabaseUser.AvatarUrl,
                AccountType = accountType,
                SubscriptionStatus = subscriptionStatus,
                ExpertiseLevel = expertiseLevel,
                CreatedAt = supabaseUser.CreatedAt,
                UpdatedAt = supabaseUser.UpdatedAt,
                LastLogin = supabaseUser.LastLogin,
                IsEmailConfirmed = supabaseUser.IsEmailConfirmed,
                ModeratorSpecialization = (ModeratorSpecialization)supabaseUser.ModeratorSpecialization,
                ModeratorStatus = (ModeratorStatus)supabaseUser.ModeratorStatus,
                Permissions = (UserPermissions)supabaseUser.Permissions,
                ModeratorSince = supabaseUser.ModeratorSince,
                OrganizationId = supabaseUser.OrganizationId
            };
        }

        public SupabaseUser ConvertFromDomainModel(User user)
        {
            return new SupabaseUser
            {
                Id = user.Id,
                Email = user.Email,
                PasswordHash = user.PasswordHash,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarUrl = user.AvatarUrl,
                AccountType = user.AccountType.ToString(),
                SubscriptionStatus = user.SubscriptionStatus.ToString(),
                ExpertiseLevel = user.ExpertiseLevel?.ToString(),
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt ?? DateTime.UtcNow,
                LastLogin = user.LastLogin,
                IsEmailConfirmed = user.IsEmailConfirmed,
                ModeratorSpecialization = (int)user.ModeratorSpecialization,
                ModeratorStatus = (int)user.ModeratorStatus,
                Permissions = (int)user.Permissions,
                ModeratorSince = user.ModeratorSince,
                OrganizationId = user.OrganizationId
            };
        }
    }
}