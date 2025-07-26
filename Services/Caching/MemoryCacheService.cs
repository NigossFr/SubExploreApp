using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SubExplore.Services.Caching
{
    /// <summary>
    /// In-memory cache implementation
    /// </summary>
    public class MemoryCacheService : ICacheService, IDisposable
    {
        private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly Timer _cleanupTimer;
        private const int CleanupIntervalMinutes = 5;
        private bool _disposed = false;

        public MemoryCacheService(ILogger<MemoryCacheService> logger)
        {
            _logger = logger;
            _cleanupTimer = new Timer(CleanupExpiredItems, null, 
                TimeSpan.FromMinutes(CleanupIntervalMinutes), 
                TimeSpan.FromMinutes(CleanupIntervalMinutes));
        }

        public Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return Task.FromResult(default(T));

            if (_cache.TryGetValue(key, out var item))
            {
                if (!item.IsExpired)
                {
                    item.LastAccessed = DateTime.UtcNow;
                    if (item.Value is T value)
                    {
                        _logger.LogTrace("Cache hit for key: {Key}", key);
                        return Task.FromResult(value);
                    }
                }
                else
                {
                    // Remove expired item
                    _cache.TryRemove(key, out _);
                    _logger.LogTrace("Removed expired cache item for key: {Key}", key);
                }
            }

            _logger.LogTrace("Cache miss for key: {Key}", key);
            return Task.FromResult(default(T));
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key))
                return Task.CompletedTask;

            var expiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : DateTime.MaxValue;
            var cacheItem = new CacheItem
            {
                Value = value,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow
            };

            _cache.AddOrUpdate(key, cacheItem, (k, existing) => cacheItem);
            _logger.LogTrace("Cache item set for key: {Key}, expires at: {ExpiresAt}", key, expiresAt);

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return Task.CompletedTask;

            _cache.TryRemove(key, out _);
            _logger.LogTrace("Cache item removed for key: {Key}", key);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _cache.Clear();
            _logger.LogInformation("Cache cleared");
            return Task.CompletedTask;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            try
            {
                var value = await factory();
                if (value != null)
                {
                    await SetAsync(key, value, expiration);
                }
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cache factory method for key: {Key}", key);
                throw;
            }
        }

        public bool Exists(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            if (_cache.TryGetValue(key, out var item))
            {
                if (!item.IsExpired)
                {
                    item.LastAccessed = DateTime.UtcNow;
                    return true;
                }
                else
                {
                    _cache.TryRemove(key, out _);
                }
            }
            return false;
        }

        public void ClearMemoryCache()
        {
            var count = _cache.Count;
            _cache.Clear();
            _logger.LogInformation("Memory cache cleared, removed {Count} items", count);
        }

        private void CleanupExpiredItems(object state)
        {
            try
            {
                var expiredKeys = new List<string>();
                var now = DateTime.UtcNow;

                foreach (var kvp in _cache)
                {
                    if (kvp.Value.IsExpired)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogDebug("Cleaned up {Count} expired cache items", expiredKeys.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _cleanupTimer?.Dispose();
                _cache.Clear();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private class CacheItem
        {
            public object Value { get; set; }
            public DateTime ExpiresAt { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastAccessed { get; set; }

            public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        }
    }
}