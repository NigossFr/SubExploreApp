using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Batch operation result with success/failure tracking
    /// </summary>
    /// <typeparam name="T">Type of operation result</typeparam>
    public class BatchOperationResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
        public string OperationId { get; set; } = string.Empty;
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Batch execution summary with performance metrics
    /// </summary>
    /// <typeparam name="T">Type of batch results</typeparam>
    public class BatchExecutionSummary<T>
    {
        public List<BatchOperationResult<T>> Results { get; set; } = new();
        public int SuccessCount => Results.Count(r => r.IsSuccess);
        public int FailureCount => Results.Count(r => !r.IsSuccess);
        public double SuccessRate => Results.Count > 0 ? (double)SuccessCount / Results.Count : 0;
        public TimeSpan TotalProcessingTime { get; set; }
        public TimeSpan AverageOperationTime { get; set; }
        public DateTime ExecutionStartTime { get; set; }
        public DateTime ExecutionEndTime { get; set; }
        public int BatchSize { get; set; }
        public string BatchId { get; set; } = Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Batch operation configuration
    /// </summary>
    public class BatchOperationConfig
    {
        /// <summary>Maximum number of operations per batch</summary>
        public int MaxBatchSize { get; set; } = 50;
        
        /// <summary>Maximum parallel operations</summary>
        public int MaxParallelism { get; set; } = Environment.ProcessorCount;
        
        /// <summary>Timeout for each operation</summary>
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);
        
        /// <summary>Whether to stop on first failure</summary>
        public bool FailFast { get; set; } = false;
        
        /// <summary>Whether to retry failed operations</summary>
        public bool EnableRetry { get; set; } = true;
        
        /// <summary>Maximum retry attempts</summary>
        public int MaxRetryAttempts { get; set; } = 3;
        
        /// <summary>Delay between retries</summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    }

    /// <summary>
    /// High-performance batch operation service for optimizing multiple operations
    /// Provides batching, parallel processing, retry logic, and comprehensive error handling
    /// </summary>
    public interface IBatchOperationService
    {
        /// <summary>
        /// Executes multiple operations in optimized batches with parallel processing
        /// </summary>
        /// <typeparam name="TInput">Input type for operations</typeparam>
        /// <typeparam name="TResult">Result type for operations</typeparam>
        /// <param name="operations">Dictionary of operation IDs and functions</param>
        /// <param name="config">Batch operation configuration</param>
        /// <returns>Batch execution summary with results</returns>
        Task<BatchExecutionSummary<TResult>> ExecuteBatchAsync<TInput, TResult>(
            Dictionary<string, Func<Task<TResult>>> operations,
            BatchOperationConfig? config = null);

        /// <summary>
        /// Executes multiple operations on a collection of items
        /// </summary>
        /// <typeparam name="TInput">Input item type</typeparam>
        /// <typeparam name="TResult">Result type</typeparam>
        /// <param name="items">Collection of items to process</param>
        /// <param name="operation">Operation function to apply to each item</param>
        /// <param name="config">Batch operation configuration</param>
        /// <returns>Batch execution summary with results</returns>
        Task<BatchExecutionSummary<TResult>> ExecuteBatchAsync<TInput, TResult>(
            IEnumerable<TInput> items,
            Func<TInput, Task<TResult>> operation,
            BatchOperationConfig? config = null);

        /// <summary>
        /// Executes database batch operations (Create, Update, Delete)
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="creates">Entities to create</param>
        /// <param name="updates">Entities to update</param>
        /// <param name="deletes">Entity IDs to delete</param>
        /// <param name="config">Batch operation configuration</param>
        /// <returns>Batch execution summary with operation counts</returns>
        Task<BatchExecutionSummary<string>> ExecuteDatabaseBatchAsync<TEntity>(
            IEnumerable<TEntity>? creates = null,
            IEnumerable<TEntity>? updates = null,
            IEnumerable<Guid>? deletes = null,
            BatchOperationConfig? config = null) where TEntity : class;

        /// <summary>
        /// Executes HTTP batch requests with automatic retries and error handling
        /// </summary>
        /// <param name="requests">HTTP request functions</param>
        /// <param name="config">Batch operation configuration</param>
        /// <returns>Batch execution summary with HTTP responses</returns>
        Task<BatchExecutionSummary<HttpResponseMessage>> ExecuteHttpBatchAsync(
            Dictionary<string, Func<Task<HttpResponseMessage>>> requests,
            BatchOperationConfig? config = null);

        /// <summary>
        /// Batches cache operations for improved performance
        /// </summary>
        /// <param name="cacheOperations">Dictionary of cache keys and operations</param>
        /// <param name="config">Batch operation configuration</param>
        /// <returns>Batch execution summary with cache results</returns>
        Task<BatchExecutionSummary<object>> ExecuteCacheBatchAsync(
            Dictionary<string, Func<Task<object>>> cacheOperations,
            BatchOperationConfig? config = null);

        /// <summary>
        /// Gets batch operation performance statistics
        /// </summary>
        /// <returns>Performance statistics for batch operations</returns>
        Task<BatchOperationStatistics> GetStatisticsAsync();

        /// <summary>
        /// Creates optimized configuration based on operation type and system resources
        /// </summary>
        /// <param name="operationType">Type of operations being batched</param>
        /// <param name="estimatedCount">Estimated number of operations</param>
        /// <returns>Optimized batch configuration</returns>
        BatchOperationConfig CreateOptimizedConfig(string operationType, int estimatedCount);
    }

    /// <summary>
    /// Batch operation performance statistics
    /// </summary>
    public class BatchOperationStatistics
    {
        public long TotalBatchesExecuted { get; set; }
        public long TotalOperationsExecuted { get; set; }
        public long TotalSuccessfulOperations { get; set; }
        public long TotalFailedOperations { get; set; }
        public double OverallSuccessRate => TotalOperationsExecuted > 0 ? (double)TotalSuccessfulOperations / TotalOperationsExecuted : 0;
        public TimeSpan AverageOperationTime { get; set; }
        public TimeSpan AverageBatchTime { get; set; }
        public Dictionary<string, long> OperationTypeStats { get; set; } = new();
        public Dictionary<string, TimeSpan> PerformanceMetrics { get; set; } = new();
        public DateTime LastResetTime { get; set; }
        public long RetryCount { get; set; }
        public long TimeoutCount { get; set; }

        /// <summary>
        /// Gets formatted statistics summary
        /// </summary>
        public string GetSummary()
        {
            return $"Batch Operations: {TotalBatchesExecuted} batches, " +
                   $"{TotalOperationsExecuted} operations, {OverallSuccessRate:P1} success rate, " +
                   $"Avg: {AverageOperationTime.TotalMilliseconds:F1}ms/op, " +
                   $"{RetryCount} retries, {TimeoutCount} timeouts";
        }
    }
}