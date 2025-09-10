using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    public interface IPracticeSpotService
    {
        Task<IEnumerable<PracticeSpot>> GetNearbyPracticeSpotsAsync(decimal latitude, decimal longitude, int radiusKm = 10, Guid? spotTypeFilter = null);
        Task<IEnumerable<PracticeSpot>> GetPracticeSpotsByTypeAsync(Guid spotTypeId);
        Task<IEnumerable<PracticeSpot>> GetPracticeSpotsByDifficultyAsync(DifficultyLevel difficulty);
        Task<PracticeSpot?> GetPracticeSpotByIdAsync(int id);
        Task<PracticeSpot> CreatePracticeSpotAsync(PracticeSpot spot);
        Task<PracticeSpot> UpdatePracticeSpotAsync(PracticeSpot spot);
        Task<bool> DeletePracticeSpotAsync(int id);
        Task<IEnumerable<PracticeSpot>> SearchPracticeSpotsAsync(string query);
        Task<bool> ValidatePracticeSpotAsync(int spotId, Guid validatorId);
    }
}