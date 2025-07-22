using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Application-specific performance monitoring service with domain-specific metrics
    /// </summary>
    public class ApplicationPerformanceService : IApplicationPerformanceService
    {
        private readonly IPerformanceProfilingService _performanceService;
        private readonly ILogger<ApplicationPerformanceService> _logger;
        private readonly ConcurrentDictionary<string, ApplicationMetric> _applicationMetrics = new();
        private readonly Timer _periodicStatsTimer;

        public ApplicationPerformanceService(
            IPerformanceProfilingService performanceService, 
            ILogger<ApplicationPerformanceService> logger)
        {
            _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Start periodic stats collection every 30 seconds
            _periodicStatsTimer = new Timer(CollectPeriodicStats, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        public void TrackSpotLoad(int spotCount, double loadTimeMs, string region = "Unknown")
        {
            var sessionId = _performanceService.StartProfiling($"SpotLoad-{region}", "Map");
            
            var additionalMetrics = new Dictionary<string, object>
            {
                { "SpotCount", spotCount },
                { "LoadTimeMs", loadTimeMs },
                { "Region", region },
                { "SpotsPerSecond", spotCount / (loadTimeMs / 1000.0) }
            };

            _performanceService.StopProfiling(sessionId, additionalMetrics);

            // Update application-specific metrics
            UpdateApplicationMetric("SpotLoads", spotCount, loadTimeMs, new Dictionary<string, string> 
            { 
                { "region", region } 
            });

            _logger.LogInformation(
                "Spot loading performance: {SpotCount} spots loaded in {LoadTime:F2}ms from {Region} ({Rate:F1} spots/sec)",
                spotCount, loadTimeMs, region, spotCount / (loadTimeMs / 1000.0));
        }

        public void TrackMapRender(double renderTimeMs, int pinCount, double zoomLevel)
        {
            var sessionId = _performanceService.StartProfiling("MapRender", "UI");
            
            var additionalMetrics = new Dictionary<string, object>
            {
                { "RenderTimeMs", renderTimeMs },
                { "PinCount", pinCount },
                { "ZoomLevel", zoomLevel },
                { "PinsPerSecond", pinCount / (renderTimeMs / 1000.0) }
            };

            _performanceService.StopProfiling(sessionId, additionalMetrics);

            UpdateApplicationMetric("MapRenders", pinCount, renderTimeMs, new Dictionary<string, string> 
            { 
                { "zoomLevel", zoomLevel.ToString("F1") } 
            });

            if (renderTimeMs > 2000) // > 2 seconds
            {
                _logger.LogWarning(
                    "Slow map render detected: {RenderTime:F2}ms for {PinCount} pins at zoom {ZoomLevel}",
                    renderTimeMs, pinCount, zoomLevel);
            }
        }

        public void TrackDatabaseQuery(string queryType, string tableName, double queryTimeMs, int recordCount = 0)
        {
            var operationName = $"Query-{queryType}-{tableName}";
            var sessionId = _performanceService.StartProfiling(operationName, "Database");
            
            var additionalMetrics = new Dictionary<string, object>
            {
                { "QueryType", queryType },
                { "TableName", tableName },
                { "QueryTimeMs", queryTimeMs },
                { "RecordCount", recordCount }
            };

            if (recordCount > 0)
            {
                additionalMetrics["RecordsPerSecond"] = recordCount / (queryTimeMs / 1000.0);
            }

            _performanceService.StopProfiling(sessionId, additionalMetrics);

            UpdateApplicationMetric("DatabaseQueries", recordCount, queryTimeMs, new Dictionary<string, string> 
            { 
                { "queryType", queryType },
                { "table", tableName }
            });

            // Log slow queries
            if (queryTimeMs > 1000) // > 1 second
            {
                _logger.LogWarning(
                    "Slow database query: {QueryType} on {Table} took {QueryTime:F2}ms returning {RecordCount} records",
                    queryType, tableName, queryTimeMs, recordCount);
            }
        }

        public void TrackUINavigation(string fromPage, string toPage, double navigationTimeMs)
        {
            var sessionId = _performanceService.StartProfiling($"Navigation-{fromPage}-to-{toPage}", "UI");
            
            var additionalMetrics = new Dictionary<string, object>
            {
                { "FromPage", fromPage },
                { "ToPage", toPage },
                { "NavigationTimeMs", navigationTimeMs }
            };

            _performanceService.StopProfiling(sessionId, additionalMetrics);

            UpdateApplicationMetric("UINavigations", 1, navigationTimeMs, new Dictionary<string, string> 
            { 
                { "route", $"{fromPage}-to-{toPage}" }
            });

            if (navigationTimeMs > 1500) // > 1.5 seconds
            {
                _logger.LogWarning(
                    "Slow UI navigation: {FromPage} to {ToPage} took {NavigationTime:F2}ms",
                    fromPage, toPage, navigationTimeMs);
            }
        }

        public void TrackMediaOperation(string operationType, string mediaType, double operationTimeMs, long fileSizeBytes = 0)
        {
            var sessionId = _performanceService.StartProfiling($"Media-{operationType}-{mediaType}", "Media");
            
            var additionalMetrics = new Dictionary<string, object>
            {
                { "OperationType", operationType },
                { "MediaType", mediaType },
                { "OperationTimeMs", operationTimeMs },
                { "FileSizeBytes", fileSizeBytes }
            };

            if (fileSizeBytes > 0)
            {
                additionalMetrics["ProcessingRateMBps"] = (fileSizeBytes / (1024.0 * 1024.0)) / (operationTimeMs / 1000.0);
            }

            _performanceService.StopProfiling(sessionId, additionalMetrics);

            UpdateApplicationMetric("MediaOperations", 1, operationTimeMs, new Dictionary<string, string> 
            { 
                { "operation", operationType },
                { "mediaType", mediaType }
            });

            _logger.LogInformation(
                "Media operation: {Operation} on {MediaType} completed in {OperationTime:F2}ms (Size: {FileSize:F1}KB)",
                operationType, mediaType, operationTimeMs, fileSizeBytes / 1024.0);
        }

        public void TrackUserAction(string actionType, string actionDetails, double responseTimeMs)
        {
            var sessionId = _performanceService.StartProfiling($"UserAction-{actionType}", "UserInteraction");
            
            var additionalMetrics = new Dictionary<string, object>
            {
                { "ActionType", actionType },
                { "ActionDetails", actionDetails },
                { "ResponseTimeMs", responseTimeMs }
            };

            _performanceService.StopProfiling(sessionId, additionalMetrics);

            UpdateApplicationMetric("UserActions", 1, responseTimeMs, new Dictionary<string, string> 
            { 
                { "actionType", actionType }
            });

            // Track user experience
            string userExperience = responseTimeMs switch
            {
                <= 100 => "Excellent",
                <= 300 => "Good", 
                <= 1000 => "Acceptable",
                _ => "Poor"
            };

            _performanceService.LogMetric($"UserExperience-{actionType}", responseTimeMs, "ms", 
                new Dictionary<string, string> { { "experience", userExperience } });
        }

        public async Task<ApplicationPerformanceSummary> GetPerformanceSummaryAsync(TimeSpan? period = null)
        {
            var actualPeriod = period ?? TimeSpan.FromHours(1);
            var cutoffTime = DateTime.UtcNow - actualPeriod;

            var summary = new ApplicationPerformanceSummary
            {
                GeneratedAt = DateTime.UtcNow,
                Period = actualPeriod,
                SystemMetrics = _performanceService.GetSystemMetrics()
            };

            // Get performance stats for key operations
            var keyOperations = new[]
            {
                ("SpotLoad", "Map"),
                ("MapRender", "UI"),
                ("Query-SELECT-Spots", "Database"),
                ("Navigation", "UI"),
                ("UserAction", "UserInteraction")
            };

            foreach (var (operation, category) in keyOperations)
            {
                try
                {
                    var stats = await _performanceService.GetPerformanceStatsAsync(operation, category);
                    if (stats != null)
                    {
                        summary.OperationStats[operation] = stats;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting performance stats for {Operation}", operation);
                }
            }

            // Add application-specific metrics
            var applicationMetricsList = new List<ApplicationMetricSummary>();
            foreach (var kvp in _applicationMetrics)
            {
                var metric = kvp.Value;
                if (metric.LastUpdated >= cutoffTime)
                {
                    applicationMetricsList.Add(new ApplicationMetricSummary
                    {
                        Name = kvp.Key,
                        Count = metric.Count,
                        AverageValue = metric.TotalValue / metric.Count,
                        AverageTimeMs = metric.TotalTimeMs / metric.Count,
                        LastUpdated = metric.LastUpdated,
                        Tags = metric.Tags
                    });
                }
            }

            summary.ApplicationMetrics = applicationMetricsList;

            return summary;
        }

        private void UpdateApplicationMetric(string metricName, double value, double timeMs, Dictionary<string, string> tags)
        {
            var key = $"{metricName}:{string.Join(",", tags.Select(kvp => $"{kvp.Key}={kvp.Value}"))}";
            
            _applicationMetrics.AddOrUpdate(key, 
                new ApplicationMetric
                {
                    Name = metricName,
                    Count = 1,
                    TotalValue = value,
                    TotalTimeMs = timeMs,
                    LastUpdated = DateTime.UtcNow,
                    Tags = tags
                },
                (k, existing) =>
                {
                    existing.Count++;
                    existing.TotalValue += value;
                    existing.TotalTimeMs += timeMs;
                    existing.LastUpdated = DateTime.UtcNow;
                    return existing;
                });
        }

        private void CollectPeriodicStats(object state)
        {
            try
            {
                var systemMetrics = _performanceService.GetSystemMetrics();
                
                // Log system metrics
                _performanceService.LogMetric("System.CPU.Usage", systemMetrics.CpuUsagePercent, "%");
                _performanceService.LogMetric("System.Memory.Usage", systemMetrics.MemoryUsagePercent, "%");
                _performanceService.LogMetric("System.Memory.Available", systemMetrics.AvailableMemoryMB, "MB");
                _performanceService.LogMetric("System.Threads.Active", systemMetrics.ActiveThreadCount, "count");

                // Clean up old application metrics (keep last hour)
                var cutoffTime = DateTime.UtcNow - TimeSpan.FromHours(1);
                var keysToRemove = _applicationMetrics
                    .Where(kvp => kvp.Value.LastUpdated < cutoffTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _applicationMetrics.TryRemove(key, out _);
                }

                _logger.LogDebug("Collected periodic performance stats. Active metrics: {MetricCount}", _applicationMetrics.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting periodic performance stats");
            }
        }

        public void Dispose()
        {
            _periodicStatsTimer?.Dispose();
        }
    }

    public interface IApplicationPerformanceService : IDisposable
    {
        void TrackSpotLoad(int spotCount, double loadTimeMs, string region = "Unknown");
        void TrackMapRender(double renderTimeMs, int pinCount, double zoomLevel);
        void TrackDatabaseQuery(string queryType, string tableName, double queryTimeMs, int recordCount = 0);
        void TrackUINavigation(string fromPage, string toPage, double navigationTimeMs);
        void TrackMediaOperation(string operationType, string mediaType, double operationTimeMs, long fileSizeBytes = 0);
        void TrackUserAction(string actionType, string actionDetails, double responseTimeMs);
        Task<ApplicationPerformanceSummary> GetPerformanceSummaryAsync(TimeSpan? period = null);
    }

    public class ApplicationMetric
    {
        public string Name { get; set; }
        public long Count { get; set; }
        public double TotalValue { get; set; }
        public double TotalTimeMs { get; set; }
        public DateTime LastUpdated { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    public class ApplicationMetricSummary
    {
        public string Name { get; set; }
        public long Count { get; set; }
        public double AverageValue { get; set; }
        public double AverageTimeMs { get; set; }
        public DateTime LastUpdated { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    public class ApplicationPerformanceSummary
    {
        public DateTime GeneratedAt { get; set; }
        public TimeSpan Period { get; set; }
        public SystemPerformanceMetrics SystemMetrics { get; set; }
        public Dictionary<string, PerformanceStats> OperationStats { get; set; } = new();
        public List<ApplicationMetricSummary> ApplicationMetrics { get; set; } = new();
    }
}