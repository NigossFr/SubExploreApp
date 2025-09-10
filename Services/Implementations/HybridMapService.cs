using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service hybride pour la carte utilisant Entity Framework + PostGIS pour les performances
    /// optimales avec fallback vers l'API Supabase si nécessaire
    /// </summary>
    public class HybridMapService : IHybridMapService
    {
        private readonly IPracticeSpotRepository _practiceSpotRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IGenericRepository<SpotType> _spotTypeRepository;
        private readonly ISupabaseSpotTypeService _supabaseSpotTypeService; // Fallback pour les SpotTypes
        private readonly ILogger<HybridMapService> _logger;

        public HybridMapService(
            IPracticeSpotRepository practiceSpotRepository,
            IOrganizationRepository organizationRepository,
            IBusinessRepository businessRepository,
            IGenericRepository<SpotType> spotTypeRepository,
            ISupabaseSpotTypeService supabaseSpotTypeService,
            ILogger<HybridMapService> logger)
        {
            _practiceSpotRepository = practiceSpotRepository;
            _organizationRepository = organizationRepository;
            _businessRepository = businessRepository;
            _spotTypeRepository = spotTypeRepository;
            _supabaseSpotTypeService = supabaseSpotTypeService;
            _logger = logger;
        }

        /// <summary>
        /// Utilise PostGIS pour une recherche géospatiale optimisée des spots de pratique
        /// </summary>
        public async Task<IEnumerable<PracticeSpot>> GetNearbyPracticeSpotsAsync(
            decimal latitude, 
            decimal longitude, 
            int radiusKm = 10, 
            Guid? spotTypeFilter = null)
        {
            try
            {
                _logger.LogInformation("Recherche PostGIS des spots de pratique à ({Latitude}, {Longitude}) dans un rayon de {Radius}km", 
                    latitude, longitude, radiusKm);

                // Utilise la fonction PostGIS optimisée
                var spots = await _practiceSpotRepository.GetNearbyPracticeSpotsAsync(
                    latitude, longitude, radiusKm, spotTypeFilter);

                _logger.LogInformation("PostGIS a trouvé {Count} spots de pratique", spots.Count());
                return spots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche PostGIS des spots de pratique");
                throw;
            }
        }

        /// <summary>
        /// Recherche géospatiale des organisations avec Entity Framework
        /// </summary>
        public async Task<IEnumerable<Organization>> GetNearbyOrganizationsAsync(
            decimal latitude, 
            decimal longitude, 
            int radiusKm = 10)
        {
            try
            {
                _logger.LogInformation("Recherche des organisations près de ({Latitude}, {Longitude})", latitude, longitude);
                
                // Si un repository spécialisé avec PostGIS existe, l'utiliser
                // Sinon, utiliser une approximation avec Entity Framework
                var organizations = await _organizationRepository.FindAsync(o => 
                    Math.Abs((double)(o.Latitude - latitude)) <= (radiusKm / 111.0) && 
                    Math.Abs((double)(o.Longitude - longitude)) <= (radiusKm / 111.0) &&
                    o.VerificationStatus == VerificationStatus.Verified);

                var result = organizations
                    .OrderBy(o => Math.Abs((double)(o.Latitude - latitude)) + Math.Abs((double)(o.Longitude - longitude)))
                    .Take(50)
                    .ToList();

                _logger.LogInformation("Trouvé {Count} organisations", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche des organisations");
                throw;
            }
        }

        /// <summary>
        /// Recherche géospatiale des commerces avec Entity Framework
        /// </summary>
        public async Task<IEnumerable<Business>> GetNearbyBusinessesAsync(
            decimal latitude, 
            decimal longitude, 
            int radiusKm = 10)
        {
            try
            {
                _logger.LogInformation("Recherche des commerces près de ({Latitude}, {Longitude})", latitude, longitude);
                
                var businesses = await _businessRepository.FindAsync(b => 
                    Math.Abs((double)(b.Latitude - latitude)) <= (radiusKm / 111.0) && 
                    Math.Abs((double)(b.Longitude - longitude)) <= (radiusKm / 111.0) &&
                    b.VerificationStatus == VerificationStatus.Verified);

                var result = businesses
                    .OrderBy(b => Math.Abs((double)(b.Latitude - latitude)) + Math.Abs((double)(b.Longitude - longitude)))
                    .Take(50)
                    .ToList();

                _logger.LogInformation("Trouvé {Count} commerces", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche des commerces");
                throw;
            }
        }

        /// <summary>
        /// Recherche textuelle des spots de pratique avec Entity Framework
        /// </summary>
        public async Task<IEnumerable<PracticeSpot>> SearchPracticeSpotsAsync(string query)
        {
            try
            {
                _logger.LogInformation("Recherche textuelle des spots : '{Query}'", query);
                var spots = await _practiceSpotRepository.SearchPracticeSpotsAsync(query);
                _logger.LogInformation("Trouvé {Count} spots pour la recherche '{Query}'", spots.Count(), query);
                return spots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche textuelle des spots");
                throw;
            }
        }

        /// <summary>
        /// Filtre les spots par type avec Entity Framework
        /// </summary>
        public async Task<IEnumerable<PracticeSpot>> GetPracticeSpotsByTypeAsync(Guid spotTypeId)
        {
            try
            {
                _logger.LogInformation("Recherche des spots par type : {SpotTypeId}", spotTypeId);
                var spots = await _practiceSpotRepository.GetBySpotTypeAsync(spotTypeId);
                _logger.LogInformation("Trouvé {Count} spots pour le type {SpotTypeId}", spots.Count(), spotTypeId);
                return spots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche par type");
                throw;
            }
        }

        /// <summary>
        /// Filtre les spots par difficulté avec Entity Framework
        /// </summary>
        public async Task<IEnumerable<PracticeSpot>> GetPracticeSpotsByDifficultyAsync(DifficultyLevel difficulty)
        {
            try
            {
                _logger.LogInformation("Recherche des spots par difficulté : {Difficulty}", difficulty);
                var spots = await _practiceSpotRepository.GetByDifficultyLevelAsync(difficulty);
                _logger.LogInformation("Trouvé {Count} spots pour la difficulté {Difficulty}", spots.Count(), difficulty);
                return spots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche par difficulté");
                throw;
            }
        }

        /// <summary>
        /// Récupère les types de spots actifs
        /// Utilise Entity Framework en priorité avec fallback vers l'API Supabase
        /// </summary>
        public async Task<IEnumerable<SpotType>> GetActiveSpotTypesAsync()
        {
            try
            {
                _logger.LogInformation("Chargement des types de spots actifs");
                
                // Essayer d'abord avec Entity Framework
                try
                {
                    var spotTypes = await _spotTypeRepository.FindAsync(st => st.IsActive);
                    var result = spotTypes.ToList();
                    
                    if (result.Any())
                    {
                        _logger.LogInformation("Entity Framework a trouvé {Count} types de spots", result.Count);
                        return result;
                    }
                }
                catch (Exception efEx)
                {
                    _logger.LogWarning(efEx, "Échec Entity Framework, fallback vers l'API Supabase");
                }

                // Fallback vers l'API Supabase
                var supabaseSpotTypes = await _supabaseSpotTypeService.GetActiveSpotTypesAsync();
                var convertedTypes = supabaseSpotTypes.Select(st => _supabaseSpotTypeService.ConvertToDomainModel(st)).ToList();
                
                _logger.LogInformation("API Supabase fallback a trouvé {Count} types de spots", convertedTypes.Count);
                return convertedTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des types de spots");
                throw;
            }
        }
    }
}