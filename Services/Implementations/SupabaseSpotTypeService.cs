// ========================================
// SERVICE SPOT TYPES SUPABASE IMPLEMENTATION
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
    /// Implémentation du service de gestion des types de spots via l'API Supabase
    /// </summary>
    public class SupabaseSpotTypeService : ISupabaseSpotTypeService
    {
        private readonly ISupabaseClientService _supabaseClient;
        private readonly ILogger<SupabaseSpotTypeService> _logger;
        private readonly IRetryPolicyService? _retryPolicy;

        public SupabaseSpotTypeService(
            ISupabaseClientService supabaseClient,
            ILogger<SupabaseSpotTypeService> logger,
            IRetryPolicyService? retryPolicy = null)
        {
            _supabaseClient = supabaseClient;
            _logger = logger;
            _retryPolicy = retryPolicy;
        }

        public async Task<List<SupabaseSpotType>> GetActiveSpotTypesAsync()
        {
            try
            {
                _logger.LogInformation("🏷️ Récupération des types de spots actifs...");
                
                var client = await _supabaseClient.GetClientAsync();
                if (client == null)
                {
                    _logger.LogError("❌ Client Supabase non disponible");
                    return new List<SupabaseSpotType>();
                }

                var result = await client
                    .From<SupabaseSpotType>()
                    .Where(x => x.IsActive == true)
                    .Order("name", Postgrest.Constants.Ordering.Ascending)
                    .Get();

                _logger.LogInformation($"✅ {result.Models.Count} types de spots actifs récupérés");
                return result.Models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération des types de spots actifs");
                return new List<SupabaseSpotType>();
            }
        }

        public async Task<SupabaseSpotType?> GetSpotTypeByIdAsync(Guid typeId)
        {
            try
            {
                var client = await _supabaseClient.GetClientAsync();
                if (client == null) return null;

                var result = await client
                    .From<SupabaseSpotType>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, typeId.ToString())
                    .Single();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération du type de spot {typeId}");
                return null;
            }
        }

        public async Task<List<SupabaseSpotType>> GetSpotTypesByCategoryAsync(string category)
        {
            try
            {
                var client = await _supabaseClient.GetClientAsync();
                if (client == null) return new List<SupabaseSpotType>();

                var result = await client
                    .From<SupabaseSpotType>()
                    .Where(x => x.IsActive == true && x.Category == category)
                    .Order("name", Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return result.Models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération des types par catégorie: {category}");
                return new List<SupabaseSpotType>();
            }
        }

        public async Task<SupabaseSpotType> CreateSpotTypeAsync(SupabaseSpotType spotType)
        {
            try
            {
                _logger.LogInformation($"🏷️ Création du type de spot: {spotType.Name}");
                
                var client = await _supabaseClient.GetClientAsync();
                if (client == null)
                    throw new InvalidOperationException("Client Supabase non disponible");

                spotType.Id = Guid.NewGuid();
                spotType.CreatedAt = DateTime.UtcNow;
                spotType.UpdatedAt = DateTime.UtcNow;

                var result = await client
                    .From<SupabaseSpotType>()
                    .Insert(spotType);

                _logger.LogInformation($"✅ Type de spot créé avec succès: {spotType.Name}");
                return result.Model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la création du type de spot: {spotType.Name}");
                throw;
            }
        }

        public async Task<SupabaseSpotType> UpdateSpotTypeAsync(SupabaseSpotType spotType)
        {
            try
            {
                var client = await _supabaseClient.GetClientAsync();
                if (client == null)
                    throw new InvalidOperationException("Client Supabase non disponible");

                spotType.UpdatedAt = DateTime.UtcNow;

                var result = await client
                    .From<SupabaseSpotType>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, spotType.Id.ToString())
                    .Update(spotType);

                _logger.LogInformation($"✅ Type de spot mis à jour: {spotType.Name}");
                return result.Model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la mise à jour du type de spot: {spotType.Name}");
                throw;
            }
        }

        public async Task<bool> SetSpotTypeActiveAsync(Guid typeId, bool isActive)
        {
            try
            {
                var client = await _supabaseClient.GetClientAsync();
                if (client == null) return false;

                var spotType = await GetSpotTypeByIdAsync(typeId);
                if (spotType == null) return false;

                spotType.IsActive = isActive;
                spotType.UpdatedAt = DateTime.UtcNow;

                await client
                    .From<SupabaseSpotType>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, typeId.ToString())
                    .Update(spotType);

                _logger.LogInformation($"✅ Type de spot {(isActive ? "activé" : "désactivé")}: {typeId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la modification du statut du type: {typeId}");
                return false;
            }
        }

        public SpotType ConvertToDomainModel(SupabaseSpotType supabaseSpotType)
        {
            // Conversion de la catégorie string vers enum
            ActivityCategory category = ActivityCategory.Activity; // Valeur par défaut
            if (Enum.TryParse<ActivityCategory>(supabaseSpotType.Category, true, out var parsedCategory))
            {
                category = parsedCategory;
            }

            return new SpotType
            {
                Id = supabaseSpotType.Id,
                Name = supabaseSpotType.Name,
                IconPath = supabaseSpotType.IconPath,
                ColorCode = supabaseSpotType.ColorCode,
                RequiresExpertValidation = supabaseSpotType.RequiresExpertValidation,
                Category = category,
                Description = supabaseSpotType.Description,
                IsActive = supabaseSpotType.IsActive,
                CreatedAt = supabaseSpotType.CreatedAt,
                UpdatedAt = supabaseSpotType.UpdatedAt
            };
        }

        public SupabaseSpotType ConvertFromDomainModel(SpotType spotType)
        {
            return new SupabaseSpotType
            {
                Id = spotType.Id,
                Name = spotType.Name,
                IconPath = spotType.IconPath,
                ColorCode = spotType.ColorCode,
                RequiresExpertValidation = spotType.RequiresExpertValidation,
                Category = spotType.Category.ToString(),
                Description = spotType.Description,
                IsActive = spotType.IsActive,
                CreatedAt = spotType.CreatedAt,
                UpdatedAt = spotType.UpdatedAt ?? DateTime.UtcNow
            };
        }
    }
}