// ========================================
// SERVICE API SUPABASE POUR SUBEXPLORE
// ========================================
// Service pour interagir avec l'API REST Supabase (solution alternative)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Supabase;
using SubExplore.Services.Interfaces;
using SubExplore.Models.Supabase;

namespace SubExplore.Services.Implementations
{
    public class SupabaseApiService : ISupabaseApiService
    {
        private readonly ILogger<SupabaseApiService> _logger;
        private readonly IRetryPolicyService? _retryPolicyService;
        private readonly ICircuitBreakerService? _circuitBreakerService;
        private Client? _client;

        public SupabaseApiService(
            ILogger<SupabaseApiService> logger,
            IRetryPolicyService? retryPolicyService = null,
            ICircuitBreakerService? circuitBreakerService = null)
        {
            _logger = logger;
            _retryPolicyService = retryPolicyService;
            _circuitBreakerService = circuitBreakerService;
        }

        /// <summary>
        /// Initialise le client Supabase
        /// </summary>
        public async Task InitializeAsync(string url, string key)
        {
            try
            {
                _logger.LogInformation("🔧 Initialisation du client Supabase API...");
                
                var options = new SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = false, // Désactivé pour commencer
                };

                _client = new Client(url, key, options);
                await _client.InitializeAsync();
                
                _logger.LogInformation("✅ Client Supabase API initialisé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'initialisation du client Supabase API");
                throw;
            }
        }

        /// <summary>
        /// Test simple de connexion avec resilience
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                _logger.LogInformation("🔍 Test de connexion Supabase API...");
                
                if (_client == null)
                {
                    _logger.LogWarning("⚠️ Client Supabase non initialisé");
                    return false;
                }

                // Test simple : compter les utilisateurs
                var result = await _client.From<SupabaseUser>()
                    .Select("id")
                    .Limit(1)
                    .Get();

