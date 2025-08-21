using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// High-performance query caching service with intelligent cache strategies
    /// </summary>
    public class QueryCacheService : IQueryCacheService
    {
        private readonly IResponseCacheService _cacheService;
        private readonly ILogger<QueryCacheService> _logger;
        private readonly QueryCacheStatistics _statistics;
        
        // Cache strategy mappings
        private static readonly Dictionary<QueryCacheStrategy, CachePolicy> StrategyMappings = new()
        {
            { QueryCacheStrategy.ReferenceData, CachePolicies.LongLived },
            { QueryCacheStrategy.UserData, CachePolicies.MediumTerm },
            { QueryCacheStrategy.VolatileData, CachePolicies.ShortLived },
            { QueryCacheStrategy.SpotData, CachePolicies.MediumTerm },
            { QueryCacheStrategy.SessionData, CachePolicies.Session }
        };

        public QueryCacheService(
            IResponseCacheService cacheService,
            ILogger<QueryCacheService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
            _statistics = new QueryCacheStatistics
            {
                LastResetTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Gets cached query result or executes and caches query with performance tracking
        /// </summary>
        public async Task<TResult> GetOrSetQueryAsync<TResult>(
            Func<Task<TResult>> queryFunc,
            string cacheKey,
            QueryCacheStrategy strategy = QueryCacheStrategy.UserData)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(cacheKey));

            if (queryFunc == null)
                throw new ArgumentNullException(nameof(queryFunc));

            var stopwatch = Stopwatch.StartNew();
            var policy = StrategyMappings.GetValueOrDefault(strategy, CachePolicies.MediumTerm);
            
            try
            {
                _logger.LogDebug("🔍 Query cache lookup: {Key} with strategy {Strategy}", cacheKey, strategy);
                
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogDebug("🔧 Executing query for cache key: {Key}", cacheKey);
                        var queryResult = await queryFunc();
                        RecordCacheMiss(strategy);
                        return queryResult;
                    },
                    policy,
                    CacheLevel.Memory);

                RecordCacheHit(strategy);
                stopwatch.Stop();
                
                _statistics.AverageQueryTime = TimeSpan.FromTicks(
                    (_statistics.AverageQueryTime.Ticks + stopwatch.Elapsed.Ticks) / 2);

                _logger.LogDebug("✅ Query cache result: {Key} in {ElapsedMs}ms", 
                    cacheKey, stopwatch.ElapsedMilliseconds);
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "❌ Error in query cache for key {Key}", cacheKey);
                throw;
            }
        }

        /// <summary>
        /// Gets cached entity by ID with automatic cache key generation
        /// </summary>
        public async Task<TEntity?> GetEntityAsync<TEntity>(
            Guid entityId,
            Func<Task<TEntity?>> queryFunc,
            QueryCacheStrategy strategy = QueryCacheStrategy.UserData) where TEntity : class
        {
            var cacheKey = GenerateEntityCacheKey<TEntity>(entityId);
            return await GetOrSetQueryAsync(queryFunc, cacheKey, strategy);
        }

        /// <summary>
        /// Gets cached collection with intelligent filtering support
        /// </summary>
        public async Task<IEnumerable<TEntity>> GetCollectionAsync<TEntity>(
            Func<Task<IEnumerable<TEntity>>> queryFunc,
            string filterHash,
            QueryCacheStrategy strategy = QueryCacheStrategy.UserData) where TEntity : class
        {
            var cacheKey = GenerateCollectionCacheKey<TEntity>(filterHash);
            return await GetOrSetQueryAsync(queryFunc, cacheKey, strategy);
        }

        /// <summary>
        /// Invalidates cache entries for specific entity type with pattern matching
        /// </summary>
        public async Task InvalidateEntityCacheAsync<TEntity>(Guid? entityId = null) where TEntity : class
        {
            try
            {
                var entityTypeName = typeof(TEntity).Name.ToLowerInvariant();
                
                if (entityId.HasValue)
                {
                    // Invalidate specific entity
                    var specificKey = GenerateEntityCacheKey<TEntity>(entityId.Value);
                    await _cacheService.RemoveAsync(specificKey);
                    _logger.LogDebug("🗑️ Invalidated specific entity cache: {Key}", specificKey);
                }
                
                // Invalidate all entities of this type
                var typePattern = $"entity:{entityTypeName}:*";
                await _cacheService.RemoveByPatternAsync(typePattern);
                
                // Invalidate collections of this type
                var collectionPattern = $"collection:{entityTypeName}:*";
                await _cacheService.RemoveByPatternAsync(collectionPattern);
                
                RecordInvalidation(entityTypeName);
                _logger.LogInformation("🗑️ Invalidated cache for entity type: {EntityType}", entityTypeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error invalidating cache for entity type {EntityType}", typeof(TEntity).Name);
                throw;
            }
        }

        /// <summary>
        /// Invalidates cache entries matching wildcard pattern
        /// </summary>
        public async Task InvalidateByPatternAsync(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return;

            try
            {
                await _cacheService.RemoveByPatternAsync(pattern);
                RecordInvalidation("pattern");
                _logger.LogDebug("🗑️ Invalidated cache by pattern: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error invalidating cache by pattern {Pattern}", pattern);
                throw;
            }
        }

        /// <summary>
        /// Pre-warms cache with frequently accessed queries
        /// </summary>
        public async Task WarmUpQueriesAsync(Dictionary<string, Func<Task<object>>> queries)
        {
            if (queries == null || !queries.Any())
            {
                _logger.LogWarning("⚠️ No queries provided for cache warm-up");
                return;
            }

            try
            {
                _logger.LogInformation("🔥 Starting query cache warm-up with {Count} queries", queries.Count);
                
                var warmupTasks = queries.Select(async kvp =>
                {
                    try
                    {
                        await GetOrSetQueryAsync(kvp.Value, kvp.Key, QueryCacheStrategy.ReferenceData);
                        _logger.LogDebug("✅ Warmed up query: {Key}", kvp.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Failed to warm up query: {Key}", kvp.Key);
                    }
                });

                await Task.WhenAll(warmupTasks);
                _logger.LogInformation("🔥 Query cache warm-up completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during query cache warm-up");
                throw;
            }
        }

        /// <summary>
        /// Gets comprehensive query cache statistics
        /// </summary>
        public async Task<QueryCacheStatistics> GetStatisticsAsync()
        {
            // Update total queries count
            _statistics.TotalQueries = _statistics.CacheHits + _statistics.CacheMisses;
            
            // Get underlying cache statistics
            var cacheStats = await _cacheService.GetStatisticsAsync();
            
            // Merge statistics if needed
            _statistics.LastResetTime = cacheStats.LastResetTime;
            
            return _statistics;
        }

        /// <summary>
        /// Generates optimized cache key with hashing for long parameters
        /// </summary>
        public string GenerateCacheKey(string baseKey, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(baseKey))
                throw new ArgumentException("Base key cannot be null or empty", nameof(baseKey));

            if (parameters == null || parameters.Length == 0)
                return baseKey.ToLowerInvariant();

            try
            {
                var keyBuilder = new StringBuilder(baseKey.ToLowerInvariant());
                
                foreach (var param in parameters)
                {
                    keyBuilder.Append(':');
                    
                    if (param == null)
                    {
                        keyBuilder.Append("null");
                    }
                    else if (param is string str && str.Length > 50)
                    {
                        // Hash long string parameters
                        keyBuilder.Append(ComputeHash(str));
                    }
                    else
                    {
                        keyBuilder.Append(param.ToString());
                    }
                }
                
                var finalKey = keyBuilder.ToString();
                
                // If the final key is too long, hash it
                if (finalKey.Length > 250)
                {
                    finalKey = $"{baseKey}:{ComputeHash(finalKey)}";
                }
                
                return finalKey;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error generating cache key, using fallback");
                return $"{baseKey}:fallback:{Guid.NewGuid():N}";
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Generates cache key for specific entity
        /// </summary>
        private string GenerateEntityCacheKey<TEntity>(Guid entityId) where TEntity : class
        {
            var entityType = typeof(TEntity).Name.ToLowerInvariant();
            return $"entity:{entityType}:{entityId:N}";
        }

        /// <summary>
        /// Generates cache key for entity collection
        /// </summary>
        private string GenerateCollectionCacheKey<TEntity>(string filterHash) where TEntity : class
        {
            var entityType = typeof(TEntity).Name.ToLowerInvariant();
            return $"collection:{entityType}:{filterHash}";
        }

        /// <summary>
        /// Computes SHA256 hash for long strings
        /// </summary>
        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes)[..12]; // Take first 12 chars
        }

        /// <summary>
        /// Records cache hit for statistics
        /// </summary>
        private void RecordCacheHit(QueryCacheStrategy strategy)
        {
            _statistics.CacheHits++;
            UpdateStrategyStats(strategy, true);
        }

        /// <summary>
        /// Records cache miss for statistics
        /// </summary>
        private void RecordCacheMiss(QueryCacheStrategy strategy)
        {
            _statistics.CacheMisses++;
            UpdateStrategyStats(strategy, false);
        }

        /// <summary>
        /// Records cache invalidation for statistics
        /// </summary>
        private void RecordInvalidation(string entityType)
        {
            _statistics.InvalidationCount++;
            
            if (!_statistics.QueryTypeStats.ContainsKey(entityType))
                _statistics.QueryTypeStats[entityType] = 0;
            
            _statistics.QueryTypeStats[entityType]++;
        }

        /// <summary>
        /// Updates strategy-specific statistics
        /// </summary>
        private void UpdateStrategyStats(QueryCacheStrategy strategy, bool isHit)
        {
            if (!_statistics.StrategyStats.ContainsKey(strategy))
                _statistics.StrategyStats[strategy] = 0;
            
            if (isHit)
                _statistics.StrategyStats[strategy]++;
        }

        #endregion
    }
}