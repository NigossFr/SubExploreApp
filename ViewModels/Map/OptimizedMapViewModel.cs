using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;
using SubExplore.ViewModels.Base;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SubExplore.ViewModels.Map
{
    /// <summary>
    /// Optimized MapViewModel with advanced pin management, viewport optimization, and performance monitoring
    /// </summary>
    public partial class OptimizedMapViewModel : ViewModelBase
    {
        private readonly IPinManagementService _pinManagementService;
        private readonly IApplicationPerformanceService _performanceService;
        private readonly ILogger<OptimizedMapViewModel> _logger;
        
        // Debouncing and throttling
        private readonly Timer _updateThrottleTimer;
        private readonly Timer _viewportUpdateTimer;
        private volatile bool _isUpdatePending = false;
        private volatile bool _isViewportUpdatePending = false;
        private IEnumerable<Spot>? _pendingSpots;
        private MapSpan? _pendingViewport;
        
        // Performance tracking
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private readonly ConcurrentDictionary<string, DateTime> _operationTimestamps = new();

        [ObservableProperty]
        private ObservableCollection<Spot> _spots = new();

        [ObservableProperty]
        private ObservableCollection<Pin> _pins = new();

        [ObservableProperty]
        private MapSpan? _visibleRegion;

        [ObservableProperty]
        private bool _isOptimizingPins;

        [ObservableProperty]
        private string _performanceStatus = "Ready";

        // Pin management statistics
        [ObservableProperty]
        private int _totalPinsCreated;

        [ObservableProperty]
        private double _averageUpdateTimeMs;

        [ObservableProperty]
        private double _cacheHitRate;

        public OptimizedMapViewModel(
            IPinManagementService pinManagementService,
            IApplicationPerformanceService performanceService,
            IDialogService dialogService,
            INavigationService navigationService,
            ILogger<OptimizedMapViewModel> logger)
            : base(dialogService, navigationService)
        {
            _pinManagementService = pinManagementService ?? throw new ArgumentNullException(nameof(pinManagementService));
            _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Title = "Carte Optimisée";

            // Initialize throttling timers
            _updateThrottleTimer = new Timer(ProcessThrottledUpdate, null, Timeout.Infinite, Timeout.Infinite);
            _viewportUpdateTimer = new Timer(ProcessViewportUpdate, null, Timeout.Infinite, Timeout.Infinite);

            // Enable debouncing by default
            _pinManagementService.SetDebouncing(true, 300);

            _logger.LogInformation("OptimizedMapViewModel initialized with advanced pin management");
        }

        /// <summary>
        /// Update spots with intelligent throttling and optimization
        /// </summary>
        [RelayCommand]
        private async Task UpdateSpotsOptimizedAsync(IEnumerable<Spot>? spots = null)
        {
            try
            {
                var spotsToUpdate = spots ?? Spots;
                
                // Prevent rapid consecutive updates
                if (IsRecentOperation("UpdateSpots", TimeSpan.FromMilliseconds(500)))
                {
                    _logger.LogDebug("UpdateSpotsOptimized throttled - too recent");
                    return;
                }

                RecordOperation("UpdateSpots");
                
                IsOptimizingPins = true;
                PerformanceStatus = "Optimizing pins...";

                var startTime = DateTime.UtcNow;
                
                // Use optimized pin creation with viewport awareness
                var optimizedPins = await _pinManagementService.GetOptimizedPinsAsync(spotsToUpdate, VisibleRegion);
                
                // Update collections atomically
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Spots = new ObservableCollection<Spot>(spotsToUpdate);
                    Pins = optimizedPins;
                });

                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _performanceService?.TrackMapRender(processingTime, Pins.Count, GetCurrentZoomLevel());

                // Update performance metrics
                UpdatePerformanceMetrics();
                
                PerformanceStatus = $"Updated {Pins.Count} pins in {processingTime:F0}ms";
                
                _logger.LogInformation("Optimized spot update completed: {PinCount} pins in {TimeMs:F1}ms", 
                    Pins.Count, processingTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateSpotsOptimizedAsync");
                PerformanceStatus = $"Update failed: {ex.Message}";
            }
            finally
            {
                IsOptimizingPins = false;
            }
        }

        /// <summary>
        /// Update spots incrementally for better performance
        /// </summary>
        [RelayCommand]
        private async Task UpdateSpotsIncrementalAsync(IEnumerable<Spot> newSpots)
        {
            try
            {
                if (IsRecentOperation("IncrementalUpdate", TimeSpan.FromMilliseconds(200)))
                {
                    // Queue for throttled update
                    QueueThrottledUpdate(newSpots);
                    return;
                }

                RecordOperation("IncrementalUpdate");
                
                IsOptimizingPins = true;
                PerformanceStatus = "Incremental update...";

                var startTime = DateTime.UtcNow;
                
                var updatedPins = await _pinManagementService.UpdatePinsIncrementallyAsync(Pins, newSpots);
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Update spots collection
                    var updatedSpots = new HashSet<string>(newSpots.Select(s => s.Name ?? ""));
                    var combinedSpots = Spots.Where(s => !updatedSpots.Contains(s.Name ?? ""))
                                            .Concat(newSpots);
                    
                    Spots = new ObservableCollection<Spot>(combinedSpots);
                    Pins = updatedPins;
                });

                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                UpdatePerformanceMetrics();
                
                PerformanceStatus = $"Incremental: {Pins.Count} pins in {processingTime:F0}ms";
                
                _logger.LogDebug("Incremental update completed in {TimeMs:F1}ms", processingTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateSpotsIncrementalAsync");
                PerformanceStatus = $"Incremental update failed: {ex.Message}";
            }
            finally
            {
                IsOptimizingPins = false;
            }
        }

        /// <summary>
        /// Handle viewport changes with optimization
        /// </summary>
        [RelayCommand]
        private async Task OnViewportChangedAsync(MapSpan newViewport)
        {
            try
            {
                if (newViewport == null) return;

                VisibleRegion = newViewport;

                // Throttle viewport updates to prevent excessive processing
                if (IsRecentOperation("ViewportUpdate", TimeSpan.FromMilliseconds(500)))
                {
                    QueueViewportUpdate(newViewport);
                    return;
                }

                RecordOperation("ViewportUpdate");
                await OptimizeForCurrentViewportAsync(newViewport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnViewportChangedAsync");
            }
        }

        /// <summary>
        /// Optimize pins for current viewport
        /// </summary>
        private async Task OptimizeForCurrentViewportAsync(MapSpan viewport)
        {
            try
            {
                IsOptimizingPins = true;
                PerformanceStatus = "Viewport optimization...";

                var startTime = DateTime.UtcNow;
                
                var optimizedPins = await _pinManagementService.OptimizeForViewportAsync(Pins, viewport, Spots);
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Pins = optimizedPins;
                });

                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _performanceService?.TrackMapRender(processingTime, Pins.Count, GetZoomLevelFromViewport(viewport));

                PerformanceStatus = $"Viewport: {Pins.Count} visible pins in {processingTime:F0}ms";
                
                _logger.LogDebug("Viewport optimization completed: {VisiblePins} pins in {TimeMs:F1}ms", 
                    Pins.Count, processingTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OptimizeForCurrentViewportAsync");
                PerformanceStatus = "Viewport optimization failed";
            }
            finally
            {
                IsOptimizingPins = false;
            }
        }

        /// <summary>
        /// Clear all caches and reset optimization
        /// </summary>
        [RelayCommand]
        private async Task ClearCachesAsync()
        {
            try
            {
                _pinManagementService.ClearCache();
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Pins.Clear();
                });

                UpdatePerformanceMetrics();
                PerformanceStatus = "Caches cleared";
                
                _logger.LogInformation("Pin caches cleared and reset");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing caches");
                PerformanceStatus = "Cache clear failed";
            }
        }

        /// <summary>
        /// Get current performance statistics
        /// </summary>
        [RelayCommand]
        private void UpdatePerformanceMetrics()
        {
            try
            {
                var stats = _pinManagementService.GetStats();
                
                TotalPinsCreated = stats.TotalPinsCreated;
                AverageUpdateTimeMs = stats.AverageCreationTimeMs;
                CacheHitRate = stats.CacheHitRate;

                _logger.LogDebug("Performance metrics updated: {TotalPins} pins, {AvgTime:F1}ms avg, {CacheRate:F1}% cache hit",
                    TotalPinsCreated, AverageUpdateTimeMs, CacheHitRate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating performance metrics");
            }
        }

        private void QueueThrottledUpdate(IEnumerable<Spot> spots)
        {
            _pendingSpots = spots;
            
            if (!_isUpdatePending)
            {
                _isUpdatePending = true;
                _updateThrottleTimer.Change(300, Timeout.Infinite);
            }
        }

        private void QueueViewportUpdate(MapSpan viewport)
        {
            _pendingViewport = viewport;
            
            if (!_isViewportUpdatePending)
            {
                _isViewportUpdatePending = true;
                _viewportUpdateTimer.Change(500, Timeout.Infinite);
            }
        }

        private void ProcessThrottledUpdate(object? state)
        {
            if (_isUpdatePending && _pendingSpots != null)
            {
                _isUpdatePending = false;
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UpdateSpotsIncrementalAsync(_pendingSpots);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in throttled update");
                    }
                });
            }
        }

        private void ProcessViewportUpdate(object? state)
        {
            if (_isViewportUpdatePending && _pendingViewport != null)
            {
                _isViewportUpdatePending = false;
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await OptimizeForCurrentViewportAsync(_pendingViewport);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in viewport update");
                    }
                });
            }
        }

        private bool IsRecentOperation(string operationName, TimeSpan threshold)
        {
            var now = DateTime.UtcNow;
            
            if (_operationTimestamps.TryGetValue(operationName, out var lastTime))
            {
                return (now - lastTime) < threshold;
            }
            
            return false;
        }

        private void RecordOperation(string operationName)
        {
            _operationTimestamps[operationName] = DateTime.UtcNow;
        }

        private double GetCurrentZoomLevel()
        {
            return VisibleRegion != null ? GetZoomLevelFromViewport(VisibleRegion) : 1.0;
        }

        private static double GetZoomLevelFromViewport(MapSpan viewport)
        {
            // Approximate zoom level calculation
            var span = viewport.LatitudeDegrees + viewport.LongitudeDegrees;
            return Math.Max(1.0, Math.Min(18.0, 18.0 - Math.Log10(span * 100)));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateThrottleTimer?.Dispose();
                _viewportUpdateTimer?.Dispose();
                _pinManagementService?.ClearCache();
            }
            
            base.Dispose(disposing);
        }
    }
}