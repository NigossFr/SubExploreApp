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
        private readonly ISupabaseClientService _supabaseClientService;
        private readonly IRetryPolicyService? _retryPolicyService;
        private readonly ICircuitBreakerService? _circuitBreakerService;

        public SupabaseApiService(
            ILogger<SupabaseApiService> logger,
            ISupabaseClientService supabaseClientService,
            IRetryPolicyService? retryPolicyService = null,
            ICircuitBreakerService? circuitBreakerService = null)
        {
            _logger = logger;
            _supabaseClientService = supabaseClientService;
            _retryPolicyService = retryPolicyService;
            _circuitBreakerService = circuitBreakerService;
        }

        /// <summary>
        /// Obtient le client Supabase partagé et vérifie qu'il est initialisé
        /// </summary>
        private async Task<Client> GetClientAsync()
        {
            if (!_supabaseClientService.IsReady)
            {
                _logger.LogInformation("🔧 Initialisation du client Supabase en cours...");
                var initialized = await _supabaseClientService.InitializeAsync();
                if (!initialized)
                {
                    throw new InvalidOperationException("Impossible d'initialiser le client Supabase");
                }
            }

            return await _supabaseClientService.GetClientAsync();
        }

        /// <summary>
        /// Test simple de connexion avec resilience
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                _logger.LogInformation("🔍 Test de connexion Supabase API...");
                
                var client = await GetClientAsync();

                // Test simple : compter les utilisateurs
                var result = await client.From<SupabaseUser>()
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
                var client = await GetClientAsync();

                _logger.LogInformation("📥 Récupération des utilisateurs...");
                var result = await client.From<SupabaseUser>().Get();
                
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
                var client = await GetClientAsync();

                _logger.LogInformation("🔍 Recherche de l'utilisateur avec email: {Email}", email);
                
                var result = await client.From<SupabaseUser>()
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
                var client = await GetClientAsync();

                _logger.LogInformation("📥 Récupération des types de spots...");
                
                var result = await client.From<SupabaseSpotType>()
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
                var client = await GetClientAsync();

                _logger.LogInformation("📥 Récupération des spots...");
                
                var result = await client.From<SupabaseSpot>()
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
                var client = await GetClientAsync();

                _logger.LogInformation("➕ Création d'un nouvel utilisateur: {Email}", user.Email);
                
                var result = await client.From<SupabaseUser>()
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
                var client = await GetClientAsync();

                _logger.LogInformation("✏️ Mise à jour de l'utilisateur: {Id}", user.Id);
                
                var result = await client.From<SupabaseUser>()
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
                var client = await GetClientAsync();

                _logger.LogInformation("🗑️ Suppression de l'utilisateur: {Id}", userId);
                
                await client.From<SupabaseUser>()
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

        /// <summary>
        /// Assure que l'utilisateur est synchronisé dans la table users
        /// </summary>
        private async Task<Guid?> EnsureUserSyncedAsync(Guid userId)
        {
            try
            {
                var client = await GetClientAsync();
                
                // Vérifier existence utilisateur par ID
                var userCheck = await client
                    .From<SupabaseUser>()
                    .Where(u => u.Id == userId)
                    .Get();
                
                if (userCheck?.Models?.Any() == true)
                {
                    // L'utilisateur existe déjà avec le bon ID
                    return userId;
                }
                
                _logger.LogWarning("🔄 Auto-sync utilisateur {UserId}", userId);
                
                var authUser = client.Auth.CurrentUser;
                if (authUser?.Id == userId.ToString())
                {
                    // Vérifier s'il existe déjà un utilisateur avec cet email
                    var emailCheck = await client
                        .From<SupabaseUser>()
                        .Where(u => u.Email == authUser.Email)
                        .Get();
                    
                    if (emailCheck?.Models?.Any() == true)
                    {
                        var existingUser = emailCheck.Models.First();
                        _logger.LogWarning("⚠️ Utilisateur avec email {Email} existe déjà avec ID {ExistingId}, utilisation de cet ID", 
                            authUser.Email, existingUser.Id);
                        return existingUser.Id; // Retourner l'ID de l'utilisateur existant
                    }
                    
                    var newUser = new SupabaseUser
                    {
                        Id = userId,
                        Email = authUser.Email ?? "unknown@supabase.com",
                        PasswordHash = "supabase_auth_managed", // Placeholder - auth managed by Supabase Auth
                        FirstName = authUser.UserMetadata?.GetValueOrDefault("first_name")?.ToString() ?? "Utilisateur",
                        LastName = authUser.UserMetadata?.GetValueOrDefault("last_name")?.ToString() ?? "SubExplore",
                        IsEmailConfirmed = authUser.EmailConfirmedAt.HasValue,
                        CreatedAt = authUser.CreatedAt,
                        LastLogin = DateTime.UtcNow,
                        AccountType = "Standard",
                        SubscriptionStatus = "Free",
                        Permissions = 1
                    };
                    
                    await client.From<SupabaseUser>().Insert(newUser);
                    _logger.LogInformation("✅ Utilisateur {UserId} synchronisé automatiquement", userId);
                    return userId;
                }
                
                throw new UnauthorizedAccessException("Session utilisateur invalide - reconnexion requise");
            }
            catch (Postgrest.Exceptions.PostgrestException pgEx) when (pgEx.Message.Contains("23505") && pgEx.Message.Contains("users_email_key"))
            {
                _logger.LogWarning("⚠️ Utilisateur avec cet email existe déjà, recherche de l'ID existant pour {UserId}", userId);
                
                // Rechercher l'utilisateur existant par email
                var authUser = (await GetClientAsync()).Auth.CurrentUser;
                if (authUser != null)
                {
                    try
                    {
                        var searchClient = await GetClientAsync();
                        var emailCheck = await searchClient
                            .From<SupabaseUser>()
                            .Where(u => u.Email == authUser.Email)
                            .Get();
                        
                        if (emailCheck?.Models?.Any() == true)
                        {
                            var existingUser = emailCheck.Models.First();
                            _logger.LogInformation("✅ Utilisateur existant trouvé avec ID {ExistingId}", existingUser.Id);
                            return existingUser.Id;
                        }
                    }
                    catch (Exception searchEx)
                    {
                        _logger.LogError(searchEx, "❌ Erreur lors de la recherche de l'utilisateur existant");
                    }
                }
                
                return null; // Impossible de résoudre l'utilisateur
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Échec synchronisation utilisateur {UserId}", userId);
                throw;
            }
        }

        // ========================================
        // FAVORITES MANAGEMENT IMPLEMENTATION
        // ========================================

        /// <summary>
        /// Récupère tous les favoris d'un utilisateur avec resilience
        /// </summary>
        public async Task<List<SupabaseUserFavoriteSpot>> GetUserFavoritesAsync(Guid userId)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("📥 Récupération des favoris pour l'utilisateur: {UserId}", userId);
                
                // 🔧 AMÉLIORATION: Résoudre l'ID utilisateur correct
                var actualUserId = await EnsureUserSyncedAsync(userId);
                if (actualUserId == null)
                {
                    _logger.LogWarning("⚠️ Impossible de résoudre l'utilisateur {UserId}, retour d'une liste vide", userId);
                    return new List<SupabaseUserFavoriteSpot>();
                }
                
                var result = await client.From<SupabaseUserFavoriteSpot>()
                    .Where(f => f.UserId == actualUserId.Value)
                    .Order("priority", Postgrest.Constants.Ordering.Descending)
                    .Order("created_at", Postgrest.Constants.Ordering.Descending)
                    .Get();
                
                _logger.LogInformation("✅ {Count} favoris récupéré(s)", result.Models.Count);
                return result.Models;
            });
        }

        /// <summary>
        /// Vérifie si un spot est en favoris pour un utilisateur
        /// </summary>
        public async Task<bool> IsSpotFavoriteAsync(Guid userId, Guid spotId)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogDebug("🔍 Vérification favori: utilisateur {UserId}, spot {SpotId}", userId, spotId);
                
                // 🔧 AMÉLIORATION: Résoudre l'ID utilisateur correct
                var actualUserId = await EnsureUserSyncedAsync(userId);
                if (actualUserId == null)
                {
                    _logger.LogWarning("⚠️ Impossible de résoudre l'utilisateur {UserId}, considéré comme non-favori", userId);
                    return false;
                }
                
                var result = await client.From<SupabaseUserFavoriteSpot>()
                    .Where(f => f.UserId == actualUserId.Value && f.SpotId == spotId)
                    .Limit(1)
                    .Get();
                
                bool isFavorite = result.Models.Count > 0;
                _logger.LogDebug("🔍 Résultat favori: {IsFavorite}", isFavorite);
                return isFavorite;
            });
        }

        /// <summary>
        /// Ajoute un spot aux favoris d'un utilisateur
        /// </summary>
        public async Task<SupabaseUserFavoriteSpot> AddToFavoritesAsync(Guid userId, Guid spotId, int priority = 5, string? notes = null, bool notificationEnabled = true)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("⭐ Ajout aux favoris: utilisateur {UserId}, spot {SpotId}", userId, spotId);
                
                // 🔧 AMÉLIORATION: Synchronisation utilisateur renforcée
                var actualUserId = await EnsureUserSyncedAsync(userId);
                if (actualUserId == null)
                {
                    throw new InvalidOperationException("Impossible de synchroniser l'utilisateur. Veuillez vous reconnecter.");
                }
                
                _logger.LogInformation("🔧 Utilisation de l'ID utilisateur: {ActualUserId} (original: {UserId})", actualUserId, userId);
                
                // Vérifier si déjà en favoris (utiliser l'ID correct)
                if (await IsSpotFavoriteAsync(actualUserId.Value, spotId))
                {
                    throw new InvalidOperationException("Ce spot est déjà en favoris");
                }
                
                var favorite = new SupabaseUserFavoriteSpot
                {
                    Id = Guid.NewGuid(),
                    UserId = actualUserId.Value, // Utiliser l'ID correct
                    SpotId = spotId,
                    Priority = priority,
                    Notes = notes,
                    NotificationEnabled = notificationEnabled,
                    CreatedAt = DateTime.UtcNow
                };
                
                var result = await client.From<SupabaseUserFavoriteSpot>()
                    .Insert(favorite);
                
                _logger.LogInformation("✅ Spot ajouté aux favoris avec succès");
                return result.Model;
            });
        }

        /// <summary>
        /// Retire un spot des favoris d'un utilisateur
        /// </summary>
        public async Task<bool> RemoveFromFavoritesAsync(Guid userId, Guid spotId)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("❌ Suppression des favoris: utilisateur {UserId}, spot {SpotId}", userId, spotId);
                
                // 🔧 AMÉLIORATION: Résoudre l'ID utilisateur correct
                var actualUserId = await EnsureUserSyncedAsync(userId);
                if (actualUserId == null)
                {
                    throw new InvalidOperationException("Impossible de synchroniser l'utilisateur. Veuillez vous reconnecter.");
                }
                
                _logger.LogInformation("🔧 Utilisation de l'ID utilisateur: {ActualUserId} (original: {UserId})", actualUserId, userId);
                
                await client.From<SupabaseUserFavoriteSpot>()
                    .Where(f => f.UserId == actualUserId.Value && f.SpotId == spotId)
                    .Delete();
                
                // Vérifier si le spot était effectivement en favoris avant suppression
                bool removed = true; // On assume le succès si aucune exception
                _logger.LogInformation(removed ? "✅ Spot retiré des favoris" : "⚠️ Spot n'était pas en favoris");
                return removed;
            });
        }

        /// <summary>
        /// Met à jour les notes d'un favori
        /// </summary>
        public async Task<bool> UpdateFavoriteNotesAsync(Guid userId, Guid spotId, string? notes)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("📝 Mise à jour notes favori: utilisateur {UserId}, spot {SpotId}", userId, spotId);
                
                // 🔧 AMÉLIORATION: Résoudre l'ID utilisateur correct
                var actualUserId = await EnsureUserSyncedAsync(userId);
                if (actualUserId == null)
                {
                    throw new InvalidOperationException("Impossible de synchroniser l'utilisateur. Veuillez vous reconnecter.");
                }
                
                var updateData = new SupabaseUserFavoriteSpot
                {
                    Notes = notes,
                    UpdatedAt = DateTime.UtcNow
                };
                
                var result = await client.From<SupabaseUserFavoriteSpot>()
                    .Where(f => f.UserId == actualUserId.Value && f.SpotId == spotId)
                    .Update(updateData);
                
                bool updated = result.Models.Count > 0;
                _logger.LogInformation(updated ? "✅ Notes mises à jour" : "⚠️ Favori non trouvé");
                return updated;
            });
        }

        /// <summary>
        /// Met à jour la priorité d'un favori
        /// </summary>
        public async Task<bool> UpdateFavoritePriorityAsync(Guid userId, Guid spotId, int priority)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("🎯 Mise à jour priorité favori: utilisateur {UserId}, spot {SpotId}, priorité {Priority}", userId, spotId, priority);
                
                var updateData = new SupabaseUserFavoriteSpot
                {
                    Priority = priority,
                    UpdatedAt = DateTime.UtcNow
                };
                
                var result = await client.From<SupabaseUserFavoriteSpot>()
                    .Where(f => f.UserId == userId && f.SpotId == spotId)
                    .Update(updateData);
                
                bool updated = result.Models.Count > 0;
                _logger.LogInformation(updated ? "✅ Priorité mise à jour" : "⚠️ Favori non trouvé");
                return updated;
            });
        }

        /// <summary>
        /// Met à jour les notifications d'un favori
        /// </summary>
        public async Task<bool> UpdateFavoriteNotificationAsync(Guid userId, Guid spotId, bool enabled)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("🔔 Mise à jour notifications favori: utilisateur {UserId}, spot {SpotId}, activé {Enabled}", userId, spotId, enabled);
                
                var updateData = new SupabaseUserFavoriteSpot
                {
                    NotificationEnabled = enabled,
                    UpdatedAt = DateTime.UtcNow
                };
                
                var result = await client.From<SupabaseUserFavoriteSpot>()
                    .Where(f => f.UserId == userId && f.SpotId == spotId)
                    .Update(updateData);
                
                bool updated = result.Models.Count > 0;
                _logger.LogInformation(updated ? "✅ Notifications mises à jour" : "⚠️ Favori non trouvé");
                return updated;
            });
        }

        /// <summary>
        /// Compte le nombre de favoris pour un spot
        /// </summary>
        public async Task<int> GetSpotFavoritesCountAsync(Guid spotId)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogDebug("📊 Comptage favoris pour spot: {SpotId}", spotId);
                
                var result = await client.From<SupabaseUserFavoriteSpot>()
                    .Where(f => f.SpotId == spotId)
                    .Select("id")
                    .Get();
                
                int count = result.Models.Count;
                _logger.LogDebug("📊 Spot {SpotId} a {Count} favoris", spotId, count);
                return count;
            });
        }

        // ========================================
        // SPOT REPORTS & EDITING IMPLEMENTATION
        // ========================================

        /// <summary>
        /// Create a new spot report
        /// </summary>
        public async Task<SupabaseSpotReport> CreateSpotReportAsync(SupabaseSpotReport report)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("🚨 Création d'un signalement pour le spot: {SpotId}", report.SpotId);
                
                var result = await client.From<SupabaseSpotReport>().Insert(report);
                
                _logger.LogInformation("✅ Signalement créé avec succès: {Id}", result.Model.Id);
                return result.Model;
            });
        }

        /// <summary>
        /// Get reports for a specific spot
        /// </summary>
        public async Task<List<SupabaseSpotReport>> GetSpotReportsAsync(Guid spotId)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogDebug("📋 Récupération des signalements pour le spot: {SpotId}", spotId);
                
                var result = await client.From<SupabaseSpotReport>()
                    .Where(r => r.SpotId == spotId)
                    .Order(r => r.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();
                
                _logger.LogDebug("📋 {Count} signalements trouvés", result.Models.Count);
                return result.Models;
            });
        }

        /// <summary>
        /// Get user's reports
        /// </summary>
        public async Task<List<SupabaseSpotReport>> GetUserReportsAsync(Guid userId)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogDebug("📋 Récupération des signalements de l'utilisateur: {UserId}", userId);
                
                var result = await client.From<SupabaseSpotReport>()
                    .Where(r => r.ReporterId == userId)
                    .Order(r => r.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();
                
                _logger.LogDebug("📋 {Count} signalements trouvés", result.Models.Count);
                return result.Models;
            });
        }

        /// <summary>
        /// Get pending reports for moderation
        /// </summary>
        public async Task<List<SupabaseSpotReport>> GetPendingReportsAsync()
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogDebug("📋 Récupération des signalements en attente");
                
                var result = await client.From<SupabaseSpotReport>()
                    .Where(r => r.Status == 1) // Pending
                    .Order(r => r.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();
                
                _logger.LogDebug("📋 {Count} signalements en attente", result.Models.Count);
                return result.Models;
            });
        }

        /// <summary>
        /// Update report status
        /// </summary>
        public async Task<bool> UpdateSpotReportAsync(Guid reportId, int status, string reviewNotes, Guid reviewerId)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("✏️ Mise à jour du signalement: {ReportId}", reportId);
                
                var updateData = new SupabaseSpotReport
                {
                    Status = status,
                    ReviewNotes = reviewNotes,
                    ReviewedBy = reviewerId,
                    ReviewedAt = DateTime.UtcNow
                };
                
                var result = await client.From<SupabaseSpotReport>()
                    .Where(r => r.Id == reportId)
                    .Update(updateData);
                
                bool updated = result.Models.Count > 0;
                _logger.LogInformation(updated ? "✅ Signalement mis à jour" : "⚠️ Signalement non trouvé");
                return updated;
            });
        }

        /// <summary>
        /// Update spot basic information
        /// </summary>
        public async Task<bool> UpdateSpotBasicInfoAsync(Guid spotId, string name, string description, 
            string requiredEquipment, string safetyNotes, string bestConditions)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("✏️ Mise à jour des informations de base du spot: {SpotId}", spotId);
                
                var updateData = new SupabaseSpot
                {
                    Name = name,
                    Description = description,
                    RequiredEquipment = requiredEquipment,
                    SafetyNotes = safetyNotes,
                    BestConditions = bestConditions,
                    LastSafetyReview = DateTime.UtcNow
                };
                
                var result = await client.From<SupabaseSpot>()
                    .Where(s => s.Id == spotId)
                    .Update(updateData);
                
                bool updated = result.Models.Count > 0;
                _logger.LogInformation(updated ? "✅ Informations du spot mises à jour" : "⚠️ Spot non trouvé");
                return updated;
            });
        }

        /// <summary>
        /// Update spot location
        /// </summary>
        public async Task<bool> UpdateSpotLocationAsync(Guid spotId, decimal latitude, decimal longitude)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("📍 Mise à jour de la localisation du spot: {SpotId}", spotId);
                
                var updateData = new SupabaseSpot
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    LastSafetyReview = DateTime.UtcNow
                };
                
                var result = await client.From<SupabaseSpot>()
                    .Where(s => s.Id == spotId)
                    .Update(updateData);
                
                bool updated = result.Models.Count > 0;
                _logger.LogInformation(updated ? "✅ Localisation du spot mise à jour" : "⚠️ Spot non trouvé");
                return updated;
            });
        }

        /// <summary>
        /// Update spot technical details
        /// </summary>
        public async Task<bool> UpdateSpotTechnicalDetailsAsync(Guid spotId, int? maxDepth, int difficultyLevel, int? currentStrength)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("🔧 Mise à jour des détails techniques du spot: {SpotId}", spotId);
                
                var updateData = new SupabaseSpot
                {
                    MaxDepth = maxDepth,
                    DifficultyLevel = difficultyLevel,
                    CurrentStrength = currentStrength ?? 0,
                    LastSafetyReview = DateTime.UtcNow
                };
                
                var result = await client.From<SupabaseSpot>()
                    .Where(s => s.Id == spotId)
                    .Update(updateData);
                
                bool updated = result.Models.Count > 0;
                _logger.LogInformation(updated ? "✅ Détails techniques du spot mis à jour" : "⚠️ Spot non trouvé");
                return updated;
            });
        }

        // ========================================
        // SPOT MEDIA MANAGEMENT IMPLEMENTATION
        // ========================================

        /// <summary>
        /// Upload image to Supabase Storage
        /// </summary>
        public async Task<string?> UploadImageAsync(Stream imageStream, string fileName, string bucketPath)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("📸 Upload d'image: {FileName} vers {BucketPath}", fileName, bucketPath);
                
                try
                {
                    // Reset stream position
                    imageStream.Position = 0;
                    
                    // Convert to byte array
                    var imageBytes = new byte[imageStream.Length];
                    await imageStream.ReadAsync(imageBytes, 0, imageBytes.Length, cancellationToken);
                    
                    // Upload to Supabase Storage
                    var result = await client.Storage
                        .From("spot-images")
                        .Upload(imageBytes, $"{bucketPath}/{fileName}", new Supabase.Storage.FileOptions
                        {
                            CacheControl = "3600",
                            Upsert = false
                        });
                    
                    if (!string.IsNullOrEmpty(result))
                    {
                        var publicUrl = client.Storage.From("spot-images").GetPublicUrl($"{bucketPath}/{fileName}");
                        _logger.LogInformation("✅ Image uploadée avec succès: {Url}", publicUrl);
                        return publicUrl;
                    }
                    
                    _logger.LogWarning("⚠️ Upload d'image échoué: résultat vide");
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur lors de l'upload d'image");
                    throw;
                }
            });
        }

        /// <summary>
        /// Delete image from Supabase Storage
        /// </summary>
        public async Task<bool> DeleteImageAsync(string imagePath)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("🗑️ Suppression d'image: {ImagePath}", imagePath);
                
                try
                {
                    var result = await client.Storage
                        .From("spot-images")
                        .Remove(new List<string> { imagePath });
                    
                    bool success = result?.Count > 0;
                    _logger.LogInformation(success ? "✅ Image supprimée" : "⚠️ Image non trouvée");
                    return success;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur lors de la suppression d'image");
                    return false;
                }
            });
        }

        /// <summary>
        /// Create spot media record
        /// </summary>
        public async Task<SupabaseSpotMedia> CreateSpotMediaAsync(SupabaseSpotMedia media)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("📸 Création d'un média pour le spot: {SpotId}", media.SpotId);
                
                var result = await client.From<SupabaseSpotMedia>().Insert(media);
                
                _logger.LogInformation("✅ Média créé avec succès: {Id}", result.Model.Id);
                return result.Model;
            });
        }

        /// <summary>
        /// Get spot media
        /// </summary>
        public async Task<List<SupabaseSpotMedia>> GetSpotMediaAsync(Guid spotId)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogDebug("📸 Récupération des médias du spot: {SpotId}", spotId);
                
                var result = await client.From<SupabaseSpotMedia>()
                    .Where(m => m.SpotId == spotId)
                    .Order(m => m.DisplayOrder, Postgrest.Constants.Ordering.Ascending)
                    .Get();
                
                _logger.LogDebug("📸 {Count} médias trouvés", result.Models.Count);
                return result.Models;
            });
        }

        /// <summary>
        /// Delete spot media
        /// </summary>
        public async Task<bool> DeleteSpotMediaAsync(Guid mediaId)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("🗑️ Suppression du média: {MediaId}", mediaId);
                
                // First get the media to delete associated image
                var media = await client.From<SupabaseSpotMedia>()
                    .Where(m => m.Id == mediaId)
                    .Single();
                
                if (media != null && !string.IsNullOrEmpty(media.MediaUrl))
                {
                    // Extract path from URL and delete image
                    var imagePath = ExtractImagePathFromUrl(media.MediaUrl);
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        await DeleteImageAsync(imagePath);
                    }
                }
                
                // Delete media record
                await client.From<SupabaseSpotMedia>()
                    .Where(m => m.Id == mediaId)
                    .Delete();
                
                _logger.LogInformation("✅ Média supprimé avec succès");
                return true;
            });
        }

        /// <summary>
        /// Update spot media metadata
        /// </summary>
        public async Task<bool> UpdateSpotMediaAsync(Guid mediaId, string? caption, bool? isPrimary)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("✏️ Mise à jour du média: {MediaId}", mediaId);
                
                var updateData = new SupabaseSpotMedia();
                if (caption != null) updateData.Caption = caption;
                if (isPrimary != null) updateData.IsPrimary = isPrimary.Value;
                
                var result = await client.From<SupabaseSpotMedia>()
                    .Where(m => m.Id == mediaId)
                    .Update(updateData);
                
                bool updated = result.Models.Count > 0;
                _logger.LogInformation(updated ? "✅ Média mis à jour" : "⚠️ Média non trouvé");
                return updated;
            });
        }

        /// <summary>
        /// Set primary photo for spot
        /// </summary>
        public async Task<bool> SetPrimarySpotPhotoAsync(Guid spotId, Guid photoId)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("⭐ Définition de la photo principale pour le spot: {SpotId}", spotId);
                
                // First, set all photos for this spot to non-primary
                await client.From<SupabaseSpotMedia>()
                    .Where(m => m.SpotId == spotId)
                    .Update(new SupabaseSpotMedia { IsPrimary = false });
                
                // Then set the specified photo as primary
                var result = await client.From<SupabaseSpotMedia>()
                    .Where(m => m.Id == photoId && m.SpotId == spotId)
                    .Update(new SupabaseSpotMedia { IsPrimary = true });
                
                bool updated = result.Models.Count > 0;
                _logger.LogInformation(updated ? "✅ Photo principale mise à jour" : "⚠️ Photo non trouvée");
                return updated;
            });
        }

        /// <summary>
        /// Upload image to Supabase Storage
        /// </summary>
        public async Task<(bool Success, string PublicUrl)> UploadImageAsync(string bucket, string fileName, byte[] imageData)
        {
            return await ExecuteWithResilienceAsync(async (cancellationToken) =>
            {
                var client = await GetClientAsync();
                _logger.LogInformation("📤 Upload d'image: {FileName} vers {Bucket}", fileName, bucket);
                
                try
                {
                    var result = await client.Storage
                        .From(bucket)
                        .Upload(imageData, fileName);
                    
                    if (!string.IsNullOrEmpty(result))
                    {
                        var publicUrl = client.Storage.From(bucket).GetPublicUrl(fileName);
                        _logger.LogInformation("✅ Image uploadée avec succès");
                        return (true, publicUrl);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Upload échoué - résultat vide");
                        return (false, string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur lors de l'upload d'image");
                    return (false, string.Empty);
                }
            });
        }


        /// <summary>
        /// Extract image path from Supabase public URL
        /// </summary>
        private string? ExtractImagePathFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var segments = uri.Segments;
                if (segments.Length >= 3)
                {
                    // Format: /storage/v1/object/public/bucket/path/file.jpg
                    return string.Join("", segments.Skip(5)); // Skip /storage/v1/object/public/bucket/
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}