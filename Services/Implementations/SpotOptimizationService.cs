using Microsoft.Extensions.Logging;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// High-performance spot optimization service with intelligent caching
    /// Eliminates conversion overhead and provides optimized data structures
    /// </summary>
    public class SpotOptimizationService : ISpotOptimizationService
    {
        #region Fields & Constants
        
        private readonly ILogger<SpotOptimizationService> _logger;
        private readonly ConcurrentDictionary<int, OptimizedSpot> _spotCache = new();
        private readonly ConcurrentDictionary<int, Pin> _pinCache = new();
        private readonly CacheStatistics _cacheStats = new();
        private readonly object _statsLock = new();
        
        private bool _cachingEnabled = true;
        private const int MAX_CACHE_SIZE = 1000;
        private const int CLEANUP_THRESHOLD = 1200;
        
        #endregion
        
        #region Constructor
        
        public SpotOptimizationService(ILogger<SpotOptimizationService> logger)
        {
            _logger = logger;
        }
        
        #endregion
        
        #region Transformation Methods
        
        public OptimizedSpot ToOptimized(Spot spot)
        {
            if (spot == null) throw new ArgumentNullException(nameof(spot));
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Check cache first if enabled
                if (_cachingEnabled && _spotCache.TryGetValue(spot.Id, out var cachedSpot))
                {
                    RecordCacheHit(stopwatch.ElapsedTicks);
                    return cachedSpot;
                }
                
                // Transform to optimized structure
                var optimizedSpot = OptimizedSpot.FromEntity(spot);
                
                // Cache result if caching is enabled
                if (_cachingEnabled)
                {
                    CacheSpot(spot.Id, optimizedSpot);
                }
                
                RecordCacheMiss(stopwatch.ElapsedTicks);
                return optimizedSpot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming spot {SpotId} to optimized structure", spot.Id);
                throw;
            }
        }
        
        public IEnumerable<OptimizedSpot> ToOptimized(IEnumerable<Spot> spots)
        {
            if (spots == null) throw new ArgumentNullException(nameof(spots));
            
            var stopwatch = Stopwatch.StartNew();
            var spotList = spots.ToList();
            
            try
            {
                var results = new List<OptimizedSpot>(spotList.Count);
                var cacheHits = 0;
                var cacheMisses = 0;
                
                foreach (var spot in spotList)
                {
                    if (_cachingEnabled && _spotCache.TryGetValue(spot.Id, out var cachedSpot))
                    {
                        results.Add(cachedSpot);
                        cacheHits++;
                    }
                    else
                    {
                        var optimizedSpot = OptimizedSpot.FromEntity(spot);
                        results.Add(optimizedSpot);
                        
                        if (_cachingEnabled)
                        {
                            CacheSpot(spot.Id, optimizedSpot);
                        }
                        
                        cacheMisses++;
                    }
                }
                
                // Record batch statistics
                lock (_statsLock)
                {
                    _cacheStats.TotalRequests += spotList.Count;
                    _cacheStats.CacheHits += cacheHits;
                    _cacheStats.CacheMisses += cacheMisses;
                    UpdateAverageTime(stopwatch.ElapsedTicks);
                }
                
                _logger.LogDebug("Transformed {Count} spots ({Hits} cached, {Misses} new) in {ElapsedMs}ms",
                    spotList.Count, cacheHits, cacheMisses, stopwatch.ElapsedMilliseconds);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming {Count} spots to optimized structures", spotList.Count);
                throw;
            }
        }
        
        public Spot ToEntity(OptimizedSpot optimizedSpot)
        {
            try
            {
                return optimizedSpot.ToEntity();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting optimized spot {SpotId} to entity", optimizedSpot.Id);
                throw;
            }
        }
        
        #endregion
        
        #region Pin Management
        
        public IEnumerable<Pin> CreateMapPins(IEnumerable<OptimizedSpot> spots)
        {
            if (spots == null) throw new ArgumentNullException(nameof(spots));
            
            var stopwatch = Stopwatch.StartNew();
            var spotList = spots.ToList();
            
            try
            {
                var pins = new List<Pin>(spotList.Count);
                
                foreach (var spot in spotList)
                {
                    var pin = CreateMapPin(spot);
                    pins.Add(pin);
                }
                
                _logger.LogDebug("Created {Count} map pins in {ElapsedMs}ms",
                    pins.Count, stopwatch.ElapsedMilliseconds);
                
                return pins;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating {Count} map pins", spotList.Count);
                throw;
            }
        }
        
        public Pin CreateMapPin(OptimizedSpot spot)
        {
            try
            {
                // Check pin cache first if enabled
                if (_cachingEnabled && _pinCache.TryGetValue(spot.Id, out var cachedPin))
                {
                    // Update binding context to ensure it's current
                    cachedPin.BindingContext = spot;
                    return cachedPin;
                }
                
                // Create new pin
                var pin = spot.CreateMapPin();
                
                // Cache pin if caching is enabled
                if (_cachingEnabled)
                {
                    CachePin(spot.Id, pin);
                }
                
                return pin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating map pin for spot {SpotId}", spot.Id);
                throw;
            }
        }
        
        public OptimizedSpot? ExtractSpotFromPin(Pin pin)
        {
            try
            {
                return pin?.BindingContext as OptimizedSpot?;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting spot from pin binding context");
                return null;
            }
        }
        
        #endregion
        
        #region Spatial Operations
        
        public IEnumerable<OptimizedSpot> FindSpotsInRadius(
            IEnumerable<OptimizedSpot> spots, 
            SpotCoordinate center, 
            double radiusKm)
        {
            if (spots == null) throw new ArgumentNullException(nameof(spots));
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var results = spots.WithinRadius(center, radiusKm).ToList();
                
                _logger.LogDebug("Found {Count} spots within {Radius}km in {ElapsedMs}ms",
                    results.Count, radiusKm, stopwatch.ElapsedMilliseconds);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding spots in radius {Radius}km", radiusKm);
                throw;
            }
        }
        
        public OptimizedSpot? FindNearestSpot(IEnumerable<OptimizedSpot> spots, SpotCoordinate location)
        {
            if (spots == null) throw new ArgumentNullException(nameof(spots));
            
            try
            {
                return spots
                    .OrderBy(spot => spot.Coordinate.DistanceToKm(location))
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding nearest spot to location {Location}", location);
                throw;
            }
        }
        
        public MapSpan? CalculateBoundingBox(IEnumerable<OptimizedSpot> spots)
        {
            if (spots == null) throw new ArgumentNullException(nameof(spots));
            
            try
            {
                var spotList = spots.ToList();
                if (!spotList.Any()) return null;
                
                var minLat = spotList.Min(s => s.Coordinate.LatitudeDouble);
                var maxLat = spotList.Max(s => s.Coordinate.LatitudeDouble);
                var minLon = spotList.Min(s => s.Coordinate.LongitudeDouble);
                var maxLon = spotList.Max(s => s.Coordinate.LongitudeDouble);
                
                var centerLat = (minLat + maxLat) / 2;
                var centerLon = (minLon + maxLon) / 2;
                var latSpan = Math.Max(maxLat - minLat, 0.01); // Min span for single spot
                var lonSpan = Math.Max(maxLon - minLon, 0.01);
                
                return new MapSpan(new Location(centerLat, centerLon), latSpan * 1.2, lonSpan * 1.2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating bounding box for spots");
                throw;
            }
        }
        
        #endregion
        
        #region Search & Filtering
        
        public IEnumerable<OptimizedSpot> SearchSpots(
            IEnumerable<OptimizedSpot> spots,
            string? searchText = null,
            DifficultyLevel? difficultyFilter = null,
            SpotValidationStatus? validationFilter = null)
        {
            if (spots == null) throw new ArgumentNullException(nameof(spots));
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var results = spots.Search(searchText, difficultyFilter, validationFilter).ToList();
                
                _logger.LogDebug("Filtered {ResultCount} spots from {TotalCount} in {ElapsedMs}ms",
                    results.Count, spots.Count(), stopwatch.ElapsedMilliseconds);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching spots with text '{SearchText}'", searchText);
                throw;
            }
        }
        
        public IEnumerable<OptimizedSpot> GetSpotsOrderedByScore(
            IEnumerable<OptimizedSpot> spots,
            SpotCoordinate clickLocation,
            double maxDistance)
        {
            if (spots == null) throw new ArgumentNullException(nameof(spots));
            
            try
            {
                return spots
                    .Select(spot => new { Spot = spot, Score = spot.CalculateSelectionScore(clickLocation, maxDistance) })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .Select(x => x.Spot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ordering spots by selection score");
                throw;
            }
        }
        
        #endregion
        
        #region Caching & Performance
        
        public void SetCachingEnabled(bool enabled)
        {
            _cachingEnabled = enabled;
            _logger.LogInformation("Caching {Status}", enabled ? "enabled" : "disabled");
            
            if (!enabled)
            {
                ClearCaches();
            }
        }
        
        public void ClearCaches()
        {
            var spotCount = _spotCache.Count;
            var pinCount = _pinCache.Count;
            
            _spotCache.Clear();
            _pinCache.Clear();
            
            lock (_statsLock)
            {
                _cacheStats.TotalRequests = 0;
                _cacheStats.CacheHits = 0;
                _cacheStats.CacheMisses = 0;
                _cacheStats.MemoryUsageBytes = 0;
            }
            
            _logger.LogInformation("Cleared caches: {SpotCount} spots, {PinCount} pins", spotCount, pinCount);
        }
        
        public CacheStatistics GetCacheStatistics()
        {
            lock (_statsLock)
            {
                // Estimate memory usage
                var spotMemory = _spotCache.Count * EstimateSpotMemoryUsage();
                var pinMemory = _pinCache.Count * EstimatePinMemoryUsage();
                
                return new CacheStatistics
                {
                    TotalRequests = _cacheStats.TotalRequests,
                    CacheHits = _cacheStats.CacheHits,
                    CacheMisses = _cacheStats.CacheMisses,
                    MemoryUsageBytes = spotMemory + pinMemory,
                    AverageTransformTime = _cacheStats.AverageTransformTime
                };
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void CacheSpot(int spotId, OptimizedSpot spot)
        {
            try
            {
                _spotCache.AddOrUpdate(spotId, spot, (key, existing) => spot);
                
                // Cleanup if cache gets too large
                if (_spotCache.Count > CLEANUP_THRESHOLD)
                {
                    CleanupSpotCache();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error caching spot {SpotId}", spotId);
            }
        }
        
        private void CachePin(int spotId, Pin pin)
        {
            try
            {
                _pinCache.AddOrUpdate(spotId, pin, (key, existing) => pin);
                
                // Cleanup if cache gets too large
                if (_pinCache.Count > CLEANUP_THRESHOLD)
                {
                    CleanupPinCache();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error caching pin for spot {SpotId}", spotId);
            }
        }
        
        private void CleanupSpotCache()
        {
            try
            {
                if (_spotCache.Count <= MAX_CACHE_SIZE) return;
                
                var toRemove = _spotCache.Count - MAX_CACHE_SIZE;
                var keysToRemove = _spotCache.Keys.Take(toRemove).ToList();
                
                foreach (var key in keysToRemove)
                {
                    _spotCache.TryRemove(key, out _);
                }
                
                _logger.LogDebug("Cleaned up spot cache, removed {Count} entries", keysToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during spot cache cleanup");
            }
        }
        
        private void CleanupPinCache()
        {
            try
            {
                if (_pinCache.Count <= MAX_CACHE_SIZE) return;
                
                var toRemove = _pinCache.Count - MAX_CACHE_SIZE;
                var keysToRemove = _pinCache.Keys.Take(toRemove).ToList();
                
                foreach (var key in keysToRemove)
                {
                    _pinCache.TryRemove(key, out _);
                }
                
                _logger.LogDebug("Cleaned up pin cache, removed {Count} entries", keysToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during pin cache cleanup");
            }
        }
        
        private void RecordCacheHit(long elapsedTicks)
        {
            lock (_statsLock)
            {
                _cacheStats.TotalRequests++;
                _cacheStats.CacheHits++;
                UpdateAverageTime(elapsedTicks);
            }
        }
        
        private void RecordCacheMiss(long elapsedTicks)
        {
            lock (_statsLock)
            {
                _cacheStats.TotalRequests++;
                _cacheStats.CacheMisses++;
                UpdateAverageTime(elapsedTicks);
            }
        }
        
        private void UpdateAverageTime(long elapsedTicks)
        {
            var elapsedMs = TimeSpan.FromTicks(elapsedTicks);
            
            if (_cacheStats.AverageTransformTime == TimeSpan.Zero)
            {
                _cacheStats.AverageTransformTime = elapsedMs;
            }
            else
            {
                // Simple moving average
                var currentAvg = _cacheStats.AverageTransformTime.TotalMilliseconds;
                var newAvg = (currentAvg * 0.9) + (elapsedMs.TotalMilliseconds * 0.1);
                _cacheStats.AverageTransformTime = TimeSpan.FromMilliseconds(newAvg);
            }
        }
        
        private static long EstimateSpotMemoryUsage()
        {
            // Rough estimate: struct size + strings
            return 200; // bytes per OptimizedSpot
        }
        
        private static long EstimatePinMemoryUsage()
        {
            // Rough estimate: Pin object + properties
            return 300; // bytes per Pin
        }
        
        #endregion
    }
}