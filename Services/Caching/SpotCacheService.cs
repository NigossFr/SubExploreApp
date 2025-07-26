using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Caching
{
    /// <summary>
    /// Spot-specific caching implementation
    /// </summary>
    public class SpotCacheService : ISpotCacheService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<SpotCacheService> _logger;

        // Cache configuration
        private static readonly TimeSpan SpotCacheExpiration = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan AreaCacheExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan MediaCacheExpiration = TimeSpan.FromMinutes(60);

        // Cache key prefixes
        private const string SpotKeyPrefix = "spot:";
        private const string AreaKeyPrefix = "area:";
        private const string MediaKeyPrefix = "media:";

        public SpotCacheService(ICacheService cacheService, ILogger<SpotCacheService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Spot> GetSpotAsync(int spotId)
        {
            var key = GetSpotKey(spotId);
            var spot = await _cacheService.GetAsync<Spot>(key);
            
            if (spot != null)
            {
                _logger.LogTrace("Spot cache hit for ID: {SpotId}", spotId);
            }
            else
            {
                _logger.LogTrace("Spot cache miss for ID: {SpotId}", spotId);
            }
            
            return spot;
        }

        public async Task SetSpotAsync(Spot spot, TimeSpan? expiration = null)
        {
            if (spot == null) return;

            var key = GetSpotKey(spot.Id);
            var exp = expiration ?? SpotCacheExpiration;
            
            await _cacheService.SetAsync(key, spot, exp);
            _logger.LogTrace("Spot cached for ID: {SpotId}, expires in: {Expiration}", spot.Id, exp);
        }

        public async Task RemoveSpotAsync(int spotId)
        {
            var key = GetSpotKey(spotId);
            await _cacheService.RemoveAsync(key);
            _logger.LogTrace("Spot cache removed for ID: {SpotId}", spotId);
        }

        public async Task ClearSpotsAsync()
        {
            // Note: This is a simplified implementation
            // In a more sophisticated version, we'd track all spot keys
            await _cacheService.ClearAsync();
            _logger.LogInformation("All spot caches cleared");
        }

        public async Task<IEnumerable<Spot>> GetSpotsInAreaAsync(decimal latitude, decimal longitude, decimal radiusKm)
        {
            var key = GetAreaKey(latitude, longitude, radiusKm);
            var spots = await _cacheService.GetAsync<IEnumerable<Spot>>(key);
            
            if (spots != null)
            {
                _logger.LogTrace("Area cache hit for {Latitude}, {Longitude}, radius: {Radius}km", latitude, longitude, radiusKm);
            }
            else
            {
                _logger.LogTrace("Area cache miss for {Latitude}, {Longitude}, radius: {Radius}km", latitude, longitude, radiusKm);
            }
            
            return spots ?? Enumerable.Empty<Spot>();
        }

        public async Task SetSpotsInAreaAsync(decimal latitude, decimal longitude, decimal radiusKm, IEnumerable<Spot> spots, TimeSpan? expiration = null)
        {
            if (spots == null) return;

            var key = GetAreaKey(latitude, longitude, radiusKm);
            var exp = expiration ?? AreaCacheExpiration;
            var spotsList = spots.ToList();
            
            await _cacheService.SetAsync(key, spotsList, exp);
            _logger.LogTrace("Area cache set for {Latitude}, {Longitude}, radius: {Radius}km, {Count} spots, expires in: {Expiration}", 
                latitude, longitude, radiusKm, spotsList.Count, exp);

            // Also cache individual spots
            foreach (var spot in spotsList)
            {
                await SetSpotAsync(spot, exp);
            }
        }

        public async Task<IEnumerable<SpotMedia>> GetSpotMediaAsync(int spotId)
        {
            var key = GetMediaKey(spotId);
            var media = await _cacheService.GetAsync<IEnumerable<SpotMedia>>(key);
            
            if (media != null)
            {
                _logger.LogTrace("Media cache hit for spot ID: {SpotId}", spotId);
            }
            else
            {
                _logger.LogTrace("Media cache miss for spot ID: {SpotId}", spotId);
            }
            
            return media ?? Enumerable.Empty<SpotMedia>();
        }

        public async Task SetSpotMediaAsync(int spotId, IEnumerable<SpotMedia> media, TimeSpan? expiration = null)
        {
            if (media == null) return;

            var key = GetMediaKey(spotId);
            var exp = expiration ?? MediaCacheExpiration;
            var mediaList = media.ToList();
            
            await _cacheService.SetAsync(key, mediaList, exp);
            _logger.LogTrace("Media cache set for spot ID: {SpotId}, {Count} items, expires in: {Expiration}", 
                spotId, mediaList.Count, exp);
        }

        public async Task RemoveSpotMediaAsync(int spotId)
        {
            var key = GetMediaKey(spotId);
            await _cacheService.RemoveAsync(key);
            _logger.LogTrace("Media cache removed for spot ID: {SpotId}", spotId);
        }

        public async Task InvalidateSpotCache(int spotId)
        {
            await RemoveSpotAsync(spotId);
            await RemoveSpotMediaAsync(spotId);
            _logger.LogDebug("Invalidated all cache for spot ID: {SpotId}", spotId);
        }

        public async Task InvalidateAreaCache(decimal latitude, decimal longitude, decimal radiusKm)
        {
            var key = GetAreaKey(latitude, longitude, radiusKm);
            await _cacheService.RemoveAsync(key);
            _logger.LogDebug("Invalidated area cache for {Latitude}, {Longitude}, radius: {Radius}km", 
                latitude, longitude, radiusKm);
        }

        // Private helper methods
        private static string GetSpotKey(int spotId) => $"{SpotKeyPrefix}{spotId}";
        
        private static string GetMediaKey(int spotId) => $"{MediaKeyPrefix}{spotId}";
        
        private static string GetAreaKey(decimal latitude, decimal longitude, decimal radiusKm)
        {
            // Round coordinates to reduce cache fragmentation
            var roundedLat = Math.Round(latitude, 4);
            var roundedLng = Math.Round(longitude, 4);
            var roundedRadius = Math.Round(radiusKm, 1);
            return $"{AreaKeyPrefix}{roundedLat}:{roundedLng}:{roundedRadius}";
        }
    }
}