                _logger.LogInformation($"✅ Connexion Supabase API réussie - {result.Models.Count} enregistrement(s) trouvé(s)");
                return true;
            });
        }

        /// <summary>
        /// Récupère tous les utilisateurs avec resilience
        /// </summary>
        public async Task<List<SupabaseUser>> GetUsersAsync()
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                if (_client == null)
                    throw new InvalidOperationException("Client Supabase non initialisé");

                _logger.LogInformation("📥 Récupération des utilisateurs...");
                var result = await _client.From<SupabaseUser>().Get();
                
                _logger.LogInformation($"✅ {result.Models.Count} utilisateur(s) récupéré(s)");
                return result.Models;
            });
        }

        /// <summary>
        /// Récupère un utilisateur par email
        /// </summary>
        public async Task<SupabaseUser?> GetUserByEmailAsync(string email)
        {
            try
            {
                if (_client == null)
                    throw new InvalidOperationException("Client Supabase non initialisé");

                _logger.LogInformation("🔍 Recherche de l'utilisateur avec email: {Email}", email);
                
                var result = await _client.From<SupabaseUser>()
                    .Where(u => u.Email == email)
                    .Single();

                if (result != null)
                {
                    _logger.LogInformation("✅ Utilisateur trouvé: {Username}", result.Username);
                }
                else
                {
                    _logger.LogInformation("⚠️ Aucun utilisateur trouvé avec l'email: {Email}", email);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération de l'utilisateur par email: {Email}", email);
                return null;
            }
        }

        /// <summary>
        /// Récupère tous les spot types
        /// </summary>
        public async Task<List<SupabaseSpotType>> GetSpotTypesAsync()
        {
            try
            {
                if (_client == null)
                    throw new InvalidOperationException("Client Supabase non initialisé");

                _logger.LogInformation("📥 Récupération des types de spots...");
                
                var result = await _client.From<SupabaseSpotType>()
                    .Where(st => st.IsActive == true)
                    .Order("name", Postgrest.Constants.Ordering.Ascending)
                    .Get();

                _logger.LogInformation($"✅ {result.Models.Count} type(s) de spot récupéré(s)");
                return result.Models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération des spot types");
                throw;
            }
        }

        /// <summary>
        /// Récupère tous les spots
        /// </summary>
        public async Task<List<SupabaseSpot>> GetSpotsAsync()
        {
            try
            {
                if (_client == null)
                    throw new InvalidOperationException("Client Supabase non initialisé");

                _logger.LogInformation("📥 Récupération des spots...");
                
                var result = await _client.From<SupabaseSpot>()
                    .Order("created_at", Postgrest.Constants.Ordering.Descending)
                    .Limit(100) // Limiter pour le test
                    .Get();

                _logger.LogInformation($"✅ {result.Models.Count} spot(s) récupéré(s)");
                return result.Models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération des spots");
                throw;
            }
        }

        /// <summary>
        /// Crée un nouvel utilisateur
        /// </summary>
        public async Task<SupabaseUser> CreateUserAsync(SupabaseUser user)
        {
            try
            {
                if (_client == null)
                    throw new InvalidOperationException("Client Supabase non initialisé");

                _logger.LogInformation("➕ Création d'un nouvel utilisateur: {Email}", user.Email);
                
                var result = await _client.From<SupabaseUser>()
                    .Insert(user);

                _logger.LogInformation("✅ Utilisateur créé avec succès: {Id}", result.Model.Id);
                return result.Model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la création de l'utilisateur");
                throw;
            }
        }

        /// <summary>
        /// Met à jour un utilisateur
        /// </summary>
        public async Task<SupabaseUser> UpdateUserAsync(SupabaseUser user)
        {
            try
            {
                if (_client == null)
                    throw new InvalidOperationException("Client Supabase non initialisé");

                _logger.LogInformation("✏️ Mise à jour de l'utilisateur: {Id}", user.Id);
                
                var result = await _client.From<SupabaseUser>()
                    .Where(u => u.Id == user.Id)
                    .Update(user);

                _logger.LogInformation("✅ Utilisateur mis à jour avec succès");
                return result.Model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la mise à jour de l'utilisateur");
                throw;
            }
        }

        /// <summary>
        /// Supprime un utilisateur
        /// </summary>
        public async Task DeleteUserAsync(Guid userId)
        {
            try
            {
                if (_client == null)
                    throw new InvalidOperationException("Client Supabase non initialisé");

                _logger.LogInformation("🗑️ Suppression de l'utilisateur: {Id}", userId);
                
                await _client.From<SupabaseUser>()
                    .Where(u => u.Id == userId)
                    .Delete();

                _logger.LogInformation("✅ Utilisateur supprimé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la suppression de l'utilisateur");
                throw;
            }
        }

        /// <summary>
        /// Test complet avec opérations CRUD
        /// </summary>
        public async Task<bool> RunCompleteTestAsync()
        {
            try
            {
                _logger.LogInformation("🧪 Démarrage du test complet API Supabase...");

                // Test 1: Connexion
                if (!await TestConnectionAsync())
                {
                    return false;
                }

                // Test 2: Lecture des utilisateurs
                var users = await GetUsersAsync();
                _logger.LogInformation($"📊 Test lecture: {users.Count} utilisateur(s) trouvé(s)");

                // Test 3: Recherche d'un utilisateur spécifique
                var adminUser = await GetUserByEmailAsync("admin@subexplore.com");
                if (adminUser != null)
                {
                    _logger.LogInformation($"👤 Admin trouvé - Type: {adminUser.AccountType}, Status: {adminUser.SubscriptionStatus}");
                }

                // Test 4: Lecture des spot types
                var spotTypes = await GetSpotTypesAsync();
                _logger.LogInformation($"🏷️ Test spot types: {spotTypes.Count} type(s) trouvé(s)");

                foreach (var spotType in spotTypes.Take(3))
                {
                    _logger.LogInformation($"   - {spotType.Name}: {spotType.Category}");
                }

                // Test 5: Création/Suppression d'un utilisateur test
                var testUser = new SupabaseUser
                {
                    Email = "test-api@subexplore.com",
                    Username = "test_api_user",
                    FirstName = "Test",
                    LastName = "API",
                    AccountType = "Standard",
                    SubscriptionStatus = "Free",
                    ExpertiseLevel = "Beginner",
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("➕ Test de création d'utilisateur...");
                var createdUser = await CreateUserAsync(testUser);
                
                _logger.LogInformation("🗑️ Nettoyage - suppression de l'utilisateur test...");
                await DeleteUserAsync(createdUser.Id);

                _logger.LogInformation("🎉 TOUS LES TESTS API SUPABASE RÉUSSIS !");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ÉCHEC DU TEST COMPLET API SUPABASE");
                return false;
            }
        }

        #region Resilience Helper Methods

        /// <summary>
        /// Executes an operation with resilience patterns (retry + circuit breaker)
        /// </summary>
        private async Task<T> ExecuteWithResilienceAsync<T>(Func<CancellationToken, Task<T>> operation)
        {
            if (_circuitBreakerService != null)
            {
                return await _circuitBreakerService.ExecuteAsync(async (cancellationToken) =>
                {
                    if (_retryPolicyService != null)
                    {
                        return await _retryPolicyService.ExecuteWithRetryAsync(operation, 
                            maxRetries: 3, baseDelay: 1000, maxDelay: 10000, cancellationToken);
                    }
                    else
                    {
                        return await operation(cancellationToken);
                    }
                });
            }
            else if (_retryPolicyService != null)
            {
                return await _retryPolicyService.ExecuteWithRetryAsync(operation, 
                    maxRetries: 3, baseDelay: 1000, maxDelay: 10000);
            }
            else
            {
                // No resilience services available, execute directly
                return await operation(CancellationToken.None);
            }
        }

        /// <summary>
        /// Executes an operation with resilience patterns (void return)
        /// </summary>
        private async Task ExecuteWithResilienceAsync(Func<CancellationToken, Task> operation)
        {
            await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                await operation(cancellationToken);
                return true; // Return dummy value for generic method
            });
        }

        #endregion
    }
}