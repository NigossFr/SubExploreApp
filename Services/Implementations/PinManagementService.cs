using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// High-performance pin management service with caching, pooling, and viewport optimization
    /// </summary>
    public class PinManagementService : IPinManagementService
    {
        private readonly ILogger<PinManagementService> _logger;
        private readonly IApplicationPerformanceService _performanceService;
        private readonly PinManagementConfig _config;

        // Pin caching and pooling
        private readonly ConcurrentDictionary<string, Pin> _pinCache = new();
        private readonly ConcurrentBag<Pin> _pinPool = new();
        private readonly PinManagementStats _stats = new();

        // Debouncing
        private readonly Timer _debounceTimer;
        private TaskCompletionSource<ObservableCollection<Pin>>? _pendingUpdate;
        private IEnumerable<Spot>? _pendingSpots;
        private readonly object _debounceLock = new();

        public PinManagementService(
            ILogger<PinManagementService> logger,
            IApplicationPerformanceService performanceService,
            PinManagementConfig? config = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
            _config = config ?? new PinManagementConfig();

            _debounceTimer = new Timer(ProcessDebouncedUpdate, null, Timeout.Infinite, Timeout.Infinite);
            
            _logger.LogInformation("PinManagementService initialized with config: Cache={MaxCache}, Pool={MaxPool}, Viewport={EnableViewport}",
                _config.MaxCacheSize, _config.MaxPoolSize, _config.EnableViewportOptimization);
        }

        public async Task<ObservableCollection<Pin>> GetOptimizedPinsAsync(IEnumerable<Spot> spots, MapSpan? viewport = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var spotsList = spots?.ToList() ?? new List<Spot>();

            try
            {
                _logger.LogDebug("GetOptimizedPinsAsync: Processing {SpotCount} spots with viewport optimization", spotsList.Count);

                var result = await CreatePinsInBatchesAsync(spotsList, _config.BatchSize);
                
                if (_config.EnableViewportOptimization && viewport != null)
                {
                    result.Pins = await OptimizeForViewportAsync(result.Pins, viewport, spotsList);
                    result.ProcessingStrategy = "Viewport-Optimized";
                }

                stopwatch.Stop();
                _performanceService?.TrackUserAction("PinOptimization", "GetOptimizedPins", stopwatch.Elapsed.TotalMilliseconds);

                _logger.LogInformation("Pin optimization completed: {ValidPins} valid pins from {TotalSpots} spots in {TimeMs:F1}ms",
                    result.ValidCount, spotsList.Count, stopwatch.Elapsed.TotalMilliseconds);

                return result.Pins;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOptimizedPinsAsync with {SpotCount} spots", spotsList.Count);
                return new ObservableCollection<Pin>();
            }
        }

        public async Task<PinCreationResult> CreatePinsInBatchesAsync(IEnumerable<Spot> spots, int batchSize = 50, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new PinCreationResult
            {
                ProcessingStrategy = "Batched",
                Pins = new ObservableCollection<Pin>()
            };

            try
            {
                var spotsList = spots?.ToList() ?? new List<Spot>();
                result.ProcessedCount = spotsList.Count;

                if (!spotsList.Any())
                {
                    return result;
                }

                // Process spots in parallel batches
                var batchTasks = new List<Task<BatchResult>>();
                var batches = spotsList
                    .Select((spot, index) => new { Spot = spot, Index = index })
                    .GroupBy(x => x.Index / batchSize)
                    .Select(g => g.Select(x => x.Spot).ToList());

                var semaphore = new SemaphoreSlim(_config.MaxConcurrentBatches);

                foreach (var batch in batches)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var batchTask = ProcessBatchAsync(batch, semaphore, cancellationToken);
                    batchTasks.Add(batchTask);
                }

                var batchResults = await Task.WhenAll(batchTasks);

                // Combine results
                var allPins = new List<Pin>();
                foreach (var batchResult in batchResults)
                {
                    allPins.AddRange(batchResult.Pins);
                    result.ValidCount += batchResult.ValidCount;
                    result.InvalidCount += batchResult.InvalidCount;
                    result.CacheHits += batchResult.CacheHits;
                    result.CacheMisses += batchResult.CacheMisses;
                }

                result.Pins = new ObservableCollection<Pin>(allPins);
                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds;

                // Update statistics
                _stats.TotalPinsCreated += result.ValidCount;
                _stats.CacheHitCount += result.CacheHits;
                _stats.CacheMissCount += result.CacheMisses;
                _stats.CacheHitRate = _stats.CacheHitCount / (double)(_stats.CacheHitCount + _stats.CacheMissCount) * 100;
                _stats.AverageCreationTimeMs = (_stats.AverageCreationTimeMs + result.ProcessingTimeMs) / 2;

                _logger.LogInformation("Batch processing completed: {ValidPins}/{TotalSpots} pins in {TimeMs:F1}ms, Cache hits: {CacheHits}",
                    result.ValidCount, result.ProcessedCount, result.ProcessingTimeMs, result.CacheHits);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreatePinsInBatchesAsync");
                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                return result;
            }
        }

        public async Task<ObservableCollection<Pin>> UpdatePinsIncrementallyAsync(ObservableCollection<Pin> currentPins, IEnumerable<Spot> newSpots)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var newSpotsList = newSpots?.ToList() ?? new List<Spot>();
                var currentPinDict = currentPins.ToDictionary(p => p.Label ?? "", p => p);
                var updatedPins = new List<Pin>(currentPins);

                var added = 0;
                var updated = 0;
                var removed = 0;

                // Add or update pins for new spots
                foreach (var spot in newSpotsList)
                {
                    var key = spot.Name ?? "";
                    var pin = await CreateOrReusePinAsync(spot);
                    
                    if (pin != null)
                    {
                        if (currentPinDict.ContainsKey(key))
                        {
                            // Update existing pin
                            var index = updatedPins.FindIndex(p => p.Label == key);
                            if (index >= 0)
                            {
                                updatedPins[index] = pin;
                                updated++;
                            }
                        }
                        else
                        {
                            // Add new pin
                            updatedPins.Add(pin);
                            added++;
                        }
                    }
                }

                // Remove pins that are no longer needed
                var newSpotNames = new HashSet<string>(newSpotsList.Select(s => s.Name ?? ""));
                var pinsToRemove = updatedPins.Where(p => !newSpotNames.Contains(p.Label ?? "")).ToList();
                
                foreach (var pin in pinsToRemove)
                {
                    updatedPins.Remove(pin);
                    ReturnPinToPool(pin);
                    removed++;
                }

                stopwatch.Stop();
                _performanceService?.TrackUserAction("PinIncremental", $"Added:{added},Updated:{updated},Removed:{removed}", 
                    stopwatch.Elapsed.TotalMilliseconds);

                _logger.LogDebug("Incremental update completed in {TimeMs:F1}ms: +{Added} ~{Updated} -{Removed}",
                    stopwatch.Elapsed.TotalMilliseconds, added, updated, removed);

                return new ObservableCollection<Pin>(updatedPins);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdatePinsIncrementallyAsync");
                return currentPins;
            }
        }

        public async Task<ObservableCollection<Pin>> OptimizeForViewportAsync(ObservableCollection<Pin> currentPins, MapSpan viewport, IEnumerable<Spot> allSpots)
        {
            if (!_config.EnableViewportOptimization)
                return currentPins;

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var visiblePins = new List<Pin>();
                var viewportBounds = CalculateViewportBounds(viewport);

                // Filter pins within viewport
                foreach (var pin in currentPins)
                {
                    if (IsLocationInViewport(pin.Location, viewportBounds))
                    {
                        visiblePins.Add(pin);
                    }
                    else
                    {
                        // Return invisible pins to pool
                        ReturnPinToPool(pin);
                    }
                }

                // Add pins for spots that became visible
                var visibleSpots = allSpots.Where(s => IsSpotInViewport(s, viewportBounds));
                var existingPinLabels = new HashSet<string>(visiblePins.Select(p => p.Label ?? ""));

                foreach (var spot in visibleSpots)
                {
                    if (!existingPinLabels.Contains(spot.Name ?? ""))
                    {
                        var pin = await CreateOrReusePinAsync(spot);
                        if (pin != null)
                        {
                            visiblePins.Add(pin);
                        }
                    }
                }

                stopwatch.Stop();
                _stats.ViewportOptimizationCount++;
                
                _performanceService?.TrackMapRender(stopwatch.Elapsed.TotalMilliseconds, visiblePins.Count, 
                    Math.Log10(viewport.LatitudeDegrees + viewport.LongitudeDegrees));

                _logger.LogDebug("Viewport optimization: {VisiblePins} pins in viewport from {TotalPins} total in {TimeMs:F1}ms",
                    visiblePins.Count, currentPins.Count, stopwatch.Elapsed.TotalMilliseconds);

                return new ObservableCollection<Pin>(visiblePins);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OptimizeForViewportAsync");
                return currentPins;
            }
        }

        public void SetDebouncing(bool enabled, int debounceDelayMs = 300)
        {
            _config.DebounceDelayMs = debounceDelayMs;
            _logger.LogInformation("Debouncing {Status} with {DelayMs}ms delay", 
                enabled ? "enabled" : "disabled", debounceDelayMs);
        }

        public void ClearCache()
        {
            _pinCache.Clear();
            
            // Return all pooled pins
            while (_pinPool.TryTake(out var pin))
            {
                // Pin returned to system pool
            }

            _stats.CacheHitCount = 0;
            _stats.CacheMissCount = 0;
            _stats.TotalPinsCreated = 0;
            
            _logger.LogInformation("Pin cache and pool cleared");
        }

        public PinManagementStats GetStats()
        {
            _stats.LastUpdated = DateTime.UtcNow;
            _stats.PooledPinsAvailable = _pinPool.Count;
            _stats.PooledPinsUsed = _config.MaxPoolSize - _pinPool.Count;
            
            return _stats;
        }

        private async Task<BatchResult> ProcessBatchAsync(List<Spot> batch, SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            
            try
            {
                var result = new BatchResult();
                
                await Task.Run(() =>
                {
                    foreach (var spot in batch)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var pin = CreateOrReusePinSync(spot);
                        if (pin != null)
                        {
                            result.Pins.Add(pin);
                            result.ValidCount++;
                            
                            if (_pinCache.ContainsKey(GetSpotCacheKey(spot)))
                                result.CacheHits++;
                            else
                                result.CacheMisses++;
                        }
                        else
                        {
                            result.InvalidCount++;
                        }
                    }
                }, cancellationToken);

                return result;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<Pin?> CreateOrReusePinAsync(Spot spot)
        {
            return await Task.Run(() => CreateOrReusePinSync(spot));
        }

        private Pin? CreateOrReusePinSync(Spot spot)
        {
            if (spot == null || !IsValidSpotCoordinates(spot))
                return null;

            // Check cache first
            var cacheKey = GetSpotCacheKey(spot);
            if (_config.EnablePinCaching && _pinCache.TryGetValue(cacheKey, out var cachedPin))
            {
                return cachedPin;
            }

            // Try to reuse from pool
            Pin? pin = null;
            if (_config.EnablePinPooling && _pinPool.TryTake(out pin))
            {
                UpdatePinFromSpot(pin, spot);
            }
            else
            {
                pin = CreateNewPin(spot);
            }

            // Cache the pin
            if (pin != null && _config.EnablePinCaching && _pinCache.Count < _config.MaxCacheSize)
            {
                _pinCache.TryAdd(cacheKey, pin);
            }

            return pin;
        }

        private Pin CreateNewPin(Spot spot)
        {
            return new Pin
            {
                Label = spot.Name ?? "Spot sans nom",
                Address = spot.Description ?? "Aucune description",
                Type = PinType.Place,
                Location = new Location((double)spot.Latitude, (double)spot.Longitude)
            };
        }

        private void UpdatePinFromSpot(Pin pin, Spot spot)
        {
            pin.Label = spot.Name ?? "Spot sans nom";
            pin.Address = spot.Description ?? "Aucune description";
            pin.Type = PinType.Place;
            pin.Location = new Location((double)spot.Latitude, (double)spot.Longitude);
        }

        private void ReturnPinToPool(Pin pin)
        {
            if (_config.EnablePinPooling && _pinPool.Count < _config.MaxPoolSize)
            {
                _pinPool.Add(pin);
            }
        }

        private bool IsValidSpotCoordinates(Spot spot)
        {
            return spot.Latitude != null && spot.Longitude != null &&
                   spot.Latitude != 0 && spot.Longitude != 0 &&
                   Math.Abs((double)spot.Latitude) <= 90 && 
                   Math.Abs((double)spot.Longitude) <= 180;
        }

        private string GetSpotCacheKey(Spot spot)
        {
            return $"{spot.Id}_{spot.Name}_{spot.Latitude}_{spot.Longitude}";
        }

        private ViewportBounds CalculateViewportBounds(MapSpan viewport)
        {
            return new ViewportBounds
            {
                North = viewport.Center.Latitude + viewport.LatitudeDegrees / 2,
                South = viewport.Center.Latitude - viewport.LatitudeDegrees / 2,
                East = viewport.Center.Longitude + viewport.LongitudeDegrees / 2,
                West = viewport.Center.Longitude - viewport.LongitudeDegrees / 2
            };
        }

        private bool IsLocationInViewport(Location location, ViewportBounds bounds)
        {
            return location.Latitude >= bounds.South && location.Latitude <= bounds.North &&
                   location.Longitude >= bounds.West && location.Longitude <= bounds.East;
        }

        private bool IsSpotInViewport(Spot spot, ViewportBounds bounds)
        {
            var lat = (double)spot.Latitude;
            var lon = (double)spot.Longitude;
            return lat >= bounds.South && lat <= bounds.North &&
                   lon >= bounds.West && lon <= bounds.East;
        }

        private void ProcessDebouncedUpdate(object? state)
        {
            lock (_debounceLock)
            {
                if (_pendingUpdate != null && _pendingSpots != null)
                {
                    var result = GetOptimizedPinsAsync(_pendingSpots).GetAwaiter().GetResult();
                    _pendingUpdate.SetResult(result);
                    
                    _stats.DebounceOperationsSaved++;
                    _pendingUpdate = null;
                    _pendingSpots = null;
                }
            }
        }

        public void Dispose()
        {
            _debounceTimer?.Dispose();
            ClearCache();
        }
    }

    internal class BatchResult
    {
        public List<Pin> Pins { get; set; } = new();
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
    }

    internal class ViewportBounds
    {
        public double North { get; set; }
        public double South { get; set; }
        public double East { get; set; }
        public double West { get; set; }
    }
}