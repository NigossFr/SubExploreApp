using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// High-performance multi-level caching service with intelligent eviction and background refresh
    /// </summary>
    public class ResponseCacheService : IResponseCacheService, IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<ResponseCacheService> _logger;
        
        // Cache statistics tracking
        private readonly ConcurrentDictionary<CacheLevel, CacheStatistics> _statistics;
        private readonly ConcurrentDictionary<string, CacheEntry<object>> _memoryEntries;
        
        // Background refresh tracking
        private readonly ConcurrentDictionary<string, Task> _refreshTasks;
        private readonly System.Threading.Timer _cleanupTimer;
        
        private bool _disposed = false;

        public ResponseCacheService(
            IMemoryCache memoryCache,
            ILogger<ResponseCacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            
            _statistics = new ConcurrentDictionary<CacheLevel, CacheStatistics>();
            _memoryEntries = new ConcurrentDictionary<string, CacheEntry<object>>();
            _refreshTasks = new ConcurrentDictionary<string, Task>();

            // Initialize statistics
            InitializeStatistics();

            // Setup cleanup timer to run every 5 minutes
            _cleanupTimer = new System.Threading.Timer(
                CleanupExpiredEntries, 
                null, 
                TimeSpan.FromMinutes(5), 
                TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Gets cached value or executes factory function with intelligent caching strategy
        /// </summary>
        public async Task<T> GetOrSetAsync<T>(
            string key,
            Func<Task<T>> factory,
            CachePolicy? policy = null,
            CacheLevel level = CacheLevel.Memory)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            policy ??= CachePolicies.MediumTerm;

            _logger.LogDebug("🔍 Cache GET: {Key} from {Level}", key, level);

            // Try to get from cache first
            var cachedValue = await GetAsync<T>(key, level);
            if (cachedValue != null)
            {
                RecordHit(level);
                
                // Check if background refresh is needed
                if (policy.EnableBackgroundRefresh && ShouldRefreshInBackground(key, level))
                {
                    _ = Task.Run(async () => await RefreshAsync(key, factory, level));
                }
                
                return cachedValue;
            }

            RecordMiss(level);

            // Generate value and cache it
            _logger.LogDebug("🔧 Cache MISS: Generating value for {Key}", key);
            
            try
            {
                var value = await factory();
                await SetAsync(key, value, policy, level);
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating value for cache key {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Gets cached value with high-performance lookup
        /// </summary>
        public async Task<T?> GetAsync<T>(string key, CacheLevel level = CacheLevel.Memory)
        {
            if (string.IsNullOrWhiteSpace(key))
                return default(T);

            try
            {
                switch (level)
                {
                    case CacheLevel.Memory:
                        return await GetFromMemoryCacheAsync<T>(key);
                    
                    case CacheLevel.Distributed:
                        // For now, fallback to memory cache
                        // TODO: Implement distributed cache when needed
                        return await GetFromMemoryCacheAsync<T>(key);
                    
                    case CacheLevel.Persistent:
                        // For now, fallback to memory cache
                        // TODO: Implement persistent cache when needed
                        return await GetFromMemoryCacheAsync<T>(key);
                    
                    default:
                        return default(T);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error retrieving from cache key {Key}", key);
                return default(T);
            }
        }

        /// <summary>
        /// Sets value in cache with intelligent size and eviction management
        /// </summary>
        public async Task SetAsync<T>(
            string key,
            T value,
            CachePolicy? policy = null,
            CacheLevel level = CacheLevel.Memory)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
                return;

            policy ??= CachePolicies.MediumTerm;

            try
            {
                switch (level)
                {
                    case CacheLevel.Memory:
                        await SetInMemoryCacheAsync(key, value, policy);
                        break;
                    
                    case CacheLevel.Distributed:
                        // TODO: Implement distributed cache
                        await SetInMemoryCacheAsync(key, value, policy);
                        break;
                    
                    case CacheLevel.Persistent:
                        // TODO: Implement persistent cache
                        await SetInMemoryCacheAsync(key, value, policy);
                        break;
                }

                _logger.LogDebug("💾 Cache SET: {Key} in {Level} (expires: {Expiry})", 
                    key, level, DateTime.UtcNow.Add(policy.ExpirationTime));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error setting cache key {Key}", key);
            }
        }

        /// <summary>
        /// Removes value from cache with pattern support
        /// </summary>
        public async Task RemoveAsync(string key, CacheLevel? level = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            try
            {
                if (level == null || level == CacheLevel.Memory)
                {
                    _memoryCache.Remove(key);
                    _memoryEntries.TryRemove(key, out _);
                }

                _logger.LogDebug("🗑️ Cache REMOVE: {Key} from {Level}", key, level?.ToString() ?? "all levels");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error removing cache key {Key}", key);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Removes all cached values matching pattern with regex support
        /// </summary>
        public async Task RemoveByPatternAsync(string pattern, CacheLevel? level = null)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return;

            try
            {
                // Convert wildcard pattern to regex
                var regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
                var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

                var keysToRemove = _memoryEntries.Keys.Where(key => regex.IsMatch(key)).ToList();

                foreach (var key in keysToRemove)
                {
                    await RemoveAsync(key, level);
                }

                _logger.LogDebug("🗑️ Cache REMOVE PATTERN: {Pattern} matched {Count} keys", pattern, keysToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error removing cache entries by pattern {Pattern}", pattern);
            }
        }

        /// <summary>
        /// Clears all cached values with optional level filtering
        /// </summary>
        public async Task ClearAsync(CacheLevel? level = null)
        {
            try
            {
                if (level == null || level == CacheLevel.Memory)
                {
                    if (_memoryCache is MemoryCache mc)
                    {
                        mc.Compact(1.0); // Remove all entries
                    }
                    _memoryEntries.Clear();
                }

                _logger.LogInformation("🧹 Cache CLEAR: {Level}", level?.ToString() ?? "all levels");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error clearing cache");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Checks if key exists with expiration validation
        /// </summary>
        public async Task<bool> ExistsAsync(string key, CacheLevel level = CacheLevel.Memory)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                switch (level)
                {
                    case CacheLevel.Memory:
                        if (_memoryEntries.TryGetValue(key, out var entry))
                        {
                            return !entry.IsExpired;
                        }
                        return _memoryCache.TryGetValue(key, out _);
                    
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error checking cache key existence {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Gets comprehensive cache statistics for monitoring
        /// </summary>
        public async Task<CacheStatistics> GetStatisticsAsync(CacheLevel? level = null)
        {
            if (level.HasValue)
            {
                _statistics.TryGetValue(level.Value, out var stats);
                return stats ?? new CacheStatistics { Level = level.Value };
            }

            // Aggregate statistics for all levels
            var aggregated = new CacheStatistics
            {
                Level = CacheLevel.Memory, // Primary level
                TotalEntries = _statistics.Values.Sum(s => s.TotalEntries),
                TotalSizeBytes = _statistics.Values.Sum(s => s.TotalSizeBytes),
                HitCount = _statistics.Values.Sum(s => s.HitCount),
                MissCount = _statistics.Values.Sum(s => s.MissCount),
                EvictionCount = _statistics.Values.Sum(s => s.EvictionCount),
                LastResetTime = _statistics.Values.Min(s => s.LastResetTime)
            };

            await Task.CompletedTask;
            return aggregated;
        }

        /// <summary>
        /// Refreshes cached value in background without blocking
        /// </summary>
        public async Task RefreshAsync<T>(string key, Func<Task<T>> factory, CacheLevel level = CacheLevel.Memory)
        {
            if (string.IsNullOrWhiteSpace(key) || factory == null)
                return;

            // Prevent multiple simultaneous refreshes of the same key
            var refreshKey = $"{level}:{key}";
            if (_refreshTasks.ContainsKey(refreshKey))
                return;

            var refreshTask = Task.Run(async () =>
            {
                try
                {
                    _logger.LogDebug("🔄 Cache REFRESH: {Key} in {Level}", key, level);
                    
                    var newValue = await factory();
                    await SetAsync(key, newValue, null, level);
                    
                    _logger.LogDebug("✅ Cache REFRESH completed: {Key}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Cache refresh failed for key {Key}", key);
                }
                finally
                {
                    _refreshTasks.TryRemove(refreshKey, out _);
                }
            });

            _refreshTasks.TryAdd(refreshKey, refreshTask);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Pre-warms cache with commonly accessed data
        /// </summary>
        public async Task WarmUpAsync()
        {
            try
            {
                _logger.LogInformation("🔥 Cache WARMUP started");

                // TODO: Add specific warmup logic for commonly accessed data
                // This could include:
                // - Loading reference data (spot types, categories)
                // - Caching user preferences for active users
                // - Pre-loading frequently accessed spots

                _logger.LogInformation("✅ Cache WARMUP completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Cache warmup failed");
            }

            await Task.CompletedTask;
        }

        #region Private Helper Methods

        /// <summary>
        /// Gets value from memory cache with metadata tracking
        /// </summary>
        private async Task<T?> GetFromMemoryCacheAsync<T>(string key)
        {
            if (_memoryCache.TryGetValue(key, out var cachedValue))
            {
                // Update access tracking
                if (_memoryEntries.TryGetValue(key, out var entry))
                {
                    entry.LastAccessedAt = DateTime.UtcNow;
                    entry.AccessCount++;
                }

                if (cachedValue is T typedValue)
                {
                    return typedValue;
                }
            }

            await Task.CompletedTask;
            return default(T);
        }

        /// <summary>
        /// Sets value in memory cache with intelligent eviction policy
        /// </summary>
        private async Task SetInMemoryCacheAsync<T>(string key, T value, CachePolicy policy)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = policy.ExpirationTime,
                Priority = MapCachePriority(policy.Priority),
                Size = EstimateSize(value)
            };

            // Add eviction callback for statistics tracking
            cacheEntryOptions.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                _memoryEntries.TryRemove(k.ToString()!, out _);
                RecordEviction(CacheLevel.Memory);
                
                _logger.LogDebug("♻️ Cache evicted: {Key} (reason: {Reason})", k, reason);
            });

            _memoryCache.Set(key, value, cacheEntryOptions);

            // Track entry metadata
            var entry = new CacheEntry<object>(value!, policy);
            _memoryEntries.AddOrUpdate(key, entry, (k, existing) => entry);

            // Update statistics
            var stats = _statistics.GetOrAdd(CacheLevel.Memory, _ => new CacheStatistics { Level = CacheLevel.Memory });
            stats.TotalEntries = _memoryEntries.Count;
            stats.TotalSizeBytes += entry.SizeBytes;

            await Task.CompletedTask;
        }

        /// <summary>
        /// Determines if background refresh is needed
        /// </summary>
        private bool ShouldRefreshInBackground(string key, CacheLevel level)
        {
            if (_memoryEntries.TryGetValue(key, out var entry))
            {
                return entry.NeedsRefresh;
            }
            return false;
        }

        /// <summary>
        /// Maps internal cache priority to framework priority
        /// </summary>
        private static Microsoft.Extensions.Caching.Memory.CacheItemPriority MapCachePriority(CachePriority priority)
        {
            return priority switch
            {
                CachePriority.Low => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Low,
                CachePriority.Normal => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal,
                CachePriority.High => Microsoft.Extensions.Caching.Memory.CacheItemPriority.High,
                CachePriority.Critical => Microsoft.Extensions.Caching.Memory.CacheItemPriority.NeverRemove,
                _ => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal
            };
        }

        /// <summary>
        /// Estimates memory size of cached object
        /// </summary>
        private static long EstimateSize(object value)
        {
            if (value == null) return 0;

            try
            {
                var json = JsonSerializer.Serialize(value);
                return System.Text.Encoding.UTF8.GetByteCount(json);
            }
            catch
            {
                // Fallback size estimation
                return 1024;
            }
        }

        /// <summary>
        /// Initializes cache statistics tracking
        /// </summary>
        private void InitializeStatistics()
        {
            var now = DateTime.UtcNow;
            _statistics[CacheLevel.Memory] = new CacheStatistics
            {
                Level = CacheLevel.Memory,
                LastResetTime = now
            };
        }

        /// <summary>
        /// Records cache hit for statistics
        /// </summary>
        private void RecordHit(CacheLevel level)
        {
            if (_statistics.TryGetValue(level, out var stats))
            {
                var currentCount = stats.HitCount;
                stats.HitCount = currentCount + 1;
            }
        }

        /// <summary>
        /// Records cache miss for statistics
        /// </summary>
        private void RecordMiss(CacheLevel level)
        {
            if (_statistics.TryGetValue(level, out var stats))
            {
                var currentCount = stats.MissCount;
                stats.MissCount = currentCount + 1;
            }
        }

        /// <summary>
        /// Records cache eviction for statistics
        /// </summary>
        private void RecordEviction(CacheLevel level)
        {
            if (_statistics.TryGetValue(level, out var stats))
            {
                var currentCount = stats.EvictionCount;
                stats.EvictionCount = currentCount + 1;
            }
        }

        /// <summary>
        /// Periodic cleanup of expired entries
        /// </summary>
        private void CleanupExpiredEntries(object? state)
        {
            try
            {
                var expiredKeys = _memoryEntries
                    .Where(kvp => kvp.Value.IsExpired)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _memoryCache.Remove(key);
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogDebug("🧹 Cleaned up {Count} expired cache entries", expiredKeys.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error during cache cleanup");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _cleanupTimer?.Dispose();
                
                // Wait for any ongoing refresh tasks
                var refreshTasks = _refreshTasks.Values.ToArray();
                if (refreshTasks.Length > 0)
                {
                    Task.WaitAll(refreshTasks, TimeSpan.FromSeconds(5));
                }

                _disposed = true;
            }
        }

        #endregion
    }
}