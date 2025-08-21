using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Cache strategies for different query types
    /// </summary>
    public enum QueryCacheStrategy
    {
        /// <summary>Aggressive caching for static reference data</summary>
        ReferenceData,
        
        /// <summary>Standard caching for frequently accessed user data</summary>
        UserData,
        
        /// <summary>Minimal caching for volatile data</summary>
        VolatileData,
        
        /// <summary>Long-term caching for spot data</summary>
        SpotData,
        
        /// <summary>Session-based caching for user-specific data</summary>
        SessionData
    }

    /// <summary>
    /// Query metadata for cache optimization
    /// </summary>
    public class QueryMetadata
    {
        public string QueryHash { get; set; } = string.Empty;
        public Type EntityType { get; set; } = typeof(object);
        public string[] Parameters { get; set; } = Array.Empty<string>();
        public QueryCacheStrategy Strategy { get; set; } = QueryCacheStrategy.UserData;
        public bool CanBeCached { get; set; } = true;
        public TimeSpan? CustomExpiry { get; set; }
    }

    /// <summary>
    /// Specialized caching service for database query results
    /// Provides intelligent caching for Entity Framework queries with automatic invalidation
    /// </summary>
    public interface IQueryCacheService
    {
        /// <summary>
        /// Gets cached query result or executes and caches query
        /// </summary>
        /// <typeparam name="TResult">Type of query result</typeparam>
        /// <param name="queryFunc">Query function to execute if not cached</param>
        /// <param name="cacheKey">Unique cache key for the query</param>
        /// <param name="strategy">Cache strategy to use</param>
        /// <returns>Cached or fresh query result</returns>
        Task<TResult> GetOrSetQueryAsync<TResult>(
            Func<Task<TResult>> queryFunc,
            string cacheKey,
            QueryCacheStrategy strategy = QueryCacheStrategy.UserData);

        /// <summary>
        /// Gets cached entity by ID with automatic cache key generation
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entityId">Entity ID</param>
        /// <param name="queryFunc">Function to load entity if not cached</param>
        /// <param name="strategy">Cache strategy to use</param>
        /// <returns>Cached or loaded entity</returns>
        Task<TEntity?> GetEntityAsync<TEntity>(
            Guid entityId,
            Func<Task<TEntity?>> queryFunc,
            QueryCacheStrategy strategy = QueryCacheStrategy.UserData) where TEntity : class;

        /// <summary>
        /// Gets cached collection with filtering support
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="queryFunc">Function to load collection if not cached</param>
        /// <param name="filterHash">Hash of filter parameters</param>
        /// <param name="strategy">Cache strategy to use</param>
        /// <returns>Cached or loaded collection</returns>
        Task<IEnumerable<TEntity>> GetCollectionAsync<TEntity>(
            Func<Task<IEnumerable<TEntity>>> queryFunc,
            string filterHash,
            QueryCacheStrategy strategy = QueryCacheStrategy.UserData) where TEntity : class;

        /// <summary>
        /// Invalidates cache entries for specific entity type
        /// </summary>
        /// <typeparam name="TEntity">Entity type to invalidate</typeparam>
        /// <param name="entityId">Optional specific entity ID</param>
        Task InvalidateEntityCacheAsync<TEntity>(Guid? entityId = null) where TEntity : class;

        /// <summary>
        /// Invalidates all cache entries matching pattern
        /// </summary>
        /// <param name="pattern">Wildcard pattern to match cache keys</param>
        Task InvalidateByPatternAsync(string pattern);

        /// <summary>
        /// Pre-warms cache with frequently accessed queries
        /// </summary>
        /// <param name="queries">Dictionary of cache keys and query functions</param>
        Task WarmUpQueriesAsync(Dictionary<string, Func<Task<object>>> queries);

        /// <summary>
        /// Gets query cache statistics
        /// </summary>
        /// <returns>Cache performance statistics</returns>
        Task<QueryCacheStatistics> GetStatisticsAsync();

        /// <summary>
        /// Generates optimized cache key for query
        /// </summary>
        /// <param name="baseKey">Base cache key</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Optimized cache key</returns>
        string GenerateCacheKey(string baseKey, params object[] parameters);
    }

    /// <summary>
    /// Query cache performance statistics
    /// </summary>
    public class QueryCacheStatistics
    {
        public long TotalQueries { get; set; }
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public long InvalidationCount { get; set; }
        public double HitRate => TotalQueries > 0 ? (double)CacheHits / TotalQueries : 0;
        public Dictionary<string, long> QueryTypeStats { get; set; } = new();
        public Dictionary<QueryCacheStrategy, long> StrategyStats { get; set; } = new();
        public TimeSpan AverageQueryTime { get; set; }
        public DateTime LastResetTime { get; set; }

        /// <summary>
        /// Gets formatted statistics summary
        /// </summary>
        public string GetSummary()
        {
            return $"Query Cache: {CacheHits + CacheMisses} queries, " +
                   $"{HitRate:P1} hit rate, {InvalidationCount} invalidations, " +
                   $"Avg: {AverageQueryTime.TotalMilliseconds:F1}ms";
        }
    }
}