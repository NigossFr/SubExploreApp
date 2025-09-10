using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Repositories.Interfaces
{
    public interface IBusinessRepository : IGenericRepository<Business>
    {
        // Approche HYBRIDE : Utilise les fonctions PostGIS
        Task<IEnumerable<Business>> GetNearbyBusinessesAsync(decimal latitude, decimal longitude, int radiusKm = 10, BusinessType? typeFilter = null);
        
        // Méthodes classiques en C#
        Task<IEnumerable<Business>> GetByBusinessTypeAsync(BusinessType businessType);
        Task<IEnumerable<Business>> GetByCreatorAsync(Guid creatorId);
        Task<IEnumerable<Business>> GetByVerificationStatusAsync(VerificationStatus status);
        Task<IEnumerable<Business>> SearchBusinessesAsync(string query);
        Task<bool> VerifyBusinessAsync(int businessId, Guid verifierId);
    }
}