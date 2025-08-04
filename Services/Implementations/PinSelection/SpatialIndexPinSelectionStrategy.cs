using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using System.Collections.Concurrent;

namespace SubExplore.Services.Implementations.PinSelection
{
    /// <summary>
    /// High-performance pin selection strategy using adaptive spatial indexing
    /// Provides O(log n) selection performance with intelligent caching and memory optimization
    /// </summary>
    public class SpatialIndexPinSelectionStrategy : IPinSelectionStrategy
    {
        public string StrategyName => "Adaptive Spatial Index Selection";

        private readonly ILogger<SpatialIndexPinSelectionStrategy>? _logger;
        private readonly ConcurrentDictionary<SpatialCellKey, SpatialCell> _spatialIndex = new();
        private readonly ConcurrentDictionary<Location, SpatialCellKey> _locationCache = new();
        private readonly ReaderWriterLockSlim _indexLock = new();
        private HashSet<Pin>? _lastIndexedPins;
        private DateTime _lastIndexUpdate = DateTime.MinValue;
        
        // Adaptive grid sizing based on context
        private double _currentCellSizeKm = 1.0;
        private const double MIN_CELL_SIZE_KM = 0.1;
        private const double MAX_CELL_SIZE_KM = 5.0;
        private const int MAX_CACHE_SIZE = 1000;
        private const int INDEX_REFRESH_THRESHOLD_MS = 5000;

