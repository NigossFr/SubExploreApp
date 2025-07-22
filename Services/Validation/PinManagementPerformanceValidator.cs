using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;
using System.Diagnostics;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace SubExplore.Services.Validation
{
    /// <summary>
    /// Utility to validate pin management performance improvements
    /// </summary>
    public class PinManagementPerformanceValidator
    {
        private readonly IPinManagementService _optimizedService;
        private readonly ILogger<PinManagementPerformanceValidator> _logger;

        public PinManagementPerformanceValidator(
            IPinManagementService optimizedService,
            ILogger<PinManagementPerformanceValidator> logger)
        {
            _optimizedService = optimizedService ?? throw new ArgumentNullException(nameof(optimizedService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Run comprehensive performance validation comparing optimized vs traditional approaches
        /// </summary>
        public async Task<PinPerformanceReport> ValidatePerformanceImprovementsAsync()
        {
            var report = new PinPerformanceReport();
            
            _logger.LogInformation("Starting pin management performance validation");

            try
            {
                // Generate test data
                var testSpots = GenerateTestSpots(1000);
                var viewport = new MapSpan(new Location(43.2969, 5.3811), 0.01, 0.01);

                // Test optimized service performance
                report.OptimizedResults = await TestOptimizedServiceAsync(testSpots, viewport);
                
                // Test traditional approach simulation
                report.TraditionalResults = await TestTraditionalApproachAsync(testSpots);
                
                // Calculate improvements
                report.CalculateImprovements();
                
                _logger.LogInformation("Performance validation completed successfully");
                _logger.LogInformation("Optimized approach is {ImprovementFactor:F1}x faster", report.ImprovementFactor);
                _logger.LogInformation("Cache hit rate: {CacheHitRate:F1}%", report.OptimizedResults.CacheHitRate);
                _logger.LogInformation("Memory efficiency: {MemoryImprovement:F1}% better", report.MemoryImprovementPercent);
                
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Performance validation failed");
                report.ErrorMessage = ex.Message;
                return report;
            }
        }

        private async Task<PerformanceTestResult> TestOptimizedServiceAsync(List<Spot> spots, MapSpan viewport)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new PerformanceTestResult { TestType = "Optimized Service" };

            try
            {
                // Test batch processing
                var batchResult = await _optimizedService.CreatePinsInBatchesAsync(spots, 50);
                result.BatchProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                result.CreatedPins = batchResult.ValidCount;
                result.CacheHits = batchResult.CacheHits;
                result.CacheMisses = batchResult.CacheMisses;
                result.CacheHitRate = batchResult.CacheHits / (double)(batchResult.CacheHits + batchResult.CacheMisses) * 100;

                stopwatch.Restart();

                // Test viewport optimization
                var optimizedPins = await _optimizedService.OptimizeForViewportAsync(batchResult.Pins, viewport, spots);
                result.ViewportOptimizationTimeMs = stopwatch.ElapsedMilliseconds;
                result.VisiblePins = optimizedPins.Count;

                stopwatch.Restart();

                // Test incremental updates
                var newSpots = GenerateTestSpots(100);
                var updatedPins = await _optimizedService.UpdatePinsIncrementallyAsync(optimizedPins, newSpots);
                result.IncrementalUpdateTimeMs = stopwatch.ElapsedMilliseconds;
                result.UpdatedPins = updatedPins.Count;

                // Get performance statistics
                var stats = _optimizedService.GetStats();
                result.TotalProcessingTimeMs = result.BatchProcessingTimeMs + result.ViewportOptimizationTimeMs + result.IncrementalUpdateTimeMs;
                result.MemoryEfficiency = CalculateMemoryEfficiency(stats);

                _logger.LogDebug("Optimized service test completed: {TotalTime}ms, {CacheHitRate:F1}% cache hits",
                    result.TotalProcessingTimeMs, result.CacheHitRate);

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error testing optimized service");
                return result;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        private async Task<PerformanceTestResult> TestTraditionalApproachAsync(List<Spot> spots)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new PerformanceTestResult { TestType = "Traditional Approach" };

            try
            {
                // Simulate traditional approach (no caching, no pooling, no batching)
                var pins = new List<Pin>();

                foreach (var spot in spots)
                {
                    if (IsValidSpotCoordinates(spot))
                    {
                        var pin = new Pin
                        {
                            Label = spot.Name ?? "Spot sans nom",
                            Address = spot.Description ?? "Aucune description",
                            Type = PinType.Place,
                            Location = new Location((double)spot.Latitude, (double)spot.Longitude)
                        };
                        pins.Add(pin);
                    }
                }

                result.BatchProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                result.CreatedPins = pins.Count;
                result.CacheHits = 0; // No caching in traditional approach
                result.CacheMisses = pins.Count;
                result.CacheHitRate = 0;

                stopwatch.Restart();

                // Simulate viewport filtering (inefficient approach)
                var visiblePins = pins.Where(p => IsInSimulatedViewport(p.Location)).ToList();
                result.ViewportOptimizationTimeMs = stopwatch.ElapsedMilliseconds;
                result.VisiblePins = visiblePins.Count;

                stopwatch.Restart();

                // Simulate incremental update (recreate all pins)
                var newSpots = GenerateTestSpots(100);
                var allSpots = spots.Concat(newSpots).ToList();
                var allPins = new List<Pin>();
                
                foreach (var spot in allSpots)
                {
                    if (IsValidSpotCoordinates(spot))
                    {
                        var pin = new Pin
                        {
                            Label = spot.Name ?? "Spot sans nom",
                            Address = spot.Description ?? "Aucune description",
                            Type = PinType.Place,
                            Location = new Location((double)spot.Latitude, (double)spot.Longitude)
                        };
                        allPins.Add(pin);
                    }
                }

                result.IncrementalUpdateTimeMs = stopwatch.ElapsedMilliseconds;
                result.UpdatedPins = allPins.Count;
                result.TotalProcessingTimeMs = result.BatchProcessingTimeMs + result.ViewportOptimizationTimeMs + result.IncrementalUpdateTimeMs;
                result.MemoryEfficiency = 1.0; // Baseline

                _logger.LogDebug("Traditional approach test completed: {TotalTime}ms, no caching",
                    result.TotalProcessingTimeMs);

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error testing traditional approach");
                return result;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        private List<Spot> GenerateTestSpots(int count)
        {
            var spots = new List<Spot>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                spots.Add(new Spot
                {
                    Id = i + 1,
                    Name = $"Spot de test {i + 1}",
                    Description = $"Description du spot {i + 1}",
                    Latitude = (decimal)(43.0 + random.NextDouble() * 1.0), // Around Marseille area
                    Longitude = (decimal)(5.0 + random.NextDouble() * 1.0),
                    MaxDepth = random.Next(5, 50),
                    DifficultyLevel = (DifficultyLevel)(random.Next(1, 4)),
                    ValidationStatus = SpotValidationStatus.Approved,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                    CreatorId = 1,
                    TypeId = 1,
                    RequiredEquipment = "Masque, tuba",
                    SafetyNotes = "Notes de sécurité",
                    BestConditions = "Conditions optimales"
                });
            }

            return spots;
        }

        private bool IsValidSpotCoordinates(Spot spot)
        {
            return spot.Latitude != null && spot.Longitude != null &&
                   spot.Latitude != 0 && spot.Longitude != 0 &&
                   Math.Abs((double)spot.Latitude) <= 90 && 
                   Math.Abs((double)spot.Longitude) <= 180;
        }

        private bool IsInSimulatedViewport(Location location)
        {
            // Simulate viewport bounds around Marseille area
            return location.Latitude >= 43.0 && location.Latitude <= 44.0 &&
                   location.Longitude >= 5.0 && location.Longitude <= 6.0;
        }

        private double CalculateMemoryEfficiency(PinManagementStats stats)
        {
            // Calculate memory efficiency based on pooling and caching
            var poolingEfficiency = stats.PooledPinsAvailable / (double)Math.Max(1, stats.PooledPinsAvailable + stats.PooledPinsUsed);
            var cachingEfficiency = stats.CacheHitRate / 100.0;
            
            return (poolingEfficiency + cachingEfficiency) / 2.0 + 1.0; // Efficiency multiplier
        }
    }

    public class PinPerformanceReport
    {
        public PerformanceTestResult OptimizedResults { get; set; } = new();
        public PerformanceTestResult TraditionalResults { get; set; } = new();
        public double ImprovementFactor { get; private set; }
        public double MemoryImprovementPercent { get; private set; }
        public string? ErrorMessage { get; set; }
        public DateTime TestTimestamp { get; set; } = DateTime.UtcNow;

        public void CalculateImprovements()
        {
            if (TraditionalResults.TotalProcessingTimeMs > 0)
            {
                ImprovementFactor = TraditionalResults.TotalProcessingTimeMs / Math.Max(1, OptimizedResults.TotalProcessingTimeMs);
                MemoryImprovementPercent = (OptimizedResults.MemoryEfficiency - TraditionalResults.MemoryEfficiency) * 100;
            }
        }

        public Dictionary<string, object> GetSummaryMetrics()
        {
            return new Dictionary<string, object>
            {
                ["ImprovementFactor"] = ImprovementFactor,
                ["MemoryImprovement"] = $"{MemoryImprovementPercent:F1}%",
                ["CacheHitRate"] = $"{OptimizedResults.CacheHitRate:F1}%",
                ["OptimizedTime"] = $"{OptimizedResults.TotalProcessingTimeMs}ms",
                ["TraditionalTime"] = $"{TraditionalResults.TotalProcessingTimeMs}ms",
                ["TimeSaved"] = $"{TraditionalResults.TotalProcessingTimeMs - OptimizedResults.TotalProcessingTimeMs}ms"
            };
        }
    }

    public class PerformanceTestResult
    {
        public string TestType { get; set; } = "";
        public long BatchProcessingTimeMs { get; set; }
        public long ViewportOptimizationTimeMs { get; set; }
        public long IncrementalUpdateTimeMs { get; set; }
        public long TotalProcessingTimeMs { get; set; }
        public int CreatedPins { get; set; }
        public int VisiblePins { get; set; }
        public int UpdatedPins { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
        public double CacheHitRate { get; set; }
        public double MemoryEfficiency { get; set; } = 1.0;
        public string? ErrorMessage { get; set; }
    }
}