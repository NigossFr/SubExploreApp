using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Repositories.Interfaces
{
    public interface ISpotRepository : IGenericRepository<Spot>
    {
        Task<IEnumerable<Spot>> GetNearbySpots(decimal latitude, decimal longitude, double radiusInKm, int limit = 50);
        Task<IEnumerable<Spot>> GetSpotsByTypeAsync(int typeId);
        Task<IEnumerable<Spot>> GetSpotsByCategoryAsync(ActivityCategory category);
        Task<IEnumerable<Spot>> GetSpotsByUserAsync(int userId);
        Task<IEnumerable<Spot>> GetSpotsByValidationStatusAsync(SpotValidationStatus status);
        Task<IEnumerable<Spot>> SearchSpotsAsync(string query);
        Task<IEnumerable<Spot>> SearchSpotsWithLocationAsync(string query, decimal? userLatitude = null, decimal? userLongitude = null, double radiusKm = 50);
        Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query, int limit = 5);
        Task<IEnumerable<Spot>> GetSpotsMinimalAsync(int limit = 100, CancellationToken cancellationToken = default);
        Task<IEnumerable<Spot>> GetSpotsByMultipleCategoriesAsync(ActivityCategory[] categories);
        Task<Dictionary<ActivityCategory, int>> GetSpotCountsByCategoryAsync();
    }
}