        public SpatialIndexPinSelectionStrategy(ILogger<SpatialIndexPinSelectionStrategy>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Dispose resources properly
        /// </summary>
        public void Dispose()
        {
            _indexLock?.Dispose();
        }

        public async Task<Spot?> SelectPinAsync(Location clickLocation, IEnumerable<Pin> visiblePins, MapSpan? mapViewport = null)
        {
            try
            {
                if (clickLocation == null)
                    return null;

                // Smart incremental index update
                await UpdateSpatialIndexIncrementalAsync(visiblePins, mapViewport);

                var tolerance = CalculateToleranceKm(mapViewport);
                var candidates = new List<SpatialCandidate>();

                // Get optimized search cells using circular algorithm
                var searchCells = GetOptimizedSearchCells(clickLocation, tolerance);

                _logger?.LogDebug("Searching {CellCount} spatial cells for pins near {Lat:F6}, {Lon:F6}", 
                    searchCells.Count, clickLocation.Latitude, clickLocation.Longitude);

                // Parallel search with early termination and distance pre-filtering
                var searchTasks = searchCells.Select(async cellKey =>
                {
                    if (_spatialIndex.TryGetValue(cellKey, out var cell))
                    {
                        var cellCandidates = new List<SpatialCandidate>();
                        foreach (var indexedPin in cell.Pins)
                        {
                            var distance = CalculateHaversineDistanceOptimized(clickLocation, indexedPin.Location);
                            
                            if (distance <= tolerance)
                            {
                                var score = CalculateSelectionScoreOptimized(distance, tolerance, indexedPin.Spot);
                                cellCandidates.Add(new SpatialCandidate(indexedPin.Spot, distance, score));
                            }
                        }
                        return cellCandidates;
                    }
                    return new List<SpatialCandidate>();
                });

                var allCandidateLists = await Task.WhenAll(searchTasks);
                candidates = allCandidateLists.SelectMany(list => list).ToList();

                if (!candidates.Any())
                {
                    _logger?.LogDebug("No pins found within {Tolerance}km using spatial index", tolerance);
                    return null;
                }

                // Return best candidate
                var selected = candidates.OrderByDescending(c => c.Score).First();
                
                _logger?.LogDebug("Spatial index selected: {SpotName} at {Distance:F3}km", 
                    selected.Spot.Name, selected.Distance);

                return selected.Spot;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in spatial index pin selection");
                return null;
            }
        }

        public bool IsApplicable(DevicePlatform platform, PinSelectionContext context)
        {
            // Enhanced applicability logic with performance considerations
            var isHighPinCount = context.TotalPinCount > 30; // Lowered threshold with optimizations
            var isDenseArea = context.IsHighDensityArea;
            var isZoomedIn = context.MapZoomLevel > 12; // Spatial indexing more effective when zoomed in
            
            return isHighPinCount || (isDenseArea && isZoomedIn);
        }

        /// <summary>
        /// Smart incremental spatial index update with change detection
        /// </summary>
        private async Task UpdateSpatialIndexIncrementalAsync(IEnumerable<Pin> visiblePins, MapSpan? mapViewport)
        {
            var pinHashSet = new HashSet<Pin>(visiblePins.Where(p => p?.Location != null && p.BindingContext is Spot));
            
            // Skip update if pins haven't changed and index is recent
            if (_lastIndexedPins != null && 
                _lastIndexedPins.SetEquals(pinHashSet) && 
                (DateTime.UtcNow - _lastIndexUpdate).TotalMilliseconds < INDEX_REFRESH_THRESHOLD_MS)
            {
                _logger?.LogDebug("Skipping spatial index update - no changes detected");
                return;
            }

            // Adapt cell size based on pin density and zoom level
            AdaptCellSize(pinHashSet.Count, mapViewport);

            await Task.Run(() =>
            {
                _indexLock.EnterWriteLock();
                try
                {
                    // Incremental update: only rebuild if significant changes
                    if (_lastIndexedPins == null || !ShouldUseIncrementalUpdate(pinHashSet))
                    {
                        RebuildCompleteIndex(pinHashSet);
                    }
                    else
                    {
                        UpdateIndexIncremental(pinHashSet);
                    }

                    _lastIndexedPins = pinHashSet;
                    _lastIndexUpdate = DateTime.UtcNow;
                    
                    _logger?.LogDebug("Updated spatial index: {CellCount} cells, {PinCount} pins, cell size: {CellSize}km", 
                        _spatialIndex.Count, pinHashSet.Count, _currentCellSizeKm);
                }
                finally
                {
                    _indexLock.ExitWriteLock();
                }
            });
        }

        /// <summary>
        /// Get spatial cell key for a location with caching
        /// </summary>
        private SpatialCellKey GetSpatialCellKey(Location location)
        {
            // Check cache first
            if (_locationCache.TryGetValue(location, out var cachedKey))
                return cachedKey;

            // Calculate cell coordinates with current adaptive cell size
            var degreesPerKmLat = _currentCellSizeKm / 111.0;
            var degreesPerKmLon = _currentCellSizeKm / (111.0 * Math.Cos(location.Latitude * Math.PI / 180.0));
            
            var cellLat = (int)Math.Floor(location.Latitude / degreesPerKmLat);
            var cellLon = (int)Math.Floor(location.Longitude / degreesPerKmLon);
            
            var key = new SpatialCellKey(cellLat, cellLon);
            
            // Cache with size limit
            if (_locationCache.Count < MAX_CACHE_SIZE)
            {
                _locationCache.TryAdd(location, key);
            }
            
            return key;
        }

        /// <summary>
        /// Get optimized search cells using circular search pattern
        /// </summary>
        private List<SpatialCellKey> GetOptimizedSearchCells(Location center, double toleranceKm)
        {
            var searchCells = new List<SpatialCellKey>();
            
            // Calculate cell radius to search (adaptive based on current cell size)
            var cellRadius = (int)Math.Ceiling(toleranceKm / _currentCellSizeKm) + 1;
            
            var centerKey = GetSpatialCellKey(center);
            var maxDistanceSquared = toleranceKm * toleranceKm;
            
            // Circular search pattern - more efficient than rectangular
            for (int latOffset = -cellRadius; latOffset <= cellRadius; latOffset++)
            {
                for (int lonOffset = -cellRadius; lonOffset <= cellRadius; lonOffset++)
                {
                    // Skip cells that are definitely outside the circular tolerance
                    var cellDistanceSquared = (latOffset * _currentCellSizeKm) * (latOffset * _currentCellSizeKm) + 
                                             (lonOffset * _currentCellSizeKm) * (lonOffset * _currentCellSizeKm);
                    
                    if (cellDistanceSquared <= maxDistanceSquared * 1.5) // Add small buffer for edge cases
                    {
                        var cellKey = new SpatialCellKey(centerKey.Latitude + latOffset, centerKey.Longitude + lonOffset);
                        searchCells.Add(cellKey);
                    }
                }
            }

            return searchCells;
        }

        private static double CalculateToleranceKm(MapSpan? viewport)
        {
            if (viewport == null) return 1.0;

            var avgSpan = (viewport.LatitudeDegrees + viewport.LongitudeDegrees) / 2;
            return avgSpan switch
            {
                > 1.0 => 3.0,
                > 0.5 => 2.0,
                > 0.1 => 1.0,
                > 0.05 => 0.5,
                _ => 0.3
            };
        }

        /// <summary>
        /// Optimized Haversine distance calculation with early exit for close points
        /// </summary>
        private static double CalculateHaversineDistanceOptimized(Location loc1, Location loc2)
        {
            const double earthRadiusKm = 6371.0;
            const double degToRad = Math.PI / 180.0;
            
            var dLat = (loc2.Latitude - loc1.Latitude) * degToRad;
            var dLon = (loc2.Longitude - loc1.Longitude) * degToRad;
            
            // Quick check for very close points to avoid expensive trig calculations
            if (Math.Abs(dLat) < 0.0001 && Math.Abs(dLon) < 0.0001)
            {
                return Math.Sqrt(dLat * dLat + dLon * dLon) * earthRadiusKm;
            }
            
            var lat1Rad = loc1.Latitude * degToRad;
            var lat2Rad = loc2.Latitude * degToRad;
            
            var sinDLat2 = Math.Sin(dLat * 0.5);
            var sinDLon2 = Math.Sin(dLon * 0.5);
            
            var a = sinDLat2 * sinDLat2 + Math.Cos(lat1Rad) * Math.Cos(lat2Rad) * sinDLon2 * sinDLon2;
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return earthRadiusKm * c;
        }

        /// <summary>
        /// Enhanced selection scoring with multiple factors
        /// </summary>
        private static double CalculateSelectionScoreOptimized(double distance, double tolerance, Spot spot)
        {
            // Base distance score (exponential decay for better discrimination)
            var normalizedDistance = distance / tolerance;
            var distanceScore = Math.Exp(-normalizedDistance * 2.0); // Exponential decay
            
            // Priority modifiers
            var priorityModifier = 1.0;
            
            // Validation status bonus
            priorityModifier *= spot.ValidationStatus switch
            {
                SpotValidationStatus.Approved => 1.2,
                SpotValidationStatus.Pending => 1.0,
                SpotValidationStatus.Rejected => 0.8,
                _ => 1.0
            };
            
            // Additional factors can be added here:
            // - Spot popularity, recency, user rating, etc.
            
            return distanceScore * priorityModifier;
        }

        #region Helper Methods
        
        /// <summary>
        /// Adapt cell size based on pin density and zoom level
        /// </summary>
        private void AdaptCellSize(int pinCount, MapSpan? mapViewport)
        {
            if (mapViewport == null) return;
            
            var zoomLevel = CalculateZoomLevel(mapViewport);
            var density = pinCount / Math.Max(mapViewport.LatitudeDegrees * mapViewport.LongitudeDegrees, 0.001);
            
            // Adaptive cell sizing: smaller cells for high density or high zoom
            _currentCellSizeKm = zoomLevel switch
            {
                >= 15 => Math.Max(MIN_CELL_SIZE_KM, 0.5 - (density * 0.1)),
                >= 12 => Math.Max(MIN_CELL_SIZE_KM, 1.0 - (density * 0.2)),
                >= 9 => Math.Min(MAX_CELL_SIZE_KM, 2.0 + (density * 0.3)),
                _ => Math.Min(MAX_CELL_SIZE_KM, 3.0)
            };
            
            // Clear location cache when cell size changes significantly
            if (Math.Abs(_currentCellSizeKm - 1.0) > 0.5)
            {
                _locationCache.Clear();
            }
        }
        
        private static double CalculateZoomLevel(MapSpan mapViewport)
        {
            var avgSpan = (mapViewport.LatitudeDegrees + mapViewport.LongitudeDegrees) / 2;
            return Math.Max(1, 20 - Math.Log2(avgSpan * 100));
        }
        
        private bool ShouldUseIncrementalUpdate(HashSet<Pin> newPins)
        {
            if (_lastIndexedPins == null) return false;
            
            var addedPins = newPins.Except(_lastIndexedPins).Count();
            var removedPins = _lastIndexedPins.Except(newPins).Count();
            var totalChanges = addedPins + removedPins;
            
            // Use incremental if changes are less than 30% of total
            return totalChanges < (newPins.Count * 0.3);
        }
        
        private void RebuildCompleteIndex(HashSet<Pin> pins)
        {
            _spatialIndex.Clear();
            _locationCache.Clear();
            
            foreach (var pin in pins)
            {
                if (pin?.Location == null || pin.BindingContext is not Spot spot)
                    continue;
                    
                AddPinToIndex(pin, spot);
            }
        }
        
        private void UpdateIndexIncremental(HashSet<Pin> newPins)
        {
            // Remove old pins
            var removedPins = _lastIndexedPins!.Except(newPins);
            foreach (var pin in removedPins)
            {
                RemovePinFromIndex(pin);
            }
            
            // Add new pins
            var addedPins = newPins.Except(_lastIndexedPins!);
            foreach (var pin in addedPins)
            {
                if (pin?.Location != null && pin.BindingContext is Spot spot)
                {
                    AddPinToIndex(pin, spot);
                }
            }
        }
        
        private void AddPinToIndex(Pin pin, Spot spot)
        {
            var cellKey = GetSpatialCellKey(pin.Location);
            var indexedPin = new IndexedPin(spot, pin.Location);
            
            _spatialIndex.AddOrUpdate(cellKey,
                new SpatialCell { Pins = new List<IndexedPin> { indexedPin } },
                (key, existingCell) =>
                {
                    existingCell.Pins.Add(indexedPin);
                    return existingCell;
                });
        }
        
        private void RemovePinFromIndex(Pin pin)
        {
            if (pin?.Location == null || pin.BindingContext is not Spot spot) return;
            
            var cellKey = GetSpatialCellKey(pin.Location);
            if (_spatialIndex.TryGetValue(cellKey, out var cell))
            {
                cell.Pins.RemoveAll(p => ReferenceEquals(p.Spot, spot));
                if (cell.Pins.Count == 0)
                {
                    _spatialIndex.TryRemove(cellKey, out _);
                }
            }
        }
        
        #endregion
        
        #region Data Structures
        
        /// <summary>
        /// Optimized spatial cell key using integers instead of strings
        /// </summary>
        private readonly struct SpatialCellKey : IEquatable<SpatialCellKey>
        {
            public readonly int Latitude;
            public readonly int Longitude;
            
            public SpatialCellKey(int latitude, int longitude)
            {
                Latitude = latitude;
                Longitude = longitude;
            }
            
            public bool Equals(SpatialCellKey other) => Latitude == other.Latitude && Longitude == other.Longitude;
            public override bool Equals(object? obj) => obj is SpatialCellKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);
            public static bool operator ==(SpatialCellKey left, SpatialCellKey right) => left.Equals(right);
            public static bool operator !=(SpatialCellKey left, SpatialCellKey right) => !left.Equals(right);
        }
        
        /// <summary>
        /// Represents a spatial grid cell containing indexed pins
        /// </summary>
        private class SpatialCell
        {
            public List<IndexedPin> Pins { get; set; } = new();
        }

        /// <summary>
        /// Represents a pin with its location in the spatial index
        /// </summary>
        private readonly struct IndexedPin
        {
            public readonly Spot Spot;
            public readonly Location Location;

            public IndexedPin(Spot spot, Location location)
            {
                Spot = spot;
                Location = location;
            }
        }

        /// <summary>
        /// Represents a candidate pin with selection metadata
        /// </summary>
        private readonly struct SpatialCandidate
        {
            public readonly Spot Spot;
            public readonly double Distance;
            public readonly double Score;

            public SpatialCandidate(Spot spot, double distance, double score)
            {
                Spot = spot;
                Distance = distance;
                Score = score;
            }
        }
        
        #endregion
    }
}