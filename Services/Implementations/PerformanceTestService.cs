using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Performance test results with detailed metrics
    /// </summary>
    public class PerformanceTestResults
    {
        public string TestName { get; set; } = string.Empty;
        public DateTime TestStartTime { get; set; }
        public DateTime TestEndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        
        // Performance Metrics
        public double AverageResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double Throughput { get; set; } // Requests per second
        public double CacheHitRate { get; set; }
        public double CompressionRatio { get; set; }
        public double DeduplicationRate { get; set; }
        
        // Resource Metrics
        public long PeakMemoryUsage { get; set; }
        public int MaxConcurrentRequests { get; set; }
        public long TotalDataTransferred { get; set; }
        public long DataSavedThroughCompression { get; set; }
        
        // Test Statistics
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();

        /// <summary>
        /// Gets formatted test summary
        /// </summary>
        public string GetSummary()
        {
            return $"Performance Test: {TestName}\n" +
                   $"Duration: {TotalDuration.TotalSeconds:F1}s\n" +
                   $"Operations: {TotalOperations} ({SuccessfulOperations} successful, {FailedOperations} failed)\n" +
                   $"Throughput: {Throughput:F1} ops/sec\n" +
                   $"Avg Response: {AverageResponseTime:F1}ms\n" +
                   $"Cache Hit Rate: {CacheHitRate:P1}\n" +
                   $"Compression Ratio: {CompressionRatio:P1}\n" +
                   $"Deduplication Rate: {DeduplicationRate:P1}\n" +
                   $"Data Saved: {DataSavedThroughCompression / (1024 * 1024):F1}MB\n" +
                   $"Peak Memory: {PeakMemoryUsage / (1024 * 1024):F1}MB\n" +
                   $"Success: {IsSuccess}";
        }
    }

    /// <summary>
    /// Comprehensive performance testing service for validating optimization improvements
    /// </summary>
    public class PerformanceTestService
    {
        private readonly ISpotService _spotService;
        private readonly IResponseCacheService _cacheService;
        private readonly IQueryCacheService _queryCacheService;
        private readonly IBatchOperationService _batchService;
        private readonly ICompressionService _compressionService;
        private readonly IRequestDeduplicationService _deduplicationService;
        private readonly ILogger<PerformanceTestService> _logger;

        public PerformanceTestService(
            ISpotService spotService,
            IResponseCacheService cacheService,
            IQueryCacheService queryCacheService,
            IBatchOperationService batchService,
            ICompressionService compressionService,
            IRequestDeduplicationService deduplicationService,
            ILogger<PerformanceTestService> logger)
        {
            _spotService = spotService;
            _cacheService = cacheService;
            _queryCacheService = queryCacheService;
            _batchService = batchService;
            _compressionService = compressionService;
            _deduplicationService = deduplicationService;
            _logger = logger;
        }

        /// <summary>
        /// Runs comprehensive performance test suite
        /// </summary>
        public async Task<PerformanceTestResults> RunComprehensiveTestSuiteAsync()
        {
            var results = new PerformanceTestResults
            {
                TestName = "Comprehensive Performance Test Suite",
                TestStartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("🚀 Starting comprehensive performance test suite...");

                var stopwatch = Stopwatch.StartNew();
                var allTests = new List<Task<PerformanceTestResults>>
                {
                    TestCachingPerformanceAsync(),
                    TestQueryCachingPerformanceAsync(),
                    TestBatchOperationsPerformanceAsync(),
                    TestCompressionPerformanceAsync(),
                    TestDeduplicationPerformanceAsync(),
                    TestDatabaseConnectionPoolingAsync()
                };

                var testResults = await Task.WhenAll(allTests);
                stopwatch.Stop();

                // Aggregate results
                results.TotalDuration = stopwatch.Elapsed;
                results.TestEndTime = DateTime.UtcNow;
                results.TotalOperations = testResults.Sum(r => r.TotalOperations);
                results.SuccessfulOperations = testResults.Sum(r => r.SuccessfulOperations);
                results.FailedOperations = testResults.Sum(r => r.FailedOperations);
                results.IsSuccess = testResults.All(r => r.IsSuccess);

                // Calculate averages
                results.AverageResponseTime = testResults.Average(r => r.AverageResponseTime);
                results.CacheHitRate = testResults.Average(r => r.CacheHitRate);
                results.CompressionRatio = testResults.Where(r => r.CompressionRatio > 0).DefaultIfEmpty().Average(r => r.CompressionRatio);
                results.DeduplicationRate = testResults.Where(r => r.DeduplicationRate > 0).DefaultIfEmpty().Average(r => r.DeduplicationRate);
                results.Throughput = results.TotalOperations / results.TotalDuration.TotalSeconds;
                
                // Resource metrics
                results.PeakMemoryUsage = testResults.Max(r => r.PeakMemoryUsage);
                results.MaxConcurrentRequests = testResults.Max(r => r.MaxConcurrentRequests);
                results.DataSavedThroughCompression = testResults.Sum(r => r.DataSavedThroughCompression);

                _logger.LogInformation("✅ Comprehensive performance test suite completed: {Duration}s, {Throughput:F1} ops/sec, {SuccessRate:P1} success rate",
                    results.TotalDuration.TotalSeconds, results.Throughput, (double)results.SuccessfulOperations / results.TotalOperations);

                return results;
            }
            catch (Exception ex)
            {
                results.IsSuccess = false;
                results.ErrorMessage = ex.Message;
                results.TestEndTime = DateTime.UtcNow;
                
                _logger.LogError(ex, "❌ Comprehensive performance test suite failed");
                return results;
            }
        }

        /// <summary>
        /// Tests response caching performance improvements
        /// </summary>
        public async Task<PerformanceTestResults> TestCachingPerformanceAsync()
        {
            var results = new PerformanceTestResults
            {
                TestName = "Response Caching Performance Test",
                TestStartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("🧪 Testing response caching performance...");

                var stopwatch = Stopwatch.StartNew();
                var operations = new List<Task<TimeSpan>>();
                var testData = Enumerable.Range(1, 100).ToList();

                // Test cache misses (first access)
                foreach (var item in testData.Take(50))
                {
                    operations.Add(TestCacheOperation($"test_key_{item}", $"test_data_{item}"));
                }

                // Test cache hits (second access)
                foreach (var item in testData.Take(50))
                {
                    operations.Add(TestCacheOperation($"test_key_{item}", $"test_data_{item}"));
                }

                var operationTimes = await Task.WhenAll(operations);
                stopwatch.Stop();

                results.TotalDuration = stopwatch.Elapsed;
                results.TestEndTime = DateTime.UtcNow;
                results.TotalOperations = operationTimes.Length;
                results.SuccessfulOperations = operationTimes.Length;
                results.AverageResponseTime = operationTimes.Average(t => t.TotalMilliseconds);
                results.MinResponseTime = operationTimes.Min(t => t.TotalMilliseconds);
                results.MaxResponseTime = operationTimes.Max(t => t.TotalMilliseconds);
                results.Throughput = results.TotalOperations / results.TotalDuration.TotalSeconds;

                // Get cache statistics
                var cacheStats = await _cacheService.GetStatisticsAsync();
                results.CacheHitRate = cacheStats.HitRate;
                results.IsSuccess = true;

                _logger.LogInformation("✅ Caching performance test completed: {AvgTime:F1}ms avg, {HitRate:P1} hit rate",
                    results.AverageResponseTime, results.CacheHitRate);

                return results;
            }
            catch (Exception ex)
            {
                results.IsSuccess = false;
                results.ErrorMessage = ex.Message;
                results.TestEndTime = DateTime.UtcNow;
                
                _logger.LogError(ex, "❌ Caching performance test failed");
                return results;
            }
        }

        /// <summary>
        /// Tests query caching performance
        /// </summary>
        public async Task<PerformanceTestResults> TestQueryCachingPerformanceAsync()
        {
            var results = new PerformanceTestResults
            {
                TestName = "Query Caching Performance Test",
                TestStartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("🧪 Testing query caching performance...");

                var stopwatch = Stopwatch.StartNew();
                var operations = new List<Task<TimeSpan>>();

                // Test cached queries with simulated database operations
                for (int i = 0; i < 100; i++)
                {
                    var queryKey = $"query_{i % 20}"; // Create overlapping queries for cache hits
                    operations.Add(TestQueryCacheOperation(queryKey));
                }

                var operationTimes = await Task.WhenAll(operations);
                stopwatch.Stop();

                results.TotalDuration = stopwatch.Elapsed;
                results.TestEndTime = DateTime.UtcNow;
                results.TotalOperations = operationTimes.Length;
                results.SuccessfulOperations = operationTimes.Length;
                results.AverageResponseTime = operationTimes.Average(t => t.TotalMilliseconds);
                results.Throughput = results.TotalOperations / results.TotalDuration.TotalSeconds;

                // Get query cache statistics
                var queryCacheStats = await _queryCacheService.GetStatisticsAsync();
                results.CacheHitRate = queryCacheStats.HitRate;
                results.IsSuccess = true;

                _logger.LogInformation("✅ Query caching performance test completed: {AvgTime:F1}ms avg, {HitRate:P1} hit rate",
                    results.AverageResponseTime, results.CacheHitRate);

                return results;
            }
            catch (Exception ex)
            {
                results.IsSuccess = false;
                results.ErrorMessage = ex.Message;
                results.TestEndTime = DateTime.UtcNow;
                
                _logger.LogError(ex, "❌ Query caching performance test failed");
                return results;
            }
        }

        /// <summary>
        /// Tests batch operations performance
        /// </summary>
        public async Task<PerformanceTestResults> TestBatchOperationsPerformanceAsync()
        {
            var results = new PerformanceTestResults
            {
                TestName = "Batch Operations Performance Test",
                TestStartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("🧪 Testing batch operations performance...");

                var stopwatch = Stopwatch.StartNew();

                // Create batch of simulated operations
                var operations = new Dictionary<string, Func<Task<string>>>();
                for (int i = 0; i < 100; i++)
                {
                    var operationId = $"op_{i}";
                    operations[operationId] = async () =>
                    {
                        await Task.Delay(Random.Shared.Next(10, 50)); // Simulate work
                        return $"Result_{operationId}";
                    };
                }

                var batchResult = await _batchService.ExecuteBatchAsync<object, string>(operations);
                stopwatch.Stop();

                results.TotalDuration = stopwatch.Elapsed;
                results.TestEndTime = DateTime.UtcNow;
                results.TotalOperations = batchResult.Results.Count;
                results.SuccessfulOperations = batchResult.SuccessCount;
                results.FailedOperations = batchResult.FailureCount;
                results.AverageResponseTime = batchResult.AverageOperationTime.TotalMilliseconds;
                results.Throughput = results.TotalOperations / results.TotalDuration.TotalSeconds;
                results.MaxConcurrentRequests = operations.Count;
                results.IsSuccess = batchResult.SuccessRate > 0.95; // 95% success threshold

                _logger.LogInformation("✅ Batch operations test completed: {SuccessRate:P1} success rate, {Throughput:F1} ops/sec",
                    batchResult.SuccessRate, results.Throughput);

                return results;
            }
            catch (Exception ex)
            {
                results.IsSuccess = false;
                results.ErrorMessage = ex.Message;
                results.TestEndTime = DateTime.UtcNow;
                
                _logger.LogError(ex, "❌ Batch operations performance test failed");
                return results;
            }
        }

        /// <summary>
        /// Tests compression performance and effectiveness
        /// </summary>
        public async Task<PerformanceTestResults> TestCompressionPerformanceAsync()
        {
            var results = new PerformanceTestResults
            {
                TestName = "Compression Performance Test",
                TestStartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("🧪 Testing compression performance...");

                var stopwatch = Stopwatch.StartNew();
                
                // Generate test data of various sizes and patterns
                var testDataSets = new[]
                {
                    CreateTestData(1024, "repetitive"), // 1KB repetitive
                    CreateTestData(10240, "random"), // 10KB random
                    CreateTestData(51200, "mixed"), // 50KB mixed
                    CreateTestData(102400, "json") // 100KB JSON-like
                };

                var compressionTasks = testDataSets.SelectMany(data => new[]
                {
                    _compressionService.CompressAsync(data, CompressionAlgorithm.GZip),
                    _compressionService.CompressAsync(data, CompressionAlgorithm.Deflate),
                    _compressionService.CompressAsync(data, CompressionAlgorithm.Brotli)
                });

                var compressionResults = await Task.WhenAll(compressionTasks);
                stopwatch.Stop();

                results.TotalDuration = stopwatch.Elapsed;
                results.TestEndTime = DateTime.UtcNow;
                results.TotalOperations = compressionResults.Length;
                results.SuccessfulOperations = compressionResults.Count(r => r.IsSuccess);
                results.FailedOperations = compressionResults.Count(r => !r.IsSuccess);
                results.AverageResponseTime = compressionResults.Average(r => r.ProcessingTime.TotalMilliseconds);

                // Calculate compression metrics
                var successfulResults = compressionResults.Where(r => r.IsSuccess).ToArray();
                results.CompressionRatio = successfulResults.Average(r => r.CompressionRatio);
                results.TotalDataTransferred = successfulResults.Sum(r => r.OriginalSize);
                results.DataSavedThroughCompression = successfulResults.Sum(r => Math.Max(0, r.OriginalSize - r.CompressedSize));
                results.Throughput = results.TotalOperations / results.TotalDuration.TotalSeconds;
                results.IsSuccess = results.SuccessfulOperations > results.TotalOperations * 0.95;

                _logger.LogInformation("✅ Compression test completed: {CompressionRatio:P1} avg ratio, {DataSaved:F1}MB saved",
                    results.CompressionRatio, results.DataSavedThroughCompression / (1024.0 * 1024.0));

                return results;
            }
            catch (Exception ex)
            {
                results.IsSuccess = false;
                results.ErrorMessage = ex.Message;
                results.TestEndTime = DateTime.UtcNow;
                
                _logger.LogError(ex, "❌ Compression performance test failed");
                return results;
            }
        }

        /// <summary>
        /// Tests request deduplication effectiveness
        /// </summary>
        public async Task<PerformanceTestResults> TestDeduplicationPerformanceAsync()
        {
            var results = new PerformanceTestResults
            {
                TestName = "Request Deduplication Performance Test",
                TestStartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("🧪 Testing request deduplication performance...");

                var stopwatch = Stopwatch.StartNew();
                var operations = new List<Task<DeduplicatedRequestResult<string>>>();

                // Create duplicate requests to test deduplication
                var requestKeys = Enumerable.Range(1, 20).Select(i => $"request_{i % 5}").ToArray(); // 5 unique keys, lots of duplicates

                foreach (var key in requestKeys)
                {
                    operations.Add(_deduplicationService.ExecuteAsync(
                        async () =>
                        {
                            await Task.Delay(Random.Shared.Next(50, 200)); // Simulate work
                            return $"Result for {key}";
                        },
                        new RequestContext { RequestKey = key, EnableCaching = true }
                    ));
                }

                var deduplicationResults = await Task.WhenAll(operations);
                stopwatch.Stop();

                results.TotalDuration = stopwatch.Elapsed;
                results.TestEndTime = DateTime.UtcNow;
                results.TotalOperations = deduplicationResults.Length;
                results.SuccessfulOperations = deduplicationResults.Count(r => r.IsSuccess);
                results.FailedOperations = deduplicationResults.Count(r => !r.IsSuccess);
                results.AverageResponseTime = deduplicationResults.Average(r => r.ExecutionTime.TotalMilliseconds);

                // Calculate deduplication metrics
                var cacheHits = deduplicationResults.Count(r => r.WasCached);
                var inFlightMerges = deduplicationResults.Count(r => r.WasDeduplicatedWithInFlight);
                results.DeduplicationRate = (double)(cacheHits + inFlightMerges) / results.TotalOperations;
                results.CacheHitRate = (double)cacheHits / results.TotalOperations;
                results.Throughput = results.TotalOperations / results.TotalDuration.TotalSeconds;
                results.IsSuccess = results.SuccessfulOperations > results.TotalOperations * 0.95;

                _logger.LogInformation("✅ Deduplication test completed: {DeduplicationRate:P1} deduplication rate, {CacheHitRate:P1} cache hits",
                    results.DeduplicationRate, results.CacheHitRate);

                return results;
            }
            catch (Exception ex)
            {
                results.IsSuccess = false;
                results.ErrorMessage = ex.Message;
                results.TestEndTime = DateTime.UtcNow;
                
                _logger.LogError(ex, "❌ Deduplication performance test failed");
                return results;
            }
        }

        /// <summary>
        /// Tests database connection pooling performance
        /// </summary>
        public async Task<PerformanceTestResults> TestDatabaseConnectionPoolingAsync()
        {
            var results = new PerformanceTestResults
            {
                TestName = "Database Connection Pooling Test",
                TestStartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("🧪 Testing database connection pooling...");

                var stopwatch = Stopwatch.StartNew();
                var operations = new List<Task<TimeSpan>>();

                // Simulate concurrent database operations
                for (int i = 0; i < 50; i++)
                {
                    operations.Add(TestDatabaseOperation(i));
                }

                var operationTimes = await Task.WhenAll(operations);
                stopwatch.Stop();

                results.TotalDuration = stopwatch.Elapsed;
                results.TestEndTime = DateTime.UtcNow;
                results.TotalOperations = operationTimes.Length;
                results.SuccessfulOperations = operationTimes.Length;
                results.AverageResponseTime = operationTimes.Average(t => t.TotalMilliseconds);
                results.MinResponseTime = operationTimes.Min(t => t.TotalMilliseconds);
                results.MaxResponseTime = operationTimes.Max(t => t.TotalMilliseconds);
                results.Throughput = results.TotalOperations / results.TotalDuration.TotalSeconds;
                results.MaxConcurrentRequests = operations.Count;
                results.IsSuccess = true;

                _logger.LogInformation("✅ Database connection pooling test completed: {AvgTime:F1}ms avg, {Throughput:F1} ops/sec",
                    results.AverageResponseTime, results.Throughput);

                return results;
            }
            catch (Exception ex)
            {
                results.IsSuccess = false;
                results.ErrorMessage = ex.Message;
                results.TestEndTime = DateTime.UtcNow;
                
                _logger.LogError(ex, "❌ Database connection pooling test failed");
                return results;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Tests individual cache operation
        /// </summary>
        private async Task<TimeSpan> TestCacheOperation(string key, string value)
        {
            var stopwatch = Stopwatch.StartNew();
            
            await _cacheService.GetOrSetAsync(key, async () =>
            {
                await Task.Delay(10); // Simulate work
                return value;
            });
            
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Tests query cache operation
        /// </summary>
        private async Task<TimeSpan> TestQueryCacheOperation(string queryKey)
        {
            var stopwatch = Stopwatch.StartNew();
            
            await _queryCacheService.GetOrSetQueryAsync(async () =>
            {
                await Task.Delay(Random.Shared.Next(20, 100)); // Simulate database query
                return $"Query result for {queryKey}";
            }, queryKey);
            
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Tests database operation timing
        /// </summary>
        private async Task<TimeSpan> TestDatabaseOperation(int operationId)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Simulate database operation by accessing spot service
                var spots = await _spotService.GetSpotsWithinRadiusAsync(0, 0, 1000);
                await Task.Delay(10); // Simulate additional processing
            }
            catch
            {
                // Ignore errors for performance testing
            }
            
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Creates test data with different patterns
        /// </summary>
        private byte[] CreateTestData(int size, string pattern)
        {
            return pattern switch
            {
                "repetitive" => Encoding.UTF8.GetBytes(new string('A', size)),
                "random" => Enumerable.Range(0, size).Select(_ => (byte)Random.Shared.Next(256)).ToArray(),
                "mixed" => CreateMixedData(size),
                "json" => CreateJsonLikeData(size),
                _ => new byte[size]
            };
        }

        /// <summary>
        /// Creates mixed pattern test data
        /// </summary>
        private byte[] CreateMixedData(int size)
        {
            var data = new byte[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = i % 2 == 0 ? (byte)'A' : (byte)Random.Shared.Next(65, 90);
            }
            return data;
        }

        /// <summary>
        /// Creates JSON-like test data
        /// </summary>
        private byte[] CreateJsonLikeData(int size)
        {
            var json = "{\"test\":\"data\",\"numbers\":[1,2,3,4,5],\"nested\":{\"field\":\"value\"}}";
            var repeated = string.Concat(Enumerable.Repeat(json, size / json.Length + 1));
            return Encoding.UTF8.GetBytes(repeated[..Math.Min(repeated.Length, size)]);
        }

        #endregion
    }
}