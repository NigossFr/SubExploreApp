using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace SubExplore.Services.Implementations
{
    public class PracticeSpotService : IPracticeSpotService
    {
        private readonly IPracticeSpotRepository _practiceSpotRepository;
        private readonly ILogger<PracticeSpotService> _logger;

        public PracticeSpotService(IPracticeSpotRepository practiceSpotRepository, ILogger<PracticeSpotService> logger)
        {
            _practiceSpotRepository = practiceSpotRepository;
            _logger = logger;
        }

        // GÉOLOCALISATION HYBRIDE avec PostGIS
        public async Task<IEnumerable<PracticeSpot>> GetNearbyPracticeSpotsAsync(decimal latitude, decimal longitude, int radiusKm = 10, Guid? spotTypeFilter = null)
        {
            try
            {
                return await _practiceSpotRepository.GetNearbyPracticeSpotsAsync(latitude, longitude, radiusKm, spotTypeFilter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nearby practice spots");
                throw;
            }
        }

        public async Task<IEnumerable<PracticeSpot>> GetPracticeSpotsByTypeAsync(Guid spotTypeId)
        {
            try
            {
                return await _practiceSpotRepository.GetBySpotTypeAsync(spotTypeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting practice spots by type {SpotTypeId}", spotTypeId);
                throw;
            }
        }

        public async Task<IEnumerable<PracticeSpot>> GetPracticeSpotsByDifficultyAsync(DifficultyLevel difficulty)
        {
            try
            {
                return await _practiceSpotRepository.GetByDifficultyLevelAsync(difficulty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting practice spots by difficulty {Difficulty}", difficulty);
                throw;
            }
        }

        public async Task<PracticeSpot?> GetPracticeSpotByIdAsync(int id)
        {
            try
            {
                return await _practiceSpotRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting practice spot by id {Id}", id);
                throw;
            }
        }

        public async Task<PracticeSpot> CreatePracticeSpotAsync(PracticeSpot spot)
        {
            try
            {
                spot.CreatedAt = DateTime.UtcNow;
                spot.UpdatedAt = DateTime.UtcNow;
                spot.ValidationStatus = SpotValidationStatus.Pending;

                await _practiceSpotRepository.AddAsync(spot);
                await _practiceSpotRepository.SaveChangesAsync();

                return spot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating practice spot");
                throw;
            }
        }

        public async Task<PracticeSpot> UpdatePracticeSpotAsync(PracticeSpot spot)
        {
            try
            {
                spot.UpdatedAt = DateTime.UtcNow;

                await _practiceSpotRepository.UpdateAsync(spot);
                await _practiceSpotRepository.SaveChangesAsync();

                return spot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating practice spot {Id}", spot.Id);
                throw;
            }
        }

        public async Task<bool> DeletePracticeSpotAsync(int id)
        {
            try
            {
                var spot = await _practiceSpotRepository.GetByIdAsync(id);
                if (spot == null)
                    return false;

                await _practiceSpotRepository.DeleteAsync(spot);
                await _practiceSpotRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting practice spot {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<PracticeSpot>> SearchPracticeSpotsAsync(string query)
        {
            try
            {
                return await _practiceSpotRepository.SearchPracticeSpotsAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching practice spots with query {Query}", query);
                throw;
            }
        }

        public async Task<bool> ValidatePracticeSpotAsync(int spotId, Guid validatorId)
        {
            try
            {
                var result = await _practiceSpotRepository.ValidateSpotAsync(spotId, validatorId);
                if (result)
                {
                    await _practiceSpotRepository.SaveChangesAsync();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating practice spot {SpotId}", spotId);
                throw;
            }
        }
    }
}