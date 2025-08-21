using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// High-performance request deduplication service with intelligent caching
    /// </summary>
    public class RequestDeduplicationService : IRequestDeduplicationService
    {
        private readonly IResponseCacheService _cacheService;
        private readonly ILogger<RequestDeduplicationService> _logger;
        private readonly DeduplicationStatistics _statistics;
        
        // In-flight request tracking
        private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _inFlightRequests;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens;
        private readonly ConcurrentDictionary<string, int> _duplicateCounters;
        
        // Performance monitoring
        private int _currentInFlightCount = 0;
        private int _maxConcurrentRequests = 0;

        public RequestDeduplicationService(
            IResponseCacheService cacheService,
            ILogger<RequestDeduplicationService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
            _statistics = new DeduplicationStatistics { LastResetTime = DateTime.UtcNow };
            _inFlightRequests = new ConcurrentDictionary<string, TaskCompletionSource<object>>();
            _cancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
            _duplicateCounters = new ConcurrentDictionary<string, int>();
        }

        /// <summary>
        /// Executes request with intelligent deduplication and caching
        /// </summary>
        public async Task<DeduplicatedRequestResult<TResult>> ExecuteAsync<TResult>(
            Func<Task<TResult>> requestFunc,
            RequestContext context)
        {
            if (requestFunc == null)
                throw new ArgumentNullException(nameof(requestFunc));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var stopwatch = Stopwatch.StartNew();
            var result = new DeduplicatedRequestResult<TResult>
            {
                RequestId = Guid.NewGuid().ToString("N"),
                RequestTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogDebug("🚀 Processing request: {RequestKey} with strategy {Strategy}",
                    context.RequestKey, context.Strategy);

                _statistics.TotalRequests++;
                UpdateStrategyStats(context.Strategy);

                // Track duplicate attempts
                var duplicateCount = _duplicateCounters.AddOrUpdate(context.RequestKey, 1, (key, count) => count + 1);
                result.DuplicateCount = duplicateCount;

                // Check cache first if enabled
                if (context.EnableCaching)
                {
                    var cachedResult = await TryGetCachedResultAsync<TResult>(context.RequestKey);
                    if (cachedResult != null)
                    {
                        result.Result = cachedResult;
                        result.IsSuccess = true;
                        result.WasCached = true;
                        stopwatch.Stop();
                        result.ExecutionTime = stopwatch.Elapsed;

                        _statistics.CacheHits++;
                        UpdateCachedRequestTime(stopwatch.Elapsed);

                        _logger.LogDebug("💾 Cache hit for request: {RequestKey} in {Ms}ms",
                            context.RequestKey, stopwatch.ElapsedMilliseconds);

                        return result;
                    }
                }

                // Handle in-flight request deduplication
                switch (context.Strategy)
                {
                    case DeduplicationStrategy.ReturnCached:
                        // Already handled above
                        break;

                    case DeduplicationStrategy.WaitForInFlight:
                        var waitResult = await WaitForInFlightRequestAsync<TResult>(context.RequestKey);
                        if (waitResult != null)
                        {
                            result.Result = waitResult;
                            result.IsSuccess = true;
                            result.WasDeduplicatedWithInFlight = true;
                            stopwatch.Stop();
                            result.ExecutionTime = stopwatch.Elapsed;

                            _statistics.InFlightMerges++;
                            _statistics.DeduplicatedRequests++;

                            _logger.LogDebug("🔗 Merged with in-flight request: {RequestKey} in {Ms}ms",
                                context.RequestKey, stopwatch.ElapsedMilliseconds);

                            return result;
                        }
                        break;

                    case DeduplicationStrategy.CancelAndRestart:
                        await CancelInFlightRequestsAsync(context.RequestKey);
                        break;

                    case DeduplicationStrategy.ExecuteParallel:
                        // No deduplication, execute directly
                        break;
                }

                // Execute the request
                result.Result = await ExecuteRequestWithTrackingAsync(requestFunc, context);
                result.IsSuccess = true;

                // Cache the result if enabled
                if (context.EnableCaching && result.IsSuccess)
                {
                    await CacheResultAsync(context.RequestKey, result.Result, context.CacheDuration);
                }

                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;

                UpdateRequestTime(stopwatch.Elapsed);

                _logger.LogDebug("✅ Request completed: {RequestKey} in {Ms}ms",
                    context.RequestKey, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;

                _statistics.ErrorCount++;
                
                _logger.LogError(ex, "❌ Request failed: {RequestKey}", context.RequestKey);
                return result;
            }
            finally
            {
                // Cleanup duplicate counter after reasonable time
                _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ =>
                {
                    _duplicateCounters.TryRemove(context.RequestKey, out var _);
                });
            }
        }

        /// <summary>
        /// Executes HTTP request with automatic deduplication
        /// </summary>
        public async Task<DeduplicatedRequestResult<HttpResponseMessage>> ExecuteHttpRequestAsync(
            Func<Task<HttpResponseMessage>> httpRequestFunc,
            string requestKey,
            DeduplicationStrategy strategy = DeduplicationStrategy.WaitForInFlight)
        {
            var context = new RequestContext
            {
                RequestKey = requestKey,
                Strategy = strategy,
                EnableCaching = false, // HTTP responses often shouldn't be cached
                Timeout = TimeSpan.FromSeconds(30)
            };

            return await ExecuteAsync(httpRequestFunc, context);
        }

        /// <summary>
        /// Executes database query with intelligent result caching
        /// </summary>
        public async Task<DeduplicatedRequestResult<TEntity>> ExecuteDatabaseQueryAsync<TEntity>(
            Func<Task<TEntity>> queryFunc,
            string queryKey,
            TimeSpan? cacheDuration = null)
        {
            var context = new RequestContext
            {
                RequestKey = queryKey,
                Strategy = DeduplicationStrategy.WaitForInFlight,
                EnableCaching = true,
                CacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5),
                Timeout = TimeSpan.FromSeconds(30)
            };

            return await ExecuteAsync(queryFunc, context);
        }

        /// <summary>
        /// Invalidates cached results for specific request key
        /// </summary>
        public async Task InvalidateCacheAsync(string requestKey)
        {
            if (string.IsNullOrWhiteSpace(requestKey))
                return;

            try
            {
                await _cacheService.RemoveAsync($"dedup:{requestKey}");
                _logger.LogDebug("🗑️ Invalidated cache for request: {RequestKey}", requestKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error invalidating cache for request: {RequestKey}", requestKey);
            }
        }

        /// <summary>
        /// Invalidates cached results matching pattern
        /// </summary>
        public async Task InvalidateCacheByPatternAsync(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return;

            try
            {
                await _cacheService.RemoveByPatternAsync($"dedup:{pattern}");
                _logger.LogDebug("🗑️ Invalidated cache by pattern: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error invalidating cache by pattern: {Pattern}", pattern);
            }
        }

        /// <summary>
        /// Cancels all in-flight requests for specific key
        /// </summary>
        public async Task CancelInFlightRequestsAsync(string requestKey)
        {
            if (string.IsNullOrWhiteSpace(requestKey))
                return;

            try
            {
                if (_cancellationTokens.TryRemove(requestKey, out var cancellationTokenSource))
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                    _logger.LogDebug("🚫 Cancelled in-flight requests for: {RequestKey}", requestKey);
                }

                if (_inFlightRequests.TryRemove(requestKey, out var taskCompletionSource))
                {
                    taskCompletionSource.TrySetCanceled();
                    DecrementInFlightCount();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error cancelling in-flight requests for: {RequestKey}", requestKey);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets current in-flight request count
        /// </summary>
        public async Task<int> GetInFlightRequestCountAsync()
        {
            await Task.CompletedTask;
            return _currentInFlightCount;
        }

        /// <summary>
        /// Gets comprehensive deduplication statistics
        /// </summary>
        public async Task<DeduplicationStatistics> GetStatisticsAsync()
        {
            _statistics.CurrentInFlightRequests = _currentInFlightCount;
            _statistics.MaxConcurrentRequests = _maxConcurrentRequests;
            
            await Task.CompletedTask;
            return _statistics;
        }

        /// <summary>
        /// Clears all cached results and cancels in-flight requests
        /// </summary>
        public async Task ClearAllAsync()
        {
            try
            {
                // Cancel all in-flight requests
                var cancellationTasks = _cancellationTokens.Keys.Select(CancelInFlightRequestsAsync);
                await Task.WhenAll(cancellationTasks);

                // Clear all cache entries
                await _cacheService.RemoveByPatternAsync("dedup:*");

                // Reset counters
                _duplicateCounters.Clear();
                _currentInFlightCount = 0;

                _logger.LogInformation("🧹 Cleared all deduplication cache and in-flight requests");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error clearing deduplication service");
                throw;
            }
        }

        /// <summary>
        /// Generates optimized request key with parameter hashing
        /// </summary>
        public string GenerateRequestKey(string baseKey, params object[] parameters)
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
                        keyBuilder.Append(ComputeHash(str));
                    }
                    else
                    {
                        keyBuilder.Append(param.ToString());
                    }
                }
                
                var finalKey = keyBuilder.ToString();
                
                // Hash very long keys
                if (finalKey.Length > 200)
                {
                    finalKey = $"{baseKey}:{ComputeHash(finalKey)}";
                }
                
                return finalKey;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error generating request key, using fallback");
                return $"{baseKey}:fallback:{Guid.NewGuid():N}";
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Tries to get cached result for request key
        /// </summary>
        private async Task<TResult?> TryGetCachedResultAsync<TResult>(string requestKey)
        {
            try
            {
                var cacheKey = $"dedup:{requestKey}";
                return await _cacheService.GetAsync<TResult>(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Cache lookup failed for request: {RequestKey}", requestKey);
                return default;
            }
        }

        /// <summary>
        /// Caches request result with specified duration
        /// </summary>
        private async Task CacheResultAsync<TResult>(string requestKey, TResult result, TimeSpan cacheDuration)
        {
            try
            {
                var cacheKey = $"dedup:{requestKey}";
                var policy = new CachePolicy
                {
                    ExpirationTime = cacheDuration,
                    Priority = CachePriority.Normal,
                    EnableBackgroundRefresh = false
                };
                
                await _cacheService.SetAsync(cacheKey, result, policy);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to cache result for request: {RequestKey}", requestKey);
            }
        }

        /// <summary>
        /// Waits for in-flight request to complete and returns result
        /// </summary>
        private async Task<TResult?> WaitForInFlightRequestAsync<TResult>(string requestKey)
        {
            if (_inFlightRequests.TryGetValue(requestKey, out var existingTask))
            {
                try
                {
                    var result = await existingTask.Task;
                    return result is TResult typedResult ? typedResult : default;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "In-flight request failed for: {RequestKey}", requestKey);
                    return default;
                }
            }
            
            return default;
        }

        /// <summary>
        /// Executes request with in-flight tracking
        /// </summary>
        private async Task<TResult> ExecuteRequestWithTrackingAsync<TResult>(
            Func<Task<TResult>> requestFunc,
            RequestContext context)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();
            var cancellationTokenSource = new CancellationTokenSource(context.Timeout);
            
            try
            {
                // Register in-flight request
                _inFlightRequests[context.RequestKey] = taskCompletionSource;
                _cancellationTokens[context.RequestKey] = cancellationTokenSource;
                IncrementInFlightCount();

                // Execute the request
                var result = await requestFunc();
                
                // Signal completion
                taskCompletionSource.TrySetResult(result!);
                
                return result;
            }
            catch (OperationCanceledException)
            {
                _statistics.TimeoutCount++;
                taskCompletionSource.TrySetCanceled();
                throw new TimeoutException($"Request {context.RequestKey} timed out after {context.Timeout}");
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetException(ex);
                throw;
            }
            finally
            {
                // Cleanup tracking
                _inFlightRequests.TryRemove(context.RequestKey, out _);
                _cancellationTokens.TryRemove(context.RequestKey, out _);
                DecrementInFlightCount();
                
                cancellationTokenSource.Dispose();
            }
        }

        /// <summary>
        /// Computes SHA256 hash for parameter values
        /// </summary>
        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes)[..12];
        }

        /// <summary>
        /// Updates strategy usage statistics
        /// </summary>
        private void UpdateStrategyStats(DeduplicationStrategy strategy)
        {
            if (!_statistics.StrategyUsageStats.ContainsKey(strategy))
                _statistics.StrategyUsageStats[strategy] = 0;
            
            _statistics.StrategyUsageStats[strategy]++;
        }

        /// <summary>
        /// Updates average request time statistics
        /// </summary>
        private void UpdateRequestTime(TimeSpan requestTime)
        {
            var currentAvg = _statistics.AverageRequestTime;
            _statistics.AverageRequestTime = TimeSpan.FromTicks((currentAvg.Ticks + requestTime.Ticks) / 2);
        }

        /// <summary>
        /// Updates average cached request time statistics
        /// </summary>
        private void UpdateCachedRequestTime(TimeSpan requestTime)
        {
            var currentAvg = _statistics.AverageCachedRequestTime;
            _statistics.AverageCachedRequestTime = TimeSpan.FromTicks((currentAvg.Ticks + requestTime.Ticks) / 2);
        }

        /// <summary>
        /// Increments in-flight request counter with max tracking
        /// </summary>
        private void IncrementInFlightCount()
        {
            var newCount = Interlocked.Increment(ref _currentInFlightCount);
            var currentMax = _maxConcurrentRequests;
            
            while (newCount > currentMax)
            {
                var originalMax = Interlocked.CompareExchange(ref _maxConcurrentRequests, newCount, currentMax);
                if (originalMax == currentMax)
                    break;
                currentMax = originalMax;
            }
        }

        /// <summary>
        /// Decrements in-flight request counter
        /// </summary>
        private void DecrementInFlightCount()
        {
            Interlocked.Decrement(ref _currentInFlightCount);
        }

        #endregion
    }
}