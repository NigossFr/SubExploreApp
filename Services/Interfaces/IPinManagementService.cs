using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.Models.Domain;
using System.Collections.ObjectModel;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Advanced pin management service with caching, pooling, and viewport optimization
    /// </summary>
    public interface IPinManagementService
    {
        /// <summary>
        /// Create or retrieve cached pins for spots with viewport optimization
        /// </summary>
        /// <param name="spots">Spots to create pins for</param>
        /// <param name="viewport">Current map viewport for optimization</param>
        /// <returns>Collection of optimized pins</returns>
        Task<ObservableCollection<Pin>> GetOptimizedPinsAsync(IEnumerable<Spot> spots, MapSpan? viewport = null);

        /// <summary>
        /// Create pins in batches with performance monitoring
        /// </summary>
        /// <param name="spots">Spots to process</param>
        /// <param name="batchSize">Size of each batch</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processed pins with performance metrics</returns>
        Task<PinCreationResult> CreatePinsInBatchesAsync(IEnumerable<Spot> spots, int batchSize = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update existing pins collection with minimal changes
        /// </summary>
        /// <param name="currentPins">Current pins collection</param>
        /// <param name="newSpots">New spots to process</param>
        /// <returns>Updated pins collection</returns>
        Task<ObservableCollection<Pin>> UpdatePinsIncrementallyAsync(ObservableCollection<Pin> currentPins, IEnumerable<Spot> newSpots);

        /// <summary>
        /// Clear pin cache and reset pooling
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Get pin management statistics
        /// </summary>
        /// <returns>Performance and usage statistics</returns>
        PinManagementStats GetStats();

        /// <summary>
        /// Optimize pins for current viewport (remove invisible pins, add visible ones)
        /// </summary>
        /// <param name="currentPins">Current pins collection</param>
        /// <param name="viewport">Current map viewport</param>
        /// <param name="allSpots">All available spots</param>
        /// <returns>Viewport-optimized pins</returns>
        Task<ObservableCollection<Pin>> OptimizeForViewportAsync(ObservableCollection<Pin> currentPins, MapSpan viewport, IEnumerable<Spot> allSpots);

        /// <summary>
        /// Enable or disable debouncing for pin updates
        /// </summary>
        /// <param name="enabled">Whether to enable debouncing</param>
        /// <param name="debounceDelayMs">Debounce delay in milliseconds</param>
        void SetDebouncing(bool enabled, int debounceDelayMs = 300);
    }

    /// <summary>
    /// Result of pin creation operations with performance metrics
    /// </summary>
    public class PinCreationResult
    {
        public ObservableCollection<Pin> Pins { get; set; } = new();
        public int ProcessedCount { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public double ProcessingTimeMs { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
        public string ProcessingStrategy { get; set; } = "Standard";
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }

    /// <summary>
    /// Pin management performance statistics
    /// </summary>
    public class PinManagementStats
    {
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int TotalPinsCreated { get; set; }
        public int CacheHitCount { get; set; }
        public int CacheMissCount { get; set; }
        public double CacheHitRate { get; set; }
        public int PooledPinsAvailable { get; set; }
        public int PooledPinsUsed { get; set; }
        public double AverageCreationTimeMs { get; set; }
        public int ViewportOptimizationCount { get; set; }
        public int DebounceOperationsSaved { get; set; }
        public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
    }

    /// <summary>
    /// Configuration for pin management optimization
    /// </summary>
    public class PinManagementConfig
    {
        public int MaxCacheSize { get; set; } = 1000;
        public int MaxPoolSize { get; set; } = 200;
        public int ViewportPadding { get; set; } = 100; // meters
        public bool EnableViewportOptimization { get; set; } = true;
        public bool EnablePinCaching { get; set; } = true;
        public bool EnablePinPooling { get; set; } = true;
        public int DebounceDelayMs { get; set; } = 300;
        public int BatchSize { get; set; } = 50;
        public int MaxConcurrentBatches { get; set; } = 3;
    }
}