using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace SubExplore.Services.Implementations
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ILogger<OrganizationService> _logger;

        public OrganizationService(IOrganizationRepository organizationRepository, ILogger<OrganizationService> logger)
        {
            _organizationRepository = organizationRepository;
            _logger = logger;
        }

        // GÉOLOCALISATION HYBRIDE avec PostGIS
        public async Task<IEnumerable<Organization>> GetNearbyOrganizationsAsync(decimal latitude, decimal longitude, int radiusKm = 10, OrganizationType? typeFilter = null)
        {
            try
            {
                return await _organizationRepository.GetNearbyOrganizationsAsync(latitude, longitude, radiusKm, typeFilter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nearby organizations");
                throw;
            }
        }

        public async Task<IEnumerable<Organization>> GetOrganizationsByTypeAsync(OrganizationType organizationType)
        {
            try
            {
                return await _organizationRepository.GetByOrganizationTypeAsync(organizationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organizations by type {OrganizationType}", organizationType);
                throw;
            }
        }

        public async Task<Organization?> GetOrganizationByIdAsync(int id)
        {
            try
            {
                return await _organizationRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organization by id {Id}", id);
                throw;
            }
        }

        public async Task<Organization> CreateOrganizationAsync(Organization organization)
        {
            try
            {
                organization.CreatedAt = DateTime.UtcNow;
                organization.UpdatedAt = DateTime.UtcNow;
                organization.VerificationStatus = VerificationStatus.Pending;

                await _organizationRepository.AddAsync(organization);
                await _organizationRepository.SaveChangesAsync();

                return organization;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organization");
                throw;
            }
        }

        public async Task<Organization> UpdateOrganizationAsync(Organization organization)
        {
            try
            {
                organization.UpdatedAt = DateTime.UtcNow;

                await _organizationRepository.UpdateAsync(organization);
                await _organizationRepository.SaveChangesAsync();

                return organization;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization {Id}", organization.Id);
                throw;
            }
        }

        public async Task<bool> DeleteOrganizationAsync(int id)
        {
            try
            {
                var organization = await _organizationRepository.GetByIdAsync(id);
                if (organization == null)
                    return false;

                await _organizationRepository.DeleteAsync(organization);
                await _organizationRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting organization {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Organization>> SearchOrganizationsAsync(string query)
        {
            try
            {
                return await _organizationRepository.SearchOrganizationsAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching organizations with query {Query}", query);
                throw;
            }
        }

        public async Task<bool> VerifyOrganizationAsync(int organizationId, Guid verifierId)
        {
            try
            {
                var result = await _organizationRepository.VerifyOrganizationAsync(organizationId, verifierId);
                if (result)
                {
                    await _organizationRepository.SaveChangesAsync();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying organization {OrganizationId}", organizationId);
                throw;
            }
        }
    }
}