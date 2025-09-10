// ========================================
// SERVICE DE GESTION DES FAVORIS SUPABASE
// ========================================
// Service avec int\u00e9gration Supabase pour la gestion des spots favoris

using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using SubExplore.Models.Supabase;
using SubExplore.Models.Enums;
using System.Net.Http;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service de gestion des favoris avec int\u00e9gration Supabase native
    /// </summary>
    public class SupabaseFavoriteSpotService : IFavoriteSpotService
    {
        private readonly ISupabaseApiService _supabaseApiService;
        private readonly IFavoriteSpotCacheService _cacheService;
        private readonly ISupabaseSpotService _spotService;
        private readonly ILogger<SupabaseFavoriteSpotService> _logger;
        private readonly Lazy<IOfflineFavoriteService>? _offlineFavoriteServiceLazy;

        public SupabaseFavoriteSpotService(
            ISupabaseApiService supabaseApiService,
            IFavoriteSpotCacheService cacheService,
            ISupabaseSpotService spotService,
            ILogger<SupabaseFavoriteSpotService> logger,
            Lazy<IOfflineFavoriteService>? offlineFavoriteService = null)
        {
            _supabaseApiService = supabaseApiService ?? throw new ArgumentNullException(nameof(supabaseApiService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _spotService = spotService ?? throw new ArgumentNullException(nameof(spotService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _offlineFavoriteServiceLazy = offlineFavoriteService;
        }

        /// <summary>
        /// Ajoute un spot aux favoris d'un utilisateur
        /// </summary>
        public async Task<bool> AddToFavoritesAsync(Guid userId, Guid spotId, int priority = 5, string? notes = null, bool notificationEnabled = true, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("⭐ Ajout spot {SpotId} aux favoris de l'utilisateur {UserId}", spotId, userId);

                // Validation des param\u00e8tres
                if (priority < 1 || priority > 10)
                {
                    throw new ArgumentOutOfRangeException(nameof(priority), "La priorit\u00e9 doit \u00eatre entre 1 et 10");
                }

                if (!string.IsNullOrEmpty(notes) && notes.Length > 500)
                {
                    throw new ArgumentException("Les notes ne peuvent pas d\u00e9passer 500 caract\u00e8res", nameof(notes));
                }

                // V\u00e9rifier si d\u00e9j\u00e0 en favoris
                var isAlreadyFavorite = await IsSpotFavoritedAsync(userId, spotId, cancellationToken);
                if (isAlreadyFavorite)
                {
                    _logger.LogWarning("Spot {SpotId} est d\u00e9j\u00e0 en favoris pour l'utilisateur {UserId}", spotId, userId);
                    return false;
                }

                // Ajouter via l'API Supabase
                var result = await _supabaseApiService.AddToFavoritesAsync(userId, spotId, priority, notes, notificationEnabled);

                // Invalider le cache pour cet utilisateur
                await InvalidateUserCaches(userId);

                _logger.LogInformation("✅ Spot ajout\u00e9 aux favoris avec succ\u00e8s: {FavoriteId}", result.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'ajout du spot {SpotId} aux favoris de l'utilisateur {UserId}", spotId, userId);
                throw;
            }
        }

        /// <summary>
        /// Retire un spot des favoris d'un utilisateur
        /// </summary>
        public async Task<bool> RemoveFromFavoritesAsync(Guid userId, Guid spotId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("❌ Suppression spot {SpotId} des favoris de l'utilisateur {UserId}", spotId, userId);

                // Supprimer via l'API Supabase
                bool removed = await _supabaseApiService.RemoveFromFavoritesAsync(userId, spotId);

                if (removed)
                {
                    // Invalider le cache pour cet utilisateur
                    await InvalidateUserCaches(userId);
                    _logger.LogInformation("✅ Spot retir\u00e9 des favoris avec succ\u00e8s");
                }
                else
                {
                    _logger.LogWarning("⚠️ Spot {SpotId} n'\u00e9tait pas en favoris pour l'utilisateur {UserId}", spotId, userId);
                }

                return removed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la suppression du spot {SpotId} des favoris de l'utilisateur {UserId}", spotId, userId);
                throw;
            }
        }

        /// <summary>
        /// Bascule le statut favori d'un spot
        /// </summary>
        public async Task<bool> ToggleFavoriteAsync(Guid userId, Guid spotId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔄 Basculer favori spot {SpotId} pour l'utilisateur {UserId}", spotId, userId);

                // V\u00e9rifier le statut actuel
                bool currentlyFavorite = await IsSpotFavoritedAsync(userId, spotId, cancellationToken);

                if (currentlyFavorite)
                {
                    // Retirer des favoris
                    await RemoveFromFavoritesAsync(userId, spotId, cancellationToken);
                    return false; // Plus en favoris
                }
                else
                {
                    // Ajouter aux favoris avec des param\u00e8tres par d\u00e9faut
                    await AddToFavoritesAsync(userId, spotId, 5, null, true, cancellationToken);
                    return true; // Maintenant en favoris
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "🌐 Erreur réseau, basculement en mode hors ligne");
                
                // 🔧 AMÉLIORATION: Fallback vers le service offline
                if (_offlineFavoriteServiceLazy?.Value != null)
                {
                    var offlineService = _offlineFavoriteServiceLazy.Value;
                    await offlineService.EnableOfflineModeAsync();
                    
                    // Obtenir le statut depuis le cache offline
                    var offlineFavorites = await offlineService.GetOfflineFavoritesAsync(userId, cancellationToken);
                    bool currentlyFavorite = offlineFavorites.Any(f => f.SpotId == spotId);
                    
                    if (currentlyFavorite)
                    {
                        await offlineService.RemoveOfflineFavoriteAsync(userId, spotId, cancellationToken);
                        return false;
                    }
                    else
                    {
                        await offlineService.AddOfflineFavoriteAsync(userId, spotId, 5, null, true, cancellationToken);
                        return true;
                    }
                }
                
                throw; // Re-throw si pas de service offline
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "⏱️ Timeout, tentative de basculement offline");
                
                if (_offlineFavoriteServiceLazy?.Value != null)
                {
                    // Même logique que pour HttpRequestException
                    var offlineService = _offlineFavoriteServiceLazy.Value;
                    await offlineService.EnableOfflineModeAsync();
                    var offlineFavorites = await offlineService.GetOfflineFavoritesAsync(userId, cancellationToken);
                    bool currentlyFavorite = offlineFavorites.Any(f => f.SpotId == spotId);
                    
                    if (currentlyFavorite)
                    {
                        await offlineService.RemoveOfflineFavoriteAsync(userId, spotId, cancellationToken);
                        return false;
                    }
                    else
                    {
                        await offlineService.AddOfflineFavoriteAsync(userId, spotId, 5, null, true, cancellationToken);
                        return true;
                    }
                }
                
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du basculement favori du spot {SpotId} pour l'utilisateur {UserId}", spotId, userId);
                throw;
            }
        }

        /// <summary>
        /// V\u00e9rifie si un spot est en favoris pour un utilisateur
        /// </summary>
        public async Task<bool> IsSpotFavoritedAsync(Guid userId, Guid spotId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Essayer le cache d'abord
                var cachedStatus = await _cacheService.GetCachedFavoriteStatusAsync(userId, spotId, cancellationToken);
                if (cachedStatus.HasValue)
                {
                    _logger.LogDebug("📈 Cache hit pour statut favori: utilisateur {UserId}, spot {SpotId}", userId, spotId);
                    return cachedStatus.Value;
                }

                // Appeler l'API Supabase
                bool isFavorite = await _supabaseApiService.IsSpotFavoriteAsync(userId, spotId);

                // Mettre en cache le r\u00e9sultat
                await _cacheService.SetFavoriteStatusCacheAsync(userId, spotId, isFavorite, cancellationToken);

                _logger.LogDebug("🔍 Statut favori d\u00e9termin\u00e9: utilisateur {UserId}, spot {SpotId} = {IsFavorite}", userId, spotId, isFavorite);
                return isFavorite;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la v\u00e9rification du statut favori du spot {SpotId} pour l'utilisateur {UserId}", spotId, userId);
                // En cas d'erreur, retourner false par d\u00e9faut
                return false;
            }
        }

        /// <summary>
        /// R\u00e9cup\u00e8re tous les favoris d'un utilisateur avec pagination
        /// </summary>
        public async Task<IEnumerable<UserFavoriteSpot>> GetUserFavoritesAsync(Guid userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("📥 R\u00e9cup\u00e9ration favoris utilisateur {UserId}, page {PageNumber}, taille {PageSize}", userId, pageNumber, pageSize);

                // R\u00e9cup\u00e9rer via l'API Supabase
                var supabaseFavorites = await _supabaseApiService.GetUserFavoritesAsync(userId);

                // Convertir en mod\u00e8les de domaine
                // Convertir en modèles de domaine avec chargement des spots
                var domainFavorites = new List<UserFavoriteSpot>();
                
                foreach (var supabaseFavorite in supabaseFavorites)
                {
                    var domainFavorite = await ConvertToDomainModelWithSpotAsync(supabaseFavorite, cancellationToken);
                    if (domainFavorite != null)
                    {
                        domainFavorites.Add(domainFavorite);
                    }
                }

                // Appliquer pagination et tri
                var pagedFavorites = domainFavorites
                    .OrderByDescending(f => f.Priority)
                    .ThenByDescending(f => f.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("✅ {Count} favoris r\u00e9cup\u00e9r\u00e9s pour l'utilisateur {UserId}", domainFavorites.Count, userId);
                return pagedFavorites;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la r\u00e9cup\u00e9ration des favoris de l'utilisateur {UserId}", userId);
                return Enumerable.Empty<UserFavoriteSpot>();
            }
        }

        /// <summary>
        /// R\u00e9cup\u00e8re les favoris d'un utilisateur tri\u00e9s par priorit\u00e9
        /// </summary>
        public async Task<IEnumerable<UserFavoriteSpot>> GetUserFavoritesByPriorityAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("📥 R\u00e9cup\u00e9ration favoris par priorit\u00e9 pour utilisateur {UserId}", userId);

                // R\u00e9cup\u00e9rer tous les favoris sans pagination
                return await GetUserFavoritesAsync(userId, 1, int.MaxValue, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la r\u00e9cup\u00e9ration des favoris par priorit\u00e9 de l'utilisateur {UserId}", userId);
                return Enumerable.Empty<UserFavoriteSpot>();
            }
        }

        /// <summary>
        /// Met \u00e0 jour la priorit\u00e9 d'un favori
        /// </summary>
        public async Task<bool> UpdateFavoritePriorityAsync(Guid userId, Guid spotId, int priority, CancellationToken cancellationToken = default)
        {
            try
            {
                if (priority < 1 || priority > 10)
                {
                    throw new ArgumentOutOfRangeException(nameof(priority), "La priorit\u00e9 doit \u00eatre entre 1 et 10");
                }

                _logger.LogInformation("🎯 Mise \u00e0 jour priorit\u00e9 favori: utilisateur {UserId}, spot {SpotId}, priorit\u00e9 {Priority}", userId, spotId, priority);

                bool updated = await _supabaseApiService.UpdateFavoritePriorityAsync(userId, spotId, priority);

                if (updated)
                {
                    await InvalidateUserCaches(userId);
                    _logger.LogInformation("✅ Priorit\u00e9 mise \u00e0 jour avec succ\u00e8s");
                }

                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la mise \u00e0 jour de la priorit\u00e9 du favori {SpotId} pour l'utilisateur {UserId}", spotId, userId);
                throw;
            }
        }

        /// <summary>
        /// Met \u00e0 jour les notes d'un favori
        /// </summary>
        public async Task<bool> UpdateFavoriteNotesAsync(Guid userId, Guid spotId, string? notes, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!string.IsNullOrEmpty(notes) && notes.Length > 500)
                {
                    throw new ArgumentException("Les notes ne peuvent pas d\u00e9passer 500 caract\u00e8res", nameof(notes));
                }

                _logger.LogInformation("📝 Mise \u00e0 jour notes favori: utilisateur {UserId}, spot {SpotId}", userId, spotId);

                bool updated = await _supabaseApiService.UpdateFavoriteNotesAsync(userId, spotId, notes);

                if (updated)
                {
                    await InvalidateUserCaches(userId);
                    _logger.LogInformation("✅ Notes mises \u00e0 jour avec succ\u00e8s");
                }

                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la mise \u00e0 jour des notes du favori {SpotId} pour l'utilisateur {UserId}", spotId, userId);
                throw;
            }
        }

        /// <summary>
        /// Met \u00e0 jour les param\u00e8tres de notification d'un favori
        /// </summary>
        public async Task<bool> UpdateFavoriteNotificationAsync(Guid userId, Guid spotId, bool enabled, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔔 Mise \u00e0 jour notifications favori: utilisateur {UserId}, spot {SpotId}, activ\u00e9 {Enabled}", userId, spotId, enabled);

                bool updated = await _supabaseApiService.UpdateFavoriteNotificationAsync(userId, spotId, enabled);

                if (updated)
                {
                    await InvalidateUserCaches(userId);
                    _logger.LogInformation("✅ Notifications mises \u00e0 jour avec succ\u00e8s");
                }

                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la mise \u00e0 jour des notifications du favori {SpotId} pour l'utilisateur {UserId}", spotId, userId);
                throw;
            }
        }

        /// <summary>
        /// R\u00e9cup\u00e8re les favoris avec notifications activ\u00e9es
        /// </summary>
        public async Task<IEnumerable<UserFavoriteSpot>> GetNotificationFavoritesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔔 R\u00e9cup\u00e9ration favoris avec notifications pour utilisateur {UserId}", userId);

                var allFavorites = await GetUserFavoritesAsync(userId, 1, int.MaxValue, cancellationToken);
                var notificationFavorites = allFavorites.Where(f => f.NotificationEnabled).ToList();

                _logger.LogInformation("✅ {Count} favoris avec notifications trouv\u00e9s", notificationFavorites.Count);
                return notificationFavorites;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la r\u00e9cup\u00e9ration des favoris avec notifications de l'utilisateur {UserId}", userId);
                return Enumerable.Empty<UserFavoriteSpot>();
            }
        }

        /// <summary>
        /// R\u00e9cup\u00e8re les statistiques des favoris d'un utilisateur
        /// </summary>
        public async Task<FavoriteSpotStats> GetUserFavoriteStatsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("📊 R\u00e9cup\u00e9ration statistiques favoris pour utilisateur {UserId}", userId);

                var favorites = await GetUserFavoritesAsync(userId, 1, int.MaxValue, cancellationToken);
                var favoritesList = favorites.ToList();

                var stats = new FavoriteSpotStats
                {
                    TotalFavorites = favoritesList.Count,
                    NotificationEnabled = favoritesList.Count(f => f.NotificationEnabled),
                    ActivityFavorites = favoritesList.Count, // Tous sont des activit\u00e9s dans notre contexte
                    MostRecentFavorite = favoritesList.Count > 0 ? favoritesList.Max(f => f.CreatedAt) : null,
                    OldestFavorite = favoritesList.Count > 0 ? favoritesList.Min(f => f.CreatedAt) : null
                };

                // Statistiques par type de spot (n\u00e9cessite des donn\u00e9es de spot compl\u00e8tes)
                stats.FavoritesByType = new Dictionary<string, int>
                {
                    { "Total", favoritesList.Count }
                };

                _logger.LogInformation("✅ Statistiques calcul\u00e9es: {TotalFavorites} favoris, {NotificationEnabled} avec notifications", stats.TotalFavorites, stats.NotificationEnabled);
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul des statistiques des favoris de l'utilisateur {UserId}", userId);
                return new FavoriteSpotStats();
            }
        }

        /// <summary>
        /// R\u00e9cup\u00e8re le nombre d'utilisateurs qui ont mis un spot en favoris
        /// </summary>
        public async Task<int> GetSpotFavoritesCountAsync(Guid spotId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("📊 Comptage favoris pour spot {SpotId}", spotId);

                int count = await _supabaseApiService.GetSpotFavoritesCountAsync(spotId);

                _logger.LogDebug("✅ Spot {SpotId} a {Count} favoris", spotId, count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du comptage des favoris du spot {SpotId}", spotId);
                return 0;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Convertit un mod\u00e8le Supabase en mod\u00e8le de domaine
        /// </summary>
        private UserFavoriteSpot? ConvertToDomainModel(SupabaseUserFavoriteSpot supabaseFavorite)
        {
            try
            {
                return new UserFavoriteSpot
                {
                    Id = supabaseFavorite.Id,
                    UserId = supabaseFavorite.UserId,
                    SpotId = supabaseFavorite.SpotId,
                    CreatedAt = supabaseFavorite.CreatedAt,
                    UpdatedAt = supabaseFavorite.UpdatedAt,
                    Notes = supabaseFavorite.Notes,
                    Priority = supabaseFavorite.Priority,
                    NotificationEnabled = supabaseFavorite.NotificationEnabled
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Erreur lors de la conversion du favori {FavoriteId}", supabaseFavorite.Id);
                return null;
            }
        }

        /// <summary>
        /// Invalide tous les caches li\u00e9s \u00e0 un utilisateur
        /// </summary>
        private async Task InvalidateUserCaches(Guid userId)
        {
            try
            {
                // Invalider le cache des favoris de l'utilisateur
                await _cacheService.InvalidateUserFavoritesCacheAsync(userId);
                _logger.LogDebug("🗑️ Cache favoris invalid\u00e9 pour l'utilisateur {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Erreur lors de l'invalidation du cache pour l'utilisateur {UserId}", userId);
                // Ne pas propager l'erreur, ce n'est pas critique
            }
        }

        /// <summary>
        /// Convertit un modèle Supabase en modèle de domaine avec chargement du spot associé
        /// </summary>
        private async Task<UserFavoriteSpot?> ConvertToDomainModelWithSpotAsync(SupabaseUserFavoriteSpot supabaseFavorite, CancellationToken cancellationToken = default)
        {
            try
            {
                // Charger les données du spot
                var supabaseSpot = await _spotService.GetSpotByIdAsync(supabaseFavorite.SpotId);
                if (supabaseSpot == null)
                {
                    _logger.LogWarning("⚠️ Spot {SpotId} introuvable pour le favori {FavoriteId}", supabaseFavorite.SpotId, supabaseFavorite.Id);
                    return null;
                }

                // Convertir le spot Supabase en modèle de domaine
                var domainSpot = ConvertSupabaseSpotToDomain(supabaseSpot);
                if (domainSpot == null)
                {
                    _logger.LogWarning("⚠️ Impossible de convertir le spot {SpotId}", supabaseFavorite.SpotId);
                    return null;
                }

                return new UserFavoriteSpot
                {
                    Id = supabaseFavorite.Id,
                    UserId = supabaseFavorite.UserId,
                    SpotId = supabaseFavorite.SpotId,
                    CreatedAt = supabaseFavorite.CreatedAt,
                    UpdatedAt = supabaseFavorite.UpdatedAt,
                    Notes = supabaseFavorite.Notes,
                    Priority = supabaseFavorite.Priority,
                    NotificationEnabled = supabaseFavorite.NotificationEnabled,
                    Spot = domainSpot,
                    User = null! // TODO: Load user data if needed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la conversion du favori avec spot {FavoriteId}", supabaseFavorite.Id);
                return null;
            }
        }

        /// <summary>
        /// Convertit un SupabaseSpot en Spot de domaine
        /// </summary>
        private Spot? ConvertSupabaseSpotToDomain(SupabaseSpot supabaseSpot)
        {
            try
            {
                return new Spot
                {
                    Id = supabaseSpot.Id,
                    CreatorId = supabaseSpot.CreatorId,
                    Name = supabaseSpot.Name,
                    Description = supabaseSpot.Description,
                    Latitude = supabaseSpot.Latitude,
                    Longitude = supabaseSpot.Longitude,
                    DifficultyLevel = supabaseSpot.DifficultyLevel.HasValue ? (DifficultyLevel)supabaseSpot.DifficultyLevel.Value : DifficultyLevel.Beginner,
                    TypeId = supabaseSpot.TypeId,
                    RequiredEquipment = supabaseSpot.RequiredEquipment,
                    SafetyNotes = supabaseSpot.SafetyNotes,
                    BestConditions = supabaseSpot.BestConditions,
                    CreatedAt = supabaseSpot.CreatedAt,
                    ValidationStatus = ConvertStringToValidationStatus(supabaseSpot.ValidationStatus),
                    LastSafetyReview = supabaseSpot.LastSafetyReview,
                    MaxDepth = (int?)supabaseSpot.MaxDepth,
                    CurrentStrength = (CurrentStrength)supabaseSpot.CurrentStrength,
                    HasMooring = supabaseSpot.HasMooring,
                    BottomType = supabaseSpot.BottomType,
                    // TODO: Load SpotType navigation property if needed for display
                    Type = new SpotType
                    {
                        Id = supabaseSpot.TypeId,
                        Name = "Type de plongée", // Placeholder - will be loaded properly later
                        Category = ActivityCategory.Activity
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la conversion du spot {SpotId}", supabaseSpot.Id);
                return null;
            }
        }

        #endregion

        #region Conversion Methods

        private SpotValidationStatus ConvertStringToValidationStatus(string status)
        {
            return status?.ToLower() switch
            {
                "draft" => SpotValidationStatus.Draft,
                "pending" => SpotValidationStatus.Pending,
                "under_review" => SpotValidationStatus.UnderReview,
                "needs_revision" => SpotValidationStatus.NeedsRevision,
                "safety_review" => SpotValidationStatus.SafetyReview,
                "approved" => SpotValidationStatus.Approved,
                "rejected" => SpotValidationStatus.Rejected,
                "archived" => SpotValidationStatus.Archived,
                _ => SpotValidationStatus.Pending
            };
        }

        private string ConvertValidationStatusToString(SpotValidationStatus status)
        {
            return status switch
            {
                SpotValidationStatus.Draft => "draft",
                SpotValidationStatus.Pending => "pending",
                SpotValidationStatus.UnderReview => "under_review",
                SpotValidationStatus.NeedsRevision => "needs_revision",
                SpotValidationStatus.SafetyReview => "safety_review",
                SpotValidationStatus.Approved => "approved",
                SpotValidationStatus.Rejected => "rejected",
                SpotValidationStatus.Archived => "archived",
                _ => "pending"
            };
        }

        #endregion
    }
}