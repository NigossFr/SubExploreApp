using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    public interface IOrganizationService
    {
        Task<IEnumerable<Organization>> GetNearbyOrganizationsAsync(decimal latitude, decimal longitude, int radiusKm = 10, OrganizationType? typeFilter = null);
        Task<IEnumerable<Organization>> GetOrganizationsByTypeAsync(OrganizationType organizationType);
        Task<Organization?> GetOrganizationByIdAsync(int id);
        Task<Organization> CreateOrganizationAsync(Organization organization);
        Task<Organization> UpdateOrganizationAsync(Organization organization);
        Task<bool> DeleteOrganizationAsync(int id);
        Task<IEnumerable<Organization>> SearchOrganizationsAsync(string query);
        Task<bool> VerifyOrganizationAsync(int organizationId, Guid verifierId);
    }
}