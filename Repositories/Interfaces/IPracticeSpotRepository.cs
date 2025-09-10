using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Repositories.Interfaces
{
    public interface IPracticeSpotRepository : IGenericRepository<PracticeSpot>
    {
        // Approche HYBRIDE : Utilise les fonctions PostGIS
        Task<IEnumerable<PracticeSpot>> GetNearbyPracticeSpotsAsync(decimal latitude, decimal longitude, int radiusKm = 10, Guid? spotTypeFilter = null);
        
        // Méthodes classiques en C#
        Task<IEnumerable<PracticeSpot>> GetBySpotTypeAsync(Guid spotTypeId);
        Task<IEnumerable<PracticeSpot>> GetByDifficultyLevelAsync(DifficultyLevel difficulty);
        Task<IEnumerable<PracticeSpot>> GetByCreatorAsync(Guid creatorId);
        Task<IEnumerable<PracticeSpot>> GetByValidationStatusAsync(SpotValidationStatus status);
        Task<IEnumerable<PracticeSpot>> SearchPracticeSpotsAsync(string query);
        Task<bool> ValidateSpotAsync(int spotId, Guid validatorId);
    }
}