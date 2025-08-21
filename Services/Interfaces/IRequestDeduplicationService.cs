using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Request deduplication strategy
    /// </summary>
    public enum DeduplicationStrategy
    {
        /// <summary>Return cached result immediately</summary>
        ReturnCached,
        
        /// <summary>Wait for in-flight request to complete</summary>
        WaitForInFlight,
        
        /// <summary>Cancel and restart request</summary>
        CancelAndRestart,
        
        /// <summary>Execute in parallel (no deduplication)</summary>
        ExecuteParallel
    }

    /// <summary>
    /// Request execution result with deduplication metadata
    /// </summary>
    /// <typeparam name="T">Type of request result</typeparam>
    public class DeduplicatedRequestResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
        public bool WasCached { get; set; }
        public bool WasDeduplicatedWithInFlight { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string RequestId { get; set; } = string.Empty;
        public DateTime RequestTime { get; set; }
        public int DuplicateCount { get; set; }
    }

    /// <summary>
    /// Request execution context
    /// </summary>
    public class RequestContext
    {
        public string RequestKey { get; set; } = string.Empty;
        public DeduplicationStrategy Strategy { get; set; } = DeduplicationStrategy.WaitForInFlight;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public bool EnableCaching { get; set; } = true;
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// High-performance request deduplication service
    /// Prevents redundant API calls, reduces server load, and improves response times
    /// </summary>
    public interface IRequestDeduplicationService
    {
        /// <summary>
        /// Executes request with deduplication logic
        /// </summary>
        /// <typeparam name="TResult">Type of request result</typeparam>
        /// <param name="requestFunc">Request function to execute</param>
        /// <param name="context">Request execution context</param>
        /// <returns>Deduplicated request result</returns>
        Task<DeduplicatedRequestResult<TResult>> ExecuteAsync<TResult>(
            Func<Task<TResult>> requestFunc,
            RequestContext context);

        /// <summary>
        /// Executes HTTP request with automatic deduplication
        /// </summary>
        /// <param name="httpRequestFunc">HTTP request function</param>
        /// <param name="requestKey">Unique request key</param>
        /// <param name="strategy">Deduplication strategy</param>
        /// <returns>Deduplicated HTTP response</returns>
        Task<DeduplicatedRequestResult<HttpResponseMessage>> ExecuteHttpRequestAsync(
            Func<Task<HttpResponseMessage>> httpRequestFunc,
            string requestKey,
            DeduplicationStrategy strategy = DeduplicationStrategy.WaitForInFlight);

        /// <summary>
        /// Executes database query with result deduplication
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="queryFunc">Database query function</param>
        /// <param name="queryKey">Unique query key</param>
        /// <param name="cacheDuration">Cache duration for results</param>
        /// <returns>Deduplicated query result</returns>
        Task<DeduplicatedRequestResult<TEntity>> ExecuteDatabaseQueryAsync<TEntity>(
            Func<Task<TEntity>> queryFunc,
            string queryKey,
            TimeSpan? cacheDuration = null);

        /// <summary>
        /// Invalidates cached results for specific request key
        /// </summary>
        /// <param name="requestKey">Request key to invalidate</param>
        Task InvalidateCacheAsync(string requestKey);

        /// <summary>
        /// Invalidates all cached results matching pattern
        /// </summary>
        /// <param name="pattern">Pattern to match request keys</param>
        Task InvalidateCacheByPatternAsync(string pattern);

        /// <summary>
        /// Cancels all in-flight requests for specific key
        /// </summary>
        /// <param name="requestKey">Request key to cancel</param>
        Task CancelInFlightRequestsAsync(string requestKey);

        /// <summary>
        /// Gets current in-flight request count
        /// </summary>
        /// <returns>Number of currently executing requests</returns>
        Task<int> GetInFlightRequestCountAsync();

        /// <summary>
        /// Gets deduplication service statistics
        /// </summary>
        /// <returns>Performance and usage statistics</returns>
        Task<DeduplicationStatistics> GetStatisticsAsync();

        /// <summary>
        /// Clears all cached results and cancels in-flight requests
        /// </summary>
        Task ClearAllAsync();

        /// <summary>
        /// Generates optimized request key from parameters
        /// </summary>
        /// <param name="baseKey">Base request identifier</param>
        /// <param name="parameters">Request parameters</param>
        /// <returns>Optimized request key</returns>
        string GenerateRequestKey(string baseKey, params object[] parameters);
    }

    /// <summary>
    /// Deduplication service performance statistics
    /// </summary>
    public class DeduplicationStatistics
    {
        public long TotalRequests { get; set; }
        public long DeduplicatedRequests { get; set; }
        public long CacheHits { get; set; }
        public long InFlightMerges { get; set; }
        public double DeduplicationRate => TotalRequests > 0 ? (double)DeduplicatedRequests / TotalRequests : 0;
        public double CacheHitRate => TotalRequests > 0 ? (double)CacheHits / TotalRequests : 0;
        public TimeSpan AverageRequestTime { get; set; }
        public TimeSpan AverageCachedRequestTime { get; set; }
        public int CurrentInFlightRequests { get; set; }
        public int MaxConcurrentRequests { get; set; }
        public Dictionary<DeduplicationStrategy, long> StrategyUsageStats { get; set; } = new();
        public Dictionary<string, long> RequestTypeStats { get; set; } = new();
        public DateTime LastResetTime { get; set; }
        public long ErrorCount { get; set; }
        public long TimeoutCount { get; set; }

        /// <summary>
        /// Gets formatted statistics summary
        /// </summary>
        public string GetSummary()
        {
            return $"Request Deduplication: {TotalRequests} total, " +
                   $"{DeduplicationRate:P1} deduplicated, {CacheHitRate:P1} cache hits, " +
                   $"{CurrentInFlightRequests} in-flight, {ErrorCount} errors";
        }
    }
}