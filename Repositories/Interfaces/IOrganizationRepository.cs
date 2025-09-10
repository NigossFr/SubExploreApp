using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Repositories.Interfaces
{
    public interface IOrganizationRepository : IGenericRepository<Organization>
    {
        // Approche HYBRIDE : Utilise les fonctions PostGIS
        Task<IEnumerable<Organization>> GetNearbyOrganizationsAsync(decimal latitude, decimal longitude, int radiusKm = 10, OrganizationType? typeFilter = null);
        
        // Méthodes classiques en C#
        Task<IEnumerable<Organization>> GetByOrganizationTypeAsync(OrganizationType organizationType);
        Task<IEnumerable<Organization>> GetByCreatorAsync(Guid creatorId);
        Task<IEnumerable<Organization>> GetByVerificationStatusAsync(VerificationStatus status);
        Task<IEnumerable<Organization>> SearchOrganizationsAsync(string query);
        Task<bool> VerifyOrganizationAsync(int organizationId, Guid verifierId);
    }
}