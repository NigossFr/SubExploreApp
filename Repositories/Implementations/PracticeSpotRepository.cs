using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;

namespace SubExplore.Repositories.Implementations
{
    public class PracticeSpotRepository : GenericRepository<PracticeSpot>, IPracticeSpotRepository
    {
        public PracticeSpotRepository(SubExploreDbContext context) : base(context)
        {
        }

        // APPROCHE HYBRIDE : Fonction PostGIS pour la géolocalisation
        public async Task<IEnumerable<PracticeSpot>> GetNearbyPracticeSpotsAsync(decimal latitude, decimal longitude, int radiusKm = 10, Guid? spotTypeFilter = null)
        {
            try
            {
                // Appel direct de la fonction PostGIS qui retourne les enregistrements complets
                var sql = "SELECT * FROM find_practice_spots_near_location({0}, {1}, {2}, {3})";
                
                // Gestion correcte des paramètres UUID/Guid
                var parameters = new object[] { 
                    latitude, 
                    longitude, 
                    radiusKm, 
                    spotTypeFilter?.ToString() ?? (object)DBNull.Value 
                };
                
                // Exécution de la requête avec récupération directe des PracticeSpots
                var nearbySpots = await _context.PracticeSpots
                    .FromSqlRaw(sql, parameters)
                    .Include(s => s.SpotType)
                    .Include(s => s.Creator)
                    .Include(s => s.Media.Where(m => m.IsPrimary))
                    .ToListAsync();

                return nearbySpots;
            }
            catch (Exception ex)
            {
                // Log de l'erreur et fallback sur une requête EF Core classique
                System.Diagnostics.Debug.WriteLine($"Erreur PostGIS PracticeSpots: {ex.Message}");
                
                // Fallback : requête EF Core avec calcul de distance approximatif
                return await _context.PracticeSpots
                    .Include(s => s.SpotType)
                    .Include(s => s.Creator)
                    .Include(s => s.Media.Where(m => m.IsPrimary))
                    .Where(s => s.ValidationStatus == SpotValidationStatus.Approved)
                    .Where(s => spotTypeFilter == null || s.SpotTypeId == spotTypeFilter)
                    // Approximation : 1 degré ≈ 111km, donc radiusKm/111 degrés
                    .Where(s => Math.Abs((double)(s.Latitude - latitude)) <= (radiusKm / 111.0) && 
                               Math.Abs((double)(s.Longitude - longitude)) <= (radiusKm / 111.0))
                    .OrderBy(s => Math.Abs((double)(s.Latitude - latitude)) + Math.Abs((double)(s.Longitude - longitude)))
                    .Take(50) // Limiter les résultats
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<PracticeSpot>> GetBySpotTypeAsync(Guid spotTypeId)
        {
            return await _context.PracticeSpots
                .Include(s => s.SpotType)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.SpotTypeId == spotTypeId && s.ValidationStatus == SpotValidationStatus.Approved)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PracticeSpot>> GetByDifficultyLevelAsync(DifficultyLevel difficulty)
        {
            return await _context.PracticeSpots
                .Include(s => s.SpotType)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.DifficultyLevel == difficulty && s.ValidationStatus == SpotValidationStatus.Approved)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PracticeSpot>> GetByCreatorAsync(Guid creatorId)
        {
            return await _context.PracticeSpots
                .Include(s => s.SpotType)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.CreatorId == creatorId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PracticeSpot>> GetByValidationStatusAsync(SpotValidationStatus status)
        {
            return await _context.PracticeSpots
                .Include(s => s.SpotType)
                .Include(s => s.Creator)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.ValidationStatus == status)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PracticeSpot>> SearchPracticeSpotsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetAllAsync();

            string normalizedQuery = query.ToLower();

            return await _context.PracticeSpots
                .Include(s => s.SpotType)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => (s.Name.ToLower().Contains(normalizedQuery) ||
                            s.Description.ToLower().Contains(normalizedQuery)) &&
                            s.ValidationStatus == SpotValidationStatus.Approved)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ValidateSpotAsync(int spotId, Guid validatorId)
        {
            var spot = await GetByIdAsync(spotId);
            if (spot == null)
                return false;

            spot.ValidationStatus = SpotValidationStatus.Approved;
            spot.ValidatedBy = validatorId;
            spot.ValidatedAt = DateTime.UtcNow;
            spot.UpdatedAt = DateTime.UtcNow;

            await UpdateAsync(spot);
            return true;
        }
    }
}