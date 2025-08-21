using System;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Cache levels with different lifetimes and storage strategies
    /// </summary>
    public enum CacheLevel
    {
        /// <summary>Memory cache - fastest, limited capacity, process lifetime</summary>
        Memory,
        
        /// <summary>Distributed cache - shared across instances, network overhead</summary>
        Distributed,
        
        /// <summary>Persistent cache - survives app restarts, disk-based</summary>
        Persistent
    }

    /// <summary>
    /// Cache policies for different data types
    /// </summary>
    public class CachePolicy
    {
        /// <summary>Time before cache entry expires</summary>
        public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>Time before cache entry is refreshed in background</summary>
        public TimeSpan RefreshTime { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>Maximum size of cache entry in bytes</summary>
        public long MaxSizeBytes { get; set; } = 1024 * 1024; // 1MB

        /// <summary>Priority for cache eviction</summary>
        public CachePriority Priority { get; set; } = CachePriority.Normal;

        /// <summary>Whether to enable background refresh</summary>
        public bool EnableBackgroundRefresh { get; set; } = false;

        /// <summary>Whether cache survives across app restarts</summary>
        public bool PersistAcrossRestarts { get; set; } = false;
    }

    /// <summary>
    /// Cache priority levels for eviction policies
    /// </summary>
    public enum CachePriority
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Cache entry metadata
    /// </summary>
    public class CacheEntry<T>
    {
        public T Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public long SizeBytes { get; set; }
        public int AccessCount { get; set; }
        public CachePriority Priority { get; set; }

        public CacheEntry(T value, CachePolicy policy)
        {
            Value = value;
            CreatedAt = DateTime.UtcNow;
            LastAccessedAt = DateTime.UtcNow;
            ExpiresAt = DateTime.UtcNow.Add(policy.ExpirationTime);
            Priority = policy.Priority;
            AccessCount = 1;
            SizeBytes = EstimateSize(value);
        }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool NeedsRefresh => DateTime.UtcNow > CreatedAt.Add(TimeSpan.FromTicks(ExpiresAt.Ticks - CreatedAt.Ticks));

        private long EstimateSize(T value)
        {
            // Simple size estimation - can be enhanced with more accurate calculations
            if (value == null) return 0;
            
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(value);
                return System.Text.Encoding.UTF8.GetByteCount(json);
            }
            catch
            {
                // Fallback estimation
                return 1024;
            }
        }
    }

    /// <summary>
    /// Advanced response caching service with multi-level caching support
    /// </summary>
    public interface IResponseCacheService
    {
        /// <summary>
        /// Gets cached value or executes factory function if not found
        /// </summary>
        /// <typeparam name="T">Type of cached value</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="factory">Function to generate value if not cached</param>
        /// <param name="policy">Cache policy to use</param>
        /// <param name="level">Cache level to use</param>
        /// <returns>Cached or generated value</returns>
        Task<T> GetOrSetAsync<T>(
            string key,
            Func<Task<T>> factory,
            CachePolicy? policy = null,
            CacheLevel level = CacheLevel.Memory);

        /// <summary>
        /// Gets cached value or returns default
        /// </summary>
        /// <typeparam name="T">Type of cached value</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="level">Cache level to check</param>
        /// <returns>Cached value or default</returns>
        Task<T?> GetAsync<T>(string key, CacheLevel level = CacheLevel.Memory);

        /// <summary>
        /// Sets value in cache
        /// </summary>
        /// <typeparam name="T">Type of value to cache</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to cache</param>
        /// <param name="policy">Cache policy to use</param>
        /// <param name="level">Cache level to use</param>
        Task SetAsync<T>(
            string key,
            T value,
            CachePolicy? policy = null,
            CacheLevel level = CacheLevel.Memory);

        /// <summary>
        /// Removes value from cache
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="level">Cache level to remove from (null for all levels)</param>
        Task RemoveAsync(string key, CacheLevel? level = null);

        /// <summary>
        /// Removes all cached values matching pattern
        /// </summary>
        /// <param name="pattern">Pattern to match (supports wildcards)</param>
        /// <param name="level">Cache level to clear from (null for all levels)</param>
        Task RemoveByPatternAsync(string pattern, CacheLevel? level = null);

        /// <summary>
        /// Clears all cached values
        /// </summary>
        /// <param name="level">Cache level to clear (null for all levels)</param>
        Task ClearAsync(CacheLevel? level = null);

        /// <summary>
        /// Checks if key exists in cache
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="level">Cache level to check</param>
        /// <returns>True if key exists and is not expired</returns>
        Task<bool> ExistsAsync(string key, CacheLevel level = CacheLevel.Memory);

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        /// <param name="level">Cache level to get stats for (null for all levels)</param>
        /// <returns>Cache statistics</returns>
        Task<CacheStatistics> GetStatisticsAsync(CacheLevel? level = null);

        /// <summary>
        /// Refreshes cached value in background
        /// </summary>
        /// <typeparam name="T">Type of cached value</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="factory">Function to generate fresh value</param>
        /// <param name="level">Cache level to refresh</param>
        Task RefreshAsync<T>(string key, Func<Task<T>> factory, CacheLevel level = CacheLevel.Memory);

        /// <summary>
        /// Pre-warms cache with commonly accessed data
        /// </summary>
        Task WarmUpAsync();
    }

    /// <summary>
    /// Cache statistics for monitoring and optimization
    /// </summary>
    public class CacheStatistics
    {
        public CacheLevel Level { get; set; }
        public long TotalEntries { get; set; }
        public long TotalSizeBytes { get; set; }
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public long EvictionCount { get; set; }
        public double HitRate => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
        public long TotalRequests => HitCount + MissCount;
        public DateTime LastResetTime { get; set; }
        public TimeSpan Uptime => DateTime.UtcNow - LastResetTime;

        /// <summary>
        /// Gets formatted statistics string
        /// </summary>
        public string GetSummary()
        {
            return $"{Level} Cache: {TotalEntries} entries, {TotalSizeBytes / 1024}KB, " +
                   $"{HitRate:P1} hit rate ({HitCount}/{TotalRequests} requests)";
        }
    }

    /// <summary>
    /// Predefined cache policies for common scenarios
    /// </summary>
    public static class CachePolicies
    {
        /// <summary>Short-lived cache for frequently changing data</summary>
        public static CachePolicy ShortLived => new()
        {
            ExpirationTime = TimeSpan.FromMinutes(5),
            RefreshTime = TimeSpan.FromMinutes(3),
            Priority = CachePriority.Normal,
            EnableBackgroundRefresh = false
        };

        /// <summary>Medium-term cache for moderately stable data</summary>
        public static CachePolicy MediumTerm => new()
        {
            ExpirationTime = TimeSpan.FromMinutes(30),
            RefreshTime = TimeSpan.FromMinutes(20),
            Priority = CachePriority.Normal,
            EnableBackgroundRefresh = true
        };

        /// <summary>Long-lived cache for stable reference data</summary>
        public static CachePolicy LongLived => new()
        {
            ExpirationTime = TimeSpan.FromHours(6),
            RefreshTime = TimeSpan.FromHours(4),
            Priority = CachePriority.High,
            EnableBackgroundRefresh = true,
            PersistAcrossRestarts = true
        };

        /// <summary>Critical cache that should rarely be evicted</summary>
        public static CachePolicy Critical => new()
        {
            ExpirationTime = TimeSpan.FromHours(24),
            RefreshTime = TimeSpan.FromHours(12),
            Priority = CachePriority.Critical,
            EnableBackgroundRefresh = true,
            PersistAcrossRestarts = true,
            MaxSizeBytes = 10 * 1024 * 1024 // 10MB
        };

        /// <summary>Session-based cache for user-specific data</summary>
        public static CachePolicy Session => new()
        {
            ExpirationTime = TimeSpan.FromHours(2),
            RefreshTime = TimeSpan.FromMinutes(90),
            Priority = CachePriority.High,
            EnableBackgroundRefresh = false
        };
    }
}