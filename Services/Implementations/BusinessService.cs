using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace SubExplore.Services.Implementations
{
    public class BusinessService : IBusinessService
    {
        private readonly IBusinessRepository _businessRepository;
        private readonly ILogger<BusinessService> _logger;

        public BusinessService(IBusinessRepository businessRepository, ILogger<BusinessService> logger)
        {
            _businessRepository = businessRepository;
            _logger = logger;
        }

        // GÉOLOCALISATION HYBRIDE avec PostGIS
        public async Task<IEnumerable<Business>> GetNearbyBusinessesAsync(decimal latitude, decimal longitude, int radiusKm = 10, BusinessType? typeFilter = null)
        {
            try
            {
                return await _businessRepository.GetNearbyBusinessesAsync(latitude, longitude, radiusKm, typeFilter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nearby businesses");
                throw;
            }
        }

        public async Task<IEnumerable<Business>> GetBusinessesByTypeAsync(BusinessType businessType)
        {
            try
            {
                return await _businessRepository.GetByBusinessTypeAsync(businessType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting businesses by type {BusinessType}", businessType);
                throw;
            }
        }

        public async Task<Business?> GetBusinessByIdAsync(int id)
        {
            try
            {
                return await _businessRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting business by id {Id}", id);
                throw;
            }
        }

        public async Task<Business> CreateBusinessAsync(Business business)
        {
            try
            {
                business.CreatedAt = DateTime.UtcNow;
                business.UpdatedAt = DateTime.UtcNow;
                business.VerificationStatus = VerificationStatus.Pending;

                await _businessRepository.AddAsync(business);
                await _businessRepository.SaveChangesAsync();

                return business;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating business");
                throw;
            }
        }

        public async Task<Business> UpdateBusinessAsync(Business business)
        {
            try
            {
                business.UpdatedAt = DateTime.UtcNow;

                await _businessRepository.UpdateAsync(business);
                await _businessRepository.SaveChangesAsync();

                return business;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating business {Id}", business.Id);
                throw;
            }
        }

        public async Task<bool> DeleteBusinessAsync(int id)
        {
            try
            {
                var business = await _businessRepository.GetByIdAsync(id);
                if (business == null)
                    return false;

                await _businessRepository.DeleteAsync(business);
                await _businessRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting business {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Business>> SearchBusinessesAsync(string query)
        {
            try
            {
                return await _businessRepository.SearchBusinessesAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching businesses with query {Query}", query);
                throw;
            }
        }

        public async Task<bool> VerifyBusinessAsync(int businessId, Guid verifierId)
        {
            try
            {
                var result = await _businessRepository.VerifyBusinessAsync(businessId, verifierId);
                if (result)
                {
                    await _businessRepository.SaveChangesAsync();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying business {BusinessId}", businessId);
                throw;
            }
        }
    }
}