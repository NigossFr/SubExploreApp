using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    public interface IBusinessService
    {
        Task<IEnumerable<Business>> GetNearbyBusinessesAsync(decimal latitude, decimal longitude, int radiusKm = 10, BusinessType? typeFilter = null);
        Task<IEnumerable<Business>> GetBusinessesByTypeAsync(BusinessType businessType);
        Task<Business?> GetBusinessByIdAsync(int id);
        Task<Business> CreateBusinessAsync(Business business);
        Task<Business> UpdateBusinessAsync(Business business);
        Task<bool> DeleteBusinessAsync(int id);
        Task<IEnumerable<Business>> SearchBusinessesAsync(string query);
        Task<bool> VerifyBusinessAsync(int businessId, Guid verifierId);
    }
}