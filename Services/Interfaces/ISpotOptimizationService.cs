using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for optimizing spot data transformations and caching
    /// Provides high-performance conversion between entity models and optimized structures
    /// </summary>
    public interface ISpotOptimizationService
    {
        #region Transformation Methods
        
        /// <summary>
        /// Convert Entity Framework Spot to optimized structure
        /// </summary>
        /// <param name="spot">EF Spot entity</param>
        /// <returns>Optimized spot structure</returns>
        OptimizedSpot ToOptimized(Spot spot);
        
        /// <summary>
        /// Convert collection of EF Spots to optimized structures
        /// </summary>
        /// <param name="spots">Collection of EF Spot entities</param>
        /// <returns>Collection of optimized spots</returns>
        IEnumerable<OptimizedSpot> ToOptimized(IEnumerable<Spot> spots);
        
        /// <summary>
        /// Convert optimized spot back to EF entity
        /// </summary>
        /// <param name="optimizedSpot">Optimized spot structure</param>
        /// <returns>EF-compatible Spot entity</returns>
        Spot ToEntity(OptimizedSpot optimizedSpot);
        
        #endregion
        
        #region Pin Management
        
        /// <summary>
        /// Create map pins from optimized spots with caching
        /// </summary>
        /// <param name="spots">Collection of optimized spots</param>
        /// <returns>Collection of map pins ready for display</returns>
        IEnumerable<Pin> CreateMapPins(IEnumerable<OptimizedSpot> spots);
        
        /// <summary>
        /// Create single map pin from optimized spot
        /// </summary>
        /// <param name="spot">Optimized spot</param>
        /// <returns>Map pin ready for display</returns>
        Pin CreateMapPin(OptimizedSpot spot);
        
        /// <summary>
        /// Extract optimized spot from pin binding context
        /// </summary>
        /// <param name="pin">Map pin with binding context</param>
        /// <returns>Optimized spot if found, null otherwise</returns>
        OptimizedSpot? ExtractSpotFromPin(Pin pin);
        
        #endregion
        
        #region Spatial Operations
        
        /// <summary>
        /// Find spots within radius of a coordinate
        /// </summary>
        /// <param name="spots">Collection of spots to search</param>
        /// <param name="center">Center coordinate</param>
        /// <param name="radiusKm">Search radius in kilometers</param>
        /// <returns>Spots within radius, ordered by distance</returns>
        IEnumerable<OptimizedSpot> FindSpotsInRadius(
            IEnumerable<OptimizedSpot> spots, 
            SpotCoordinate center, 
            double radiusKm);
        
        /// <summary>
        /// Find nearest spot to a coordinate
        /// </summary>
        /// <param name="spots">Collection of spots to search</param>
        /// <param name="location">Target coordinate</param>
        /// <returns>Nearest spot or null if none found</returns>
        OptimizedSpot? FindNearestSpot(IEnumerable<OptimizedSpot> spots, SpotCoordinate location);
        
        /// <summary>
        /// Calculate bounding box for a collection of spots
        /// </summary>
        /// <param name="spots">Collection of spots</param>
        /// <returns>MapSpan covering all spots</returns>
        MapSpan? CalculateBoundingBox(IEnumerable<OptimizedSpot> spots);
        
        #endregion
        
        #region Search & Filtering
        
        /// <summary>
        /// Search spots by text and filters
        /// </summary>
        /// <param name="spots">Collection of spots to search</param>
        /// <param name="searchText">Text to search in name/description</param>
        /// <param name="difficultyFilter">Optional difficulty filter</param>
        /// <param name="validationFilter">Optional validation status filter</param>
        /// <returns>Filtered spots</returns>
        IEnumerable<OptimizedSpot> SearchSpots(
            IEnumerable<OptimizedSpot> spots,
            string? searchText = null,
            DifficultyLevel? difficultyFilter = null,
            SpotValidationStatus? validationFilter = null);
        
        /// <summary>
        /// Get spots sorted by selection score for a location
        /// </summary>
        /// <param name="spots">Collection of spots</param>
        /// <param name="clickLocation">User click location</param>
        /// <param name="maxDistance">Maximum search distance</param>
        /// <returns>Spots ordered by selection score (best first)</returns>
        IEnumerable<OptimizedSpot> GetSpotsOrderedByScore(
            IEnumerable<OptimizedSpot> spots,
            SpotCoordinate clickLocation,
            double maxDistance);
        
        #endregion
        
        #region Caching & Performance
        
        /// <summary>
        /// Enable/disable internal caching for transformations
        /// </summary>
        /// <param name="enabled">True to enable caching</param>
        void SetCachingEnabled(bool enabled);
        
        /// <summary>
        /// Clear internal caches to free memory
        /// </summary>
        void ClearCaches();
        
        /// <summary>
        /// Get cache statistics for monitoring
        /// </summary>
        /// <returns>Cache performance statistics</returns>
        CacheStatistics GetCacheStatistics();
        
        #endregion
    }
    
    /// <summary>
    /// Cache performance statistics
    /// </summary>
    public class CacheStatistics
    {
        public int TotalRequests { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
        public long MemoryUsageBytes { get; set; }
        public TimeSpan AverageTransformTime { get; set; }
        
        public double HitRatio => TotalRequests > 0 ? (double)CacheHits / TotalRequests : 0.0;
        public double MissRatio => TotalRequests > 0 ? (double)CacheMisses / TotalRequests : 0.0;
    }
}