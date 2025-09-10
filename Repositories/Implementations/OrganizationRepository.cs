using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;

namespace SubExplore.Repositories.Implementations
{
    public class OrganizationRepository : GenericRepository<Organization>, IOrganizationRepository
    {
        public OrganizationRepository(SubExploreDbContext context) : base(context)
        {
        }

        // APPROCHE HYBRIDE : Fonction PostGIS pour la géolocalisation
        public async Task<IEnumerable<Organization>> GetNearbyOrganizationsAsync(decimal latitude, decimal longitude, int radiusKm = 10, OrganizationType? typeFilter = null)
        {
            try
            {
                // Appel direct de la fonction PostGIS qui retourne les enregistrements complets
                var sql = "SELECT * FROM find_organizations_near_location({0}, {1}, {2}, {3})";
                
                // Gestion correcte des paramètres enum
                var parameters = new object[] { 
                    latitude, 
                    longitude, 
                    radiusKm, 
                    typeFilter?.ToString() ?? (object)DBNull.Value 
                };
                
                // Exécution de la requête avec récupération directe des Organizations
                var nearbyOrganizations = await _context.Organizations
                    .FromSqlRaw(sql, parameters)
                    .Include(o => o.Creator)
                    .Include(o => o.Media.Where(m => m.IsPrimary))
                    .ToListAsync();

                return nearbyOrganizations;
            }
            catch (Exception ex)
            {
                // Log de l'erreur et fallback sur une requête EF Core classique
                System.Diagnostics.Debug.WriteLine($"Erreur PostGIS Organizations: {ex.Message}");
                
                // Fallback : requête EF Core avec calcul de distance approximatif
                return await _context.Organizations
                    .Include(o => o.Creator)
                    .Include(o => o.Media.Where(m => m.IsPrimary))
                    .Where(o => o.VerificationStatus == VerificationStatus.Verified)
                    .Where(o => typeFilter == null || o.OrganizationType == typeFilter)
                    // Approximation : 1 degré ≈ 111km, donc radiusKm/111 degrés
                    .Where(o => Math.Abs((double)(o.Latitude - latitude)) <= (radiusKm / 111.0) && 
                               Math.Abs((double)(o.Longitude - longitude)) <= (radiusKm / 111.0))
                    .OrderBy(o => Math.Abs((double)(o.Latitude - latitude)) + Math.Abs((double)(o.Longitude - longitude)))
                    .Take(50) // Limiter les résultats
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<Organization>> GetByOrganizationTypeAsync(OrganizationType organizationType)
        {
            return await _context.Organizations
                .Include(o => o.Creator)
                .Include(o => o.Media.Where(m => m.IsPrimary))
                .Where(o => o.OrganizationType == organizationType && o.VerificationStatus == VerificationStatus.Verified)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Organization>> GetByCreatorAsync(Guid creatorId)
        {
            return await _context.Organizations
                .Include(o => o.Media.Where(m => m.IsPrimary))
                .Where(o => o.CreatorId == creatorId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Organization>> GetByVerificationStatusAsync(VerificationStatus status)
        {
            return await _context.Organizations
                .Include(o => o.Creator)
                .Include(o => o.Media.Where(m => m.IsPrimary))
                .Where(o => o.VerificationStatus == status)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Organization>> SearchOrganizationsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetAllAsync();

            string normalizedQuery = query.ToLower();

            return await _context.Organizations
                .Include(o => o.Creator)
                .Include(o => o.Media.Where(m => m.IsPrimary))
                .Where(o => (o.Name.ToLower().Contains(normalizedQuery) ||
                            (o.Description != null && o.Description.ToLower().Contains(normalizedQuery)) ||
                            o.City.ToLower().Contains(normalizedQuery)) &&
                            o.VerificationStatus == VerificationStatus.Verified)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> VerifyOrganizationAsync(int organizationId, Guid verifierId)
        {
            var organization = await GetByIdAsync(organizationId);
            if (organization == null)
                return false;

            organization.VerificationStatus = VerificationStatus.Verified;
            organization.VerifiedBy = verifierId;
            organization.VerifiedAt = DateTime.UtcNow;
            organization.UpdatedAt = DateTime.UtcNow;

            await UpdateAsync(organization);
            return true;
        }
    }
}