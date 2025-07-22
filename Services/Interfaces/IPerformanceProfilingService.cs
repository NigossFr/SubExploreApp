using System.Diagnostics;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for comprehensive performance profiling and metrics collection
    /// </summary>
    public interface IPerformanceProfilingService
    {
        /// <summary>
        /// Start a performance measurement session
        /// </summary>
        /// <param name="operationName">Name of the operation to profile</param>
        /// <param name="category">Category of the operation (Database, UI, Network, etc.)</param>
        /// <returns>Performance tracking session ID</returns>
        string StartProfiling(string operationName, string category = "General");

        /// <summary>
        /// Stop a performance measurement session and log results
        /// </summary>
        /// <param name="sessionId">Session ID returned from StartProfiling</param>
        /// <param name="additionalMetrics">Optional additional metrics to log</param>
        void StopProfiling(string sessionId, Dictionary<string, object> additionalMetrics = null);

        /// <summary>
        /// Profile an async operation with automatic timing
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="operation">The operation to profile</param>
        /// <param name="category">Category of the operation</param>
        /// <returns>Result of the operation</returns>
        Task<T> ProfileAsync<T>(string operationName, Func<Task<T>> operation, string category = "General");

        /// <summary>
        /// Profile a synchronous operation with automatic timing
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="operation">The operation to profile</param>
        /// <param name="category">Category of the operation</param>
        /// <returns>Result of the operation</returns>
        T Profile<T>(string operationName, Func<T> operation, string category = "General");

        /// <summary>
        /// Log a custom metric
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="value">Metric value</param>
        /// <param name="unit">Unit of measurement</param>
        /// <param name="tags">Optional tags for categorization</param>
        void LogMetric(string metricName, double value, string unit = "", Dictionary<string, string> tags = null);

        /// <summary>
        /// Get performance statistics for a specific operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="category">Optional category filter</param>
        /// <returns>Performance statistics</returns>
        Task<PerformanceStats> GetPerformanceStatsAsync(string operationName, string category = null);

        /// <summary>
        /// Get system performance metrics
        /// </summary>
        /// <returns>Current system performance metrics</returns>
        SystemPerformanceMetrics GetSystemMetrics();

        /// <summary>
        /// Export performance data to JSON for analysis
        /// </summary>
        /// <param name="startDate">Start date for export</param>
        /// <param name="endDate">End date for export</param>
        /// <returns>JSON string containing performance data</returns>
        Task<string> ExportPerformanceDataAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Clear old performance data to prevent storage bloat
        /// </summary>
        /// <param name="olderThan">Remove data older than this date</param>
        Task CleanupOldDataAsync(DateTime olderThan);
    }

    /// <summary>
    /// Performance statistics for an operation
    /// </summary>
    public class PerformanceStats
    {
        public string OperationName { get; set; }
        public string Category { get; set; }
        public int ExecutionCount { get; set; }
        public double AverageExecutionTimeMs { get; set; }
        public double MinExecutionTimeMs { get; set; }
        public double MaxExecutionTimeMs { get; set; }
        public double StandardDeviation { get; set; }
        public double PercentileP95 { get; set; }
        public double PercentileP99 { get; set; }
        public DateTime FirstRecorded { get; set; }
        public DateTime LastRecorded { get; set; }
        public Dictionary<string, double> AdditionalMetrics { get; set; } = new();
    }

    /// <summary>
    /// System performance metrics
    /// </summary>
    public class SystemPerformanceMetrics
    {
        public double CpuUsagePercent { get; set; }
        public long MemoryUsageMB { get; set; }
        public long AvailableMemoryMB { get; set; }
        public double MemoryUsagePercent { get; set; }
        public long StorageUsedMB { get; set; }
        public long StorageAvailableMB { get; set; }
        public int ActiveThreadCount { get; set; }
        public long NetworkBytesReceived { get; set; }
        public long NetworkBytesSent { get; set; }
        public DateTime Timestamp { get; set; }
        public string Platform { get; set; }
        public string DeviceModel { get; set; }
        public string OSVersion { get; set; }
    }

    /// <summary>
    /// Internal performance tracking session
    /// </summary>
    internal class PerformanceSession
    {
        public string Id { get; set; }
        public string OperationName { get; set; }
        public string Category { get; set; }
        public Stopwatch Stopwatch { get; set; }
        public DateTime StartTime { get; set; }
        public long StartMemory { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }
}