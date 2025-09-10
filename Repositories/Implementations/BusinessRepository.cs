using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;

namespace SubExplore.Repositories.Implementations
{
    public class BusinessRepository : GenericRepository<Business>, IBusinessRepository
    {
        public BusinessRepository(SubExploreDbContext context) : base(context)
        {
        }

        // APPROCHE HYBRIDE : Fonction PostGIS pour la géolocalisation
        public async Task<IEnumerable<Business>> GetNearbyBusinessesAsync(decimal latitude, decimal longitude, int radiusKm = 10, BusinessType? typeFilter = null)
        {
            try
            {
                // Appel direct de la fonction PostGIS qui retourne les enregistrements complets
                var sql = "SELECT * FROM find_businesses_near_location({0}, {1}, {2}, {3})";
                
                // Gestion correcte des paramètres enum
                var parameters = new object[] { 
                    latitude, 
                    longitude, 
                    radiusKm, 
                    typeFilter?.ToString() ?? (object)DBNull.Value 
                };
                
                // Exécution de la requête avec récupération directe des Businesses
                var nearbyBusinesses = await _context.Businesses
                    .FromSqlRaw(sql, parameters)
                    .Include(b => b.Creator)
                    .Include(b => b.Media.Where(m => m.IsPrimary))
                    .ToListAsync();

                return nearbyBusinesses;
            }
            catch (Exception ex)
            {
                // Log de l'erreur et fallback sur une requête EF Core classique
                System.Diagnostics.Debug.WriteLine($"Erreur PostGIS Businesses: {ex.Message}");
                
                // Fallback : requête EF Core avec calcul de distance approximatif
                return await _context.Businesses
                    .Include(b => b.Creator)
                    .Include(b => b.Media.Where(m => m.IsPrimary))
                    .Where(b => b.VerificationStatus == VerificationStatus.Verified)
                    .Where(b => typeFilter == null || b.BusinessType == typeFilter)
                    // Approximation : 1 degré ≈ 111km, donc radiusKm/111 degrés
                    .Where(b => Math.Abs((double)(b.Latitude - latitude)) <= (radiusKm / 111.0) && 
                               Math.Abs((double)(b.Longitude - longitude)) <= (radiusKm / 111.0))
                    .OrderBy(b => Math.Abs((double)(b.Latitude - latitude)) + Math.Abs((double)(b.Longitude - longitude)))
                    .Take(50) // Limiter les résultats
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<Business>> GetByBusinessTypeAsync(BusinessType businessType)
        {
            return await _context.Businesses
                .Include(b => b.Creator)
                .Include(b => b.Media.Where(m => m.IsPrimary))
                .Where(b => b.BusinessType == businessType && b.VerificationStatus == VerificationStatus.Verified)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Business>> GetByCreatorAsync(Guid creatorId)
        {
            return await _context.Businesses
                .Include(b => b.Media.Where(m => m.IsPrimary))
                .Where(b => b.CreatorId == creatorId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Business>> GetByVerificationStatusAsync(VerificationStatus status)
        {
            return await _context.Businesses
                .Include(b => b.Creator)
                .Include(b => b.Media.Where(m => m.IsPrimary))
                .Where(b => b.VerificationStatus == status)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Business>> SearchBusinessesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetAllAsync();

            string normalizedQuery = query.ToLower();

            return await _context.Businesses
                .Include(b => b.Creator)
                .Include(b => b.Media.Where(m => m.IsPrimary))
                .Where(b => (b.Name.ToLower().Contains(normalizedQuery) ||
                            (b.Description != null && b.Description.ToLower().Contains(normalizedQuery)) ||
                            b.City.ToLower().Contains(normalizedQuery)) &&
                            b.VerificationStatus == VerificationStatus.Verified)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> VerifyBusinessAsync(int businessId, Guid verifierId)
        {
            var business = await GetByIdAsync(businessId);
            if (business == null)
                return false;

            business.VerificationStatus = VerificationStatus.Verified;
            business.VerifiedBy = verifierId;
            business.VerifiedAt = DateTime.UtcNow;
            business.UpdatedAt = DateTime.UtcNow;

            await UpdateAsync(business);
            return true;
        }
    }
}