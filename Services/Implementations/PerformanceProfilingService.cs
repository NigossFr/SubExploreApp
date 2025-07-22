using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Comprehensive performance profiling service for monitoring application performance
    /// </summary>
    public class PerformanceProfilingService : IPerformanceProfilingService
    {
        private readonly ILogger<PerformanceProfilingService> _logger;
        private readonly ConcurrentDictionary<string, PerformanceSession> _activeSessions = new();
        private readonly ConcurrentDictionary<string, List<PerformanceRecord>> _performanceHistory = new();
        private readonly object _lockObject = new object();

        public PerformanceProfilingService(ILogger<PerformanceProfilingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string StartProfiling(string operationName, string category = "General")
        {
            var sessionId = Guid.NewGuid().ToString();
            var session = new PerformanceSession
            {
                Id = sessionId,
                OperationName = operationName,
                Category = category,
                Stopwatch = Stopwatch.StartNew(),
                StartTime = DateTime.UtcNow,
                StartMemory = GC.GetTotalMemory(false)
            };

            _activeSessions[sessionId] = session;
            
            _logger.LogDebug("Started profiling session {SessionId} for operation {OperationName} in category {Category}", 
                sessionId, operationName, category);

            return sessionId;
        }

        public void StopProfiling(string sessionId, Dictionary<string, object> additionalMetrics = null)
        {
            if (!_activeSessions.TryRemove(sessionId, out var session))
            {
                _logger.LogWarning("Attempted to stop non-existent profiling session {SessionId}", sessionId);
                return;
            }

            session.Stopwatch.Stop();
            var endMemory = GC.GetTotalMemory(false);
            var memoryDelta = endMemory - session.StartMemory;

            var record = new PerformanceRecord
            {
                OperationName = session.OperationName,
                Category = session.Category,
                ExecutionTimeMs = session.Stopwatch.Elapsed.TotalMilliseconds,
                StartTime = session.StartTime,
                EndTime = DateTime.UtcNow,
                MemoryDeltaBytes = memoryDelta,
                AdditionalMetrics = additionalMetrics ?? new Dictionary<string, object>()
            };

            // Store performance record
            var key = $"{session.Category}::{session.OperationName}";
            lock (_lockObject)
            {
                if (!_performanceHistory.ContainsKey(key))
                {
                    _performanceHistory[key] = new List<PerformanceRecord>();
                }
                _performanceHistory[key].Add(record);

                // Keep only last 1000 records per operation to prevent memory bloat
                if (_performanceHistory[key].Count > 1000)
                {
                    _performanceHistory[key] = _performanceHistory[key]
                        .OrderByDescending(r => r.StartTime)
                        .Take(1000)
                        .ToList();
                }
            }

            _logger.LogInformation(
                "Performance: {Operation} [{Category}] completed in {ExecutionTime:F2}ms, Memory Δ: {MemoryDelta:F1}KB",
                session.OperationName,
                session.Category,
                record.ExecutionTimeMs,
                memoryDelta / 1024.0);

            // Log warning if operation took too long
            if (record.ExecutionTimeMs > 1000) // > 1 second
            {
                _logger.LogWarning(
                    "Slow operation detected: {Operation} [{Category}] took {ExecutionTime:F2}ms",
                    session.OperationName,
                    session.Category,
                    record.ExecutionTimeMs);
            }
        }

        public async Task<T> ProfileAsync<T>(string operationName, Func<Task<T>> operation, string category = "General")
        {
            var sessionId = StartProfiling(operationName, category);
            try
            {
                var result = await operation().ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during profiled operation {Operation} [{Category}]", operationName, category);
                StopProfiling(sessionId, new Dictionary<string, object> { { "ExceptionType", ex.GetType().Name } });
                throw;
            }
            finally
            {
                StopProfiling(sessionId);
            }
        }

        public T Profile<T>(string operationName, Func<T> operation, string category = "General")
        {
            var sessionId = StartProfiling(operationName, category);
            try
            {
                var result = operation();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during profiled operation {Operation} [{Category}]", operationName, category);
                StopProfiling(sessionId, new Dictionary<string, object> { { "ExceptionType", ex.GetType().Name } });
                throw;
            }
            finally
            {
                StopProfiling(sessionId);
            }
        }

        public void LogMetric(string metricName, double value, string unit = "", Dictionary<string, string> tags = null)
        {
            var tagsString = tags != null ? string.Join(", ", tags.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "";
            _logger.LogInformation("Metric: {MetricName} = {Value} {Unit} [{Tags}]", 
                metricName, value, unit, tagsString);
        }

        public async Task<PerformanceStats> GetPerformanceStatsAsync(string operationName, string category = null)
        {
            await Task.Yield(); // Make async for future database integration

            string key;
            if (category != null)
            {
                key = $"{category}::{operationName}";
            }
            else
            {
                // Find first matching operation across all categories
                key = _performanceHistory.Keys.FirstOrDefault(k => k.EndsWith($"::{operationName}"));
            }

            if (key == null || !_performanceHistory.ContainsKey(key))
            {
                return null;
            }

            lock (_lockObject)
            {
                var records = _performanceHistory[key];
                if (!records.Any()) return null;

                var executionTimes = records.Select(r => r.ExecutionTimeMs).OrderBy(t => t).ToList();
                var avgTime = executionTimes.Average();
                var minTime = executionTimes.Min();
                var maxTime = executionTimes.Max();

                // Calculate standard deviation
                var variance = executionTimes.Select(t => Math.Pow(t - avgTime, 2)).Average();
                var stdDev = Math.Sqrt(variance);

                // Calculate percentiles
                var p95Index = (int)Math.Ceiling(executionTimes.Count * 0.95) - 1;
                var p99Index = (int)Math.Ceiling(executionTimes.Count * 0.99) - 1;
                var p95 = executionTimes[Math.Max(0, Math.Min(p95Index, executionTimes.Count - 1))];
                var p99 = executionTimes[Math.Max(0, Math.Min(p99Index, executionTimes.Count - 1))];

                var parts = key.Split("::");
                var stats = new PerformanceStats
                {
                    OperationName = operationName,
                    Category = parts.Length > 1 ? parts[0] : "General",
                    ExecutionCount = records.Count,
                    AverageExecutionTimeMs = avgTime,
                    MinExecutionTimeMs = minTime,
                    MaxExecutionTimeMs = maxTime,
                    StandardDeviation = stdDev,
                    PercentileP95 = p95,
                    PercentileP99 = p99,
                    FirstRecorded = records.Min(r => r.StartTime),
                    LastRecorded = records.Max(r => r.StartTime)
                };

                // Aggregate additional metrics
                var allAdditionalMetrics = records
                    .SelectMany(r => r.AdditionalMetrics)
                    .Where(kvp => kvp.Value is double or int or float or decimal)
                    .GroupBy(kvp => kvp.Key)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(kvp => Convert.ToDouble(kvp.Value)).Average()
                    );

                stats.AdditionalMetrics = allAdditionalMetrics;

                return stats;
            }
        }

        public SystemPerformanceMetrics GetSystemMetrics()
        {
            var process = Process.GetCurrentProcess();
            
            // Get memory info
            var workingSet = process.WorkingSet64;
            var totalMemory = GC.GetTotalMemory(false);

            // Get available memory (platform-specific approximation)
            var availableMemory = GetAvailableMemory();

            var metrics = new SystemPerformanceMetrics
            {
                CpuUsagePercent = GetCpuUsage(),
                MemoryUsageMB = workingSet / (1024 * 1024),
                AvailableMemoryMB = availableMemory / (1024 * 1024),
                MemoryUsagePercent = availableMemory > 0 ? (double)workingSet / availableMemory * 100 : 0,
                ActiveThreadCount = process.Threads.Count,
                Timestamp = DateTime.UtcNow,
                Platform = DeviceInfo.Platform.ToString(),
                DeviceModel = DeviceInfo.Model,
                OSVersion = DeviceInfo.VersionString
            };

            return metrics;
        }

        public async Task<string> ExportPerformanceDataAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            await Task.Yield();

            var exportData = new
            {
                ExportTimestamp = DateTime.UtcNow,
                StartDate = startDate,
                EndDate = endDate,
                SystemMetrics = GetSystemMetrics(),
                PerformanceData = await GetFilteredPerformanceDataAsync(startDate, endDate)
            };

            return JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        public async Task CleanupOldDataAsync(DateTime olderThan)
        {
            await Task.Yield();

            lock (_lockObject)
            {
                foreach (var key in _performanceHistory.Keys.ToList())
                {
                    var records = _performanceHistory[key];
                    var filteredRecords = records.Where(r => r.StartTime >= olderThan).ToList();
                    
                    if (filteredRecords.Any())
                    {
                        _performanceHistory[key] = filteredRecords;
                    }
                    else
                    {
                        _performanceHistory.TryRemove(key, out _);
                    }
                }
            }

            _logger.LogInformation("Cleaned up performance data older than {OlderThan}", olderThan);
        }

        private async Task<Dictionary<string, List<PerformanceRecord>>> GetFilteredPerformanceDataAsync(DateTime? startDate, DateTime? endDate)
        {
            await Task.Yield();

            lock (_lockObject)
            {
                var filteredData = new Dictionary<string, List<PerformanceRecord>>();

                foreach (var kvp in _performanceHistory)
                {
                    var filteredRecords = kvp.Value.AsEnumerable();

                    if (startDate.HasValue)
                        filteredRecords = filteredRecords.Where(r => r.StartTime >= startDate.Value);

                    if (endDate.HasValue)
                        filteredRecords = filteredRecords.Where(r => r.StartTime <= endDate.Value);

                    var recordList = filteredRecords.ToList();
                    if (recordList.Any())
                    {
                        filteredData[kvp.Key] = recordList;
                    }
                }

                return filteredData;
            }
        }

        private double GetCpuUsage()
        {
            try
            {
                // Platform-specific CPU usage approximation
                // This is a simplified version - in production, you might want more accurate measurements
                var process = Process.GetCurrentProcess();
                return process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / 1000.0 * 100;
            }
            catch
            {
                return 0;
            }
        }

        private long GetAvailableMemory()
        {
            try
            {
                // Platform-specific memory detection
                // This is a simplified approximation
                return 2L * 1024 * 1024 * 1024; // Assume 2GB available as baseline
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Internal performance record for storing execution data
    /// </summary>
    internal class PerformanceRecord
    {
        public string OperationName { get; set; }
        public string Category { get; set; }
        public double ExecutionTimeMs { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long MemoryDeltaBytes { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }
}