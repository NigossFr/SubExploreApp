using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Performance optimization service providing caching, memory management, and performance monitoring
    /// </summary>
    public interface IPerformanceOptimizationService
    {
        Task<T> GetOrSetCacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        void InvalidateCache(string key);
        void InvalidateCachePattern(string pattern);
        Task<T> ExecuteWithPerformanceTrackingAsync<T>(Func<Task<T>> operation, [CallerMemberName] string operationName = "");
        void LogPerformanceMetrics();
        Task OptimizeMemoryUsageAsync();
    }

    public class PerformanceOptimizationService : IPerformanceOptimizationService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<PerformanceOptimizationService> _logger;
        private readonly ConcurrentDictionary<string, long> _performanceMetrics;
        private readonly ConcurrentDictionary<string, int> _operationCounts;

        // Performance thresholds
        private const int SLOW_OPERATION_THRESHOLD_MS = 1000;
        private const int CACHE_DEFAULT_EXPIRATION_MINUTES = 10;
        private const int MAX_CACHE_ENTRIES = 1000;

        public PerformanceOptimizationService(
            IMemoryCache memoryCache,
            ILogger<PerformanceOptimizationService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _performanceMetrics = new ConcurrentDictionary<string, long>();
            _operationCounts = new ConcurrentDictionary<string, int>();
        }

        /// <summary>
        /// Generic cache method with automatic expiration and memory management
        /// </summary>
        public async Task<T> GetOrSetCacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            try
            {
                // Check if value exists in cache
                if (_memoryCache.TryGetValue(key, out T cachedValue))
                {
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return cachedValue;
                }

                // Execute factory to get value
                var startTime = DateTime.UtcNow;
                var value = await factory().ConfigureAwait(false);
                var duration = DateTime.UtcNow - startTime;

                // Set cache with expiration
                var cacheExpiration = expiration ?? TimeSpan.FromMinutes(CACHE_DEFAULT_EXPIRATION_MINUTES);
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = cacheExpiration,
                    Priority = CacheItemPriority.Normal,
                    Size = 1 // For memory management
                };

                // Set callback for cache removal logging
                cacheOptions.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
                {
                    _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", evictedKey, reason);
                });

                _memoryCache.Set(key, value, cacheOptions);

                _logger.LogDebug("Cache miss for key: {Key}, Factory execution took {Duration}ms", 
                    key, duration.TotalMilliseconds);

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cache operation for key: {Key}", key);
                // Return factory result without caching on error
                return await factory().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Invalidate specific cache entry
        /// </summary>
        public void InvalidateCache(string key)
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("Cache invalidated for key: {Key}", key);
        }

        /// <summary>
        /// Invalidate cache entries matching a pattern
        /// </summary>
        public void InvalidateCachePattern(string pattern)
        {
            try
            {
                // Note: IMemoryCache doesn't expose keys directly
                // This is a simplified implementation - in production, consider using a cache with key enumeration
                _logger.LogDebug("Cache pattern invalidation requested for pattern: {Pattern}", pattern);
                
                // For now, we'll rely on natural expiration
                // In a production scenario, consider using a cache implementation that supports key enumeration
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache pattern: {Pattern}", pattern);
            }
        }

        /// <summary>
        /// Execute operation with performance tracking
        /// </summary>
        public async Task<T> ExecuteWithPerformanceTrackingAsync<T>(
            Func<Task<T>> operation, 
            [CallerMemberName] string operationName = "")
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                var result = await operation().ConfigureAwait(false);
                var duration = DateTime.UtcNow - startTime;
                
                // Track performance metrics
                _performanceMetrics.AddOrUpdate(operationName, 
                    (long)duration.TotalMilliseconds,
                    (key, oldValue) => (oldValue + (long)duration.TotalMilliseconds) / 2); // Simple moving average
                
                _operationCounts.AddOrUpdate(operationName, 1, (key, oldValue) => oldValue + 1);

                // Log slow operations
                if (duration.TotalMilliseconds > SLOW_OPERATION_THRESHOLD_MS)
                {
                    _logger.LogWarning("Slow operation detected: {OperationName} took {Duration}ms", 
                        operationName, duration.TotalMilliseconds);
                }
                else
                {
                    _logger.LogDebug("Operation {OperationName} completed in {Duration}ms", 
                        operationName, duration.TotalMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Operation {OperationName} failed after {Duration}ms", 
                    operationName, duration.TotalMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Log current performance metrics
        /// </summary>
        public void LogPerformanceMetrics()
        {
            try
            {
                _logger.LogInformation("=== Performance Metrics ===");
                
                foreach (var metric in _performanceMetrics)
                {
                    var count = _operationCounts.GetValueOrDefault(metric.Key, 0);
                    _logger.LogInformation("Operation: {Operation}, Avg Duration: {AvgDuration}ms, Count: {Count}",
                        metric.Key, metric.Value, count);
                }

                // Log memory usage
                var memoryUsage = GC.GetTotalMemory(false);
                _logger.LogInformation("Current memory usage: {MemoryMB} MB", memoryUsage / (1024 * 1024));
                
                _logger.LogInformation("=== End Performance Metrics ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging performance metrics");
            }
        }

        /// <summary>
        /// Optimize memory usage by triggering garbage collection and cache cleanup
        /// </summary>
        public async Task OptimizeMemoryUsageAsync()
        {
            try
            {
                _logger.LogInformation("Starting memory optimization...");
                
                var beforeMemory = GC.GetTotalMemory(false);
                
                // Clear performance metrics older than 1 hour
                // (In a real implementation, you'd track timestamps)
                if (_performanceMetrics.Count > 100)
                {
                    _performanceMetrics.Clear();
                    _operationCounts.Clear();
                    _logger.LogDebug("Cleared old performance metrics");
                }

                // Trigger garbage collection
                await Task.Run(() =>
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }).ConfigureAwait(false);

                var afterMemory = GC.GetTotalMemory(true);
                var memoryFreed = beforeMemory - afterMemory;
                
                _logger.LogInformation("Memory optimization completed. Freed: {MemoryFreedMB} MB, Current: {CurrentMemoryMB} MB",
                    memoryFreed / (1024 * 1024), afterMemory / (1024 * 1024));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during memory optimization");
            }
        }
    }
}