using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations.PinSelection;
using System.Diagnostics;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Intelligent pin selection service with automatic strategy selection and performance monitoring
    /// Coordinates different pin selection strategies for optimal user experience
    /// </summary>
    public class PinSelectionService : IPinSelectionService
    {
        private readonly ILogger<PinSelectionService> _logger;
        private readonly List<IPinSelectionStrategy> _strategies;
        private readonly PinSelectionMetrics _metrics;
        private readonly object _metricsLock = new();

        public PinSelectionService(ILogger<PinSelectionService> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _metrics = new PinSelectionMetrics();
            
            // Initialize available strategies
            _strategies = new List<IPinSelectionStrategy>
            {
                new DistanceBasedPinSelectionStrategy(loggerFactory.CreateLogger<DistanceBasedPinSelectionStrategy>()),
                new SpatialIndexPinSelectionStrategy(loggerFactory.CreateLogger<SpatialIndexPinSelectionStrategy>()),
                new NativePinSelectionStrategy(loggerFactory.CreateLogger<NativePinSelectionStrategy>())
            };

            _logger.LogInformation("PinSelectionService initialized with {StrategyCount} strategies", 
                _strategies.Count);
        }

        public async Task<Spot?> SelectPinAsync(Location clickLocation, IEnumerable<Pin> visiblePins, 
            MapSpan? mapViewport = null, int pinCount = 0)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                if (clickLocation == null)
                    return null;

                var pinList = visiblePins.ToList();
                var actualPinCount = pinCount > 0 ? pinCount : pinList.Count;

                // Select optimal strategy based on context
                var strategy = GetOptimalStrategy(actualPinCount, mapViewport, DeviceInfo.Current.Platform);
                
                _logger.LogDebug("Using {StrategyName} for {PinCount} pins", 
                    strategy.StrategyName, actualPinCount);

                // Perform selection
                var result = await SelectPinWithStrategyAsync(strategy, clickLocation, pinList, mapViewport);

                stopwatch.Stop();
                RecordMetrics(strategy.StrategyName, stopwatch.ElapsedMilliseconds, result != null);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in automatic pin selection");
                stopwatch.Stop();
                RecordMetrics("Error", stopwatch.ElapsedMilliseconds, false);
                return null;
            }
        }

        public async Task<Spot?> SelectPinWithStrategyAsync(IPinSelectionStrategy strategy, Location clickLocation, 
            IEnumerable<Pin> visiblePins, MapSpan? mapViewport = null)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogDebug("Executing pin selection with {StrategyName}", strategy.StrategyName);
                
                var result = await strategy.SelectPinAsync(clickLocation, visiblePins, mapViewport);
                
                stopwatch.Stop();
                RecordMetrics(strategy.StrategyName, stopwatch.ElapsedMilliseconds, result != null);
                
                if (result != null)
                {
                    _logger.LogDebug("Successfully selected spot: {SpotName} using {StrategyName} in {ElapsedMs}ms", 
                        result.Name, strategy.StrategyName, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogDebug("No spot selected using {StrategyName} in {ElapsedMs}ms", 
                        strategy.StrategyName, stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing {StrategyName}", strategy.StrategyName);
                stopwatch.Stop();
                RecordMetrics(strategy.StrategyName, stopwatch.ElapsedMilliseconds, false);
                return null;
            }
        }

        public IPinSelectionStrategy GetOptimalStrategy(int pinCount, MapSpan? mapViewport = null, 
            DevicePlatform? platform = null)
        {
            try
            {
                var currentPlatform = platform ?? DeviceInfo.Current.Platform;
                var context = CreateSelectionContext(pinCount, mapViewport);

                // Find the best strategy for current context
                var applicableStrategies = _strategies
                    .Where(s => s.IsApplicable(currentPlatform, context))
                    .ToList();

                if (!applicableStrategies.Any())
                {
                    // Fallback to distance-based if no strategies are applicable
                    _logger.LogWarning("No applicable strategies found, falling back to distance-based");
                    return _strategies.OfType<DistanceBasedPinSelectionStrategy>().First();
                }

                // Strategy selection logic based on context
                IPinSelectionStrategy selectedStrategy;

                if (context.IsHighDensityArea && context.TotalPinCount > 100)
                {
                    // High density areas benefit from spatial indexing
                    selectedStrategy = applicableStrategies.OfType<SpatialIndexPinSelectionStrategy>().FirstOrDefault()
                                    ?? applicableStrategies.First();
                }
                else if (context.RequiredPrecision == SelectionPrecision.Maximum)
                {
                    // Maximum precision requires native hit testing when available
                    selectedStrategy = applicableStrategies.OfType<NativePinSelectionStrategy>().FirstOrDefault()
                                    ?? applicableStrategies.First();
                }
                else
                {
                    // Standard contexts use distance-based selection
                    selectedStrategy = applicableStrategies.OfType<DistanceBasedPinSelectionStrategy>().FirstOrDefault()
                                    ?? applicableStrategies.First();
                }

                _logger.LogDebug("Selected {StrategyName} for context: pins={PinCount}, density={IsHighDensity}, precision={Precision}", 
                    selectedStrategy.StrategyName, context.TotalPinCount, context.IsHighDensityArea, context.RequiredPrecision);

                return selectedStrategy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting optimal strategy, falling back to default");
                return _strategies.OfType<DistanceBasedPinSelectionStrategy>().First();
            }
        }

        public IEnumerable<IPinSelectionStrategy> GetAvailableStrategies()
        {
            return _strategies.AsReadOnly();
        }

        public PinSelectionMetrics GetPerformanceMetrics()
        {
            lock (_metricsLock)
            {
                // Create a deep copy to avoid concurrency issues
                return new PinSelectionMetrics
                {
                    StrategyUsageCount = new Dictionary<string, int>(_metrics.StrategyUsageCount),
                    AverageSelectionTimeMs = new Dictionary<string, double>(_metrics.AverageSelectionTimeMs),
                    SuccessRate = new Dictionary<string, double>(_metrics.SuccessRate),
                    TotalSelections = _metrics.TotalSelections,
                    LastResetTime = _metrics.LastResetTime
                };
            }
        }

        /// <summary>
        /// Create selection context based on current conditions
        /// </summary>
        private static PinSelectionContext CreateSelectionContext(int pinCount, MapSpan? mapViewport)
        {
            var context = new PinSelectionContext
            {
                TotalPinCount = pinCount
            };

            if (mapViewport != null)
            {
                // Calculate zoom level approximation
                var avgSpan = (mapViewport.LatitudeDegrees + mapViewport.LongitudeDegrees) / 2;
                context.MapZoomLevel = avgSpan switch
                {
                    > 1.0 => 8,   // Very zoomed out
                    > 0.5 => 10,  // Moderately zoomed out
                    > 0.1 => 12,  // Medium zoom
                    > 0.05 => 14, // Good zoom
                    _ => 16       // Very zoomed in
                };

                // Determine if this is a high density area
                // Simple heuristic: more than 1 pin per square km of visible area
                var visibleAreaKm2 = mapViewport.LatitudeDegrees * mapViewport.LongitudeDegrees * 111 * 111; // Rough conversion
                context.IsHighDensityArea = visibleAreaKm2 > 0 && (pinCount / visibleAreaKm2) > 1.0;

                // Set precision requirements based on zoom
                context.RequiredPrecision = context.MapZoomLevel switch
                {
                    >= 15 => SelectionPrecision.Maximum,
                    >= 13 => SelectionPrecision.High,
                    >= 10 => SelectionPrecision.Standard,
                    _ => SelectionPrecision.Coarse
                };
            }

            return context;
        }

        /// <summary>
        /// Record performance metrics for strategy analysis
        /// </summary>
        private void RecordMetrics(string strategyName, long elapsedMs, bool success)
        {
            lock (_metricsLock)
            {
                _metrics.TotalSelections++;

                // Update usage count
                _metrics.StrategyUsageCount.TryGetValue(strategyName, out var usageCount);
                _metrics.StrategyUsageCount[strategyName] = usageCount + 1;

                // Update average time (simple moving average)
                _metrics.AverageSelectionTimeMs.TryGetValue(strategyName, out var avgTime);
                var newCount = _metrics.StrategyUsageCount[strategyName];
                _metrics.AverageSelectionTimeMs[strategyName] = ((avgTime * (newCount - 1)) + elapsedMs) / newCount;

                // Update success rate
                _metrics.SuccessRate.TryGetValue(strategyName, out var successRate);
                var previousSuccesses = successRate * (newCount - 1);
                var newSuccesses = previousSuccesses + (success ? 1 : 0);
                _metrics.SuccessRate[strategyName] = newSuccesses / newCount;
            }
        }
    }
}