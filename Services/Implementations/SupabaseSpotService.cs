// ========================================
// SERVICE SPOTS SUPABASE IMPLEMENTATION
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
    /// Implémentation du service de gestion des spots via l'API Supabase
    /// </summary>
    public class SupabaseSpotService : ISupabaseSpotService
    {
        private readonly ISupabaseClientService _supabaseClient;
        private readonly ILogger<SupabaseSpotService> _logger;
        private readonly IRetryPolicyService? _retryPolicy;

        public SupabaseSpotService(
            ISupabaseClientService supabaseClient,
            ILogger<SupabaseSpotService> logger,
            IRetryPolicyService? retryPolicy = null)
        {
            _supabaseClient = supabaseClient;
            _logger = logger;
            _retryPolicy = retryPolicy;
        }

        public async Task<List<SupabaseSpot>> GetApprovedSpotsAsync()
        {
            try
            {
                _logger.LogInformation("📍 Récupération des spots approuvés...");
                
                var client = await _supabaseClient.GetClientAsync();
                if (client == null)
                {
                    _logger.LogError("❌ Client Supabase non disponible");
                    return new List<SupabaseSpot>();
                }

                var result = await client
                    .From<SupabaseSpot>()
                    .Filter("validation_status", Postgrest.Constants.Operator.Equals, 1) // Approved = 1
                    .Get();

                _logger.LogInformation($"✅ {result.Models.Count} spots approuvés récupérés");
                return result.Models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération des spots approuvés");
                return new List<SupabaseSpot>();
            }
        }

        public async Task<SupabaseSpot?> GetSpotByIdAsync(Guid spotId)
        {
            try
            {
                var client = await _supabaseClient.GetClientAsync();
                if (client == null) return null;

                var result = await client
                    .From<SupabaseSpot>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, spotId.ToString())
                    .Single();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération du spot {spotId}");
                return null;
            }
        }

        public async Task<SupabaseSpot> CreateSpotAsync(SupabaseSpot spot)
        {
            try
            {
                _logger.LogInformation($"📍 Création du spot: {spot.Name}");
                
                var client = await _supabaseClient.GetClientAsync();
                if (client == null)
                    throw new InvalidOperationException("Client Supabase non disponible");

                spot.Id = Guid.NewGuid();
                spot.CreatedAt = DateTime.UtcNow;
                spot.ValidationStatus = 0; // Pending

                var result = await client
                    .From<SupabaseSpot>()
                    .Insert(spot);

                _logger.LogInformation($"✅ Spot créé avec succès: {spot.Name}");
                return result.Model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la création du spot: {spot.Name}");
                throw;
            }
        }

        public async Task<SupabaseSpot> UpdateSpotAsync(SupabaseSpot spot)
        {
            try
            {
                var client = await _supabaseClient.GetClientAsync();
                if (client == null)
                    throw new InvalidOperationException("Client Supabase non disponible");

                var result = await client
                    .From<SupabaseSpot>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, spot.Id.ToString())
                    .Update(spot);

                _logger.LogInformation($"✅ Spot mis à jour: {spot.Name}");
                return result.Model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la mise à jour du spot: {spot.Name}");
                throw;
            }
        }

        public async Task<bool> DeleteSpotAsync(Guid spotId)
        {
            try
            {
                var client = await _supabaseClient.GetClientAsync();
                if (client == null) return false;

                await client
                    .From<SupabaseSpot>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, spotId.ToString())
                    .Delete();

                _logger.LogInformation($"✅ Spot supprimé: {spotId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la suppression du spot: {spotId}");
                return false;
            }
        }

        public async Task<List<SupabaseSpot>> SearchSpotsAsync(string searchText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText))
                    return await GetApprovedSpotsAsync();

                var client = await _supabaseClient.GetClientAsync();
                if (client == null) return new List<SupabaseSpot>();

                var result = await client
                    .From<SupabaseSpot>()
                    .Filter("validation_status", Postgrest.Constants.Operator.Equals, 1)
                    .Filter("name", Postgrest.Constants.Operator.ILike, $"%{searchText}%")
                    .Get();

                return result.Models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la recherche de spots: {searchText}");
                return new List<SupabaseSpot>();
            }
        }

        public async Task<List<SupabaseSpot>> GetSpotsByLocationAsync(decimal latitude, decimal longitude, double radiusKm)
        {
            try
            {
                // Pour l'instant, récupérons tous les spots et filtrons côté client
                // TODO: Implémenter une requête PostGIS pour la géolocalisation
                var allSpots = await GetApprovedSpotsAsync();
                
                var spotsInRadius = allSpots.Where(spot =>
                {
                    var distance = CalculateDistance(latitude, longitude, spot.Latitude, spot.Longitude);
                    return distance <= radiusKm;
                }).ToList();

                return spotsInRadius;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération des spots par localisation");
                return new List<SupabaseSpot>();
            }
        }

        public async Task<List<SupabaseSpot>> GetUserSpotsAsync(Guid userId)
        {
            try
            {
                var client = await _supabaseClient.GetClientAsync();
                if (client == null) return new List<SupabaseSpot>();

                var result = await client
                    .From<SupabaseSpot>()
                    .Filter("creator_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                    .Get();

                return result.Models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération des spots de l'utilisateur: {userId}");
                return new List<SupabaseSpot>();
            }
        }

        public Spot ConvertToDomainModel(SupabaseSpot supabaseSpot)
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
                ValidationStatus = (SpotValidationStatus)supabaseSpot.ValidationStatus,
                LastSafetyReview = supabaseSpot.LastSafetyReview,
                MaxDepth = supabaseSpot.MaxDepth.HasValue ? Convert.ToInt32(supabaseSpot.MaxDepth.Value) : (int?)null,
                CurrentStrength = supabaseSpot.CurrentStrength != null ? (CurrentStrength)supabaseSpot.CurrentStrength : (CurrentStrength?)null,
                HasMooring = supabaseSpot.HasMooring,
                BottomType = supabaseSpot.BottomType
            };
        }

        public SupabaseSpot ConvertFromDomainModel(Spot spot)
        {
            return new SupabaseSpot
            {
                Id = spot.Id,
                CreatorId = spot.CreatorId,
                Name = spot.Name,
                Description = spot.Description,
                Latitude = spot.Latitude,
                Longitude = spot.Longitude,
                DifficultyLevel = (int?)spot.DifficultyLevel,
                TypeId = spot.TypeId,
                RequiredEquipment = spot.RequiredEquipment,
                SafetyNotes = spot.SafetyNotes,
                BestConditions = spot.BestConditions,
                CreatedAt = spot.CreatedAt,
                ValidationStatus = (int)spot.ValidationStatus,
                LastSafetyReview = spot.LastSafetyReview,
                MaxDepth = spot.MaxDepth.HasValue ? Convert.ToDecimal(spot.MaxDepth.Value) : (decimal?)null,
                CurrentStrength = spot.CurrentStrength.HasValue ? (int)spot.CurrentStrength.Value : 0,
                HasMooring = spot.HasMooring ?? false,
                BottomType = spot.BottomType
            };
        }

        private double CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            var R = 6371; // Radius of the Earth in km
            var dLat = DegreesToRadians((double)(lat2 - lat1));
            var dLon = DegreesToRadians((double)(lon2 - lon1));
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians((double)lat1)) * Math.Cos(DegreesToRadians((double)lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }
}