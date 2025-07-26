using System;
using System.Threading.Tasks;

namespace SubExplore.Services.Caching
{
    /// <summary>
    /// Generic cache service interface
    /// </summary>
    public interface ICacheService
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task ClearAsync();
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        bool Exists(string key);
        void ClearMemoryCache();
    }

    /// <summary>
    /// Spot-specific caching service
    /// </summary>
    public interface ISpotCacheService
    {
        Task<Models.Domain.Spot> GetSpotAsync(int spotId);
        Task SetSpotAsync(Models.Domain.Spot spot, TimeSpan? expiration = null);
        Task RemoveSpotAsync(int spotId);
        Task ClearSpotsAsync();
        
        Task<IEnumerable<Models.Domain.Spot>> GetSpotsInAreaAsync(decimal latitude, decimal longitude, decimal radiusKm);
        Task SetSpotsInAreaAsync(decimal latitude, decimal longitude, decimal radiusKm, IEnumerable<Models.Domain.Spot> spots, TimeSpan? expiration = null);
        
        Task<IEnumerable<Models.Domain.SpotMedia>> GetSpotMediaAsync(int spotId);
        Task SetSpotMediaAsync(int spotId, IEnumerable<Models.Domain.SpotMedia> media, TimeSpan? expiration = null);
        Task RemoveSpotMediaAsync(int spotId);
        
        Task InvalidateSpotCache(int spotId);
        Task InvalidateAreaCache(decimal latitude, decimal longitude, decimal radiusKm);
    }
}