using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for managing pin selection strategies and coordinating pin interactions
    /// Provides a clean abstraction layer for pin selection operations
    /// </summary>
    public interface IPinSelectionService
    {
        /// <summary>
        /// Select a pin based on user interaction with automatic strategy selection
        /// </summary>
        /// <param name="clickLocation">Location where user clicked/tapped</param>
        /// <param name="visiblePins">Pins currently visible on the map</param>
        /// <param name="mapViewport">Current map viewport for context</param>
        /// <param name="pinCount">Total number of pins for optimization decisions</param>
        /// <returns>Selected spot or null if no pin was selected</returns>
        Task<Spot?> SelectPinAsync(Location clickLocation, IEnumerable<Pin> visiblePins, 
            MapSpan? mapViewport = null, int pinCount = 0);

        /// <summary>
        /// Select a pin using a specific strategy
        /// </summary>
        /// <param name="strategy">Strategy to use for selection</param>
        /// <param name="clickLocation">Location where user clicked/tapped</param>
        /// <param name="visiblePins">Pins currently visible on the map</param>
        /// <param name="mapViewport">Current map viewport for context</param>
        /// <returns>Selected spot or null if no pin was selected</returns>
        Task<Spot?> SelectPinWithStrategyAsync(IPinSelectionStrategy strategy, Location clickLocation, 
            IEnumerable<Pin> visiblePins, MapSpan? mapViewport = null);

        /// <summary>
        /// Get the optimal selection strategy for current context
        /// </summary>
        /// <param name="pinCount">Number of pins to consider</param>
        /// <param name="mapViewport">Current map viewport</param>
        /// <param name="platform">Current device platform</param>
        /// <returns>Best strategy for the given context</returns>
        IPinSelectionStrategy GetOptimalStrategy(int pinCount, MapSpan? mapViewport = null, 
            DevicePlatform? platform = null);

        /// <summary>
        /// Get all available selection strategies
        /// </summary>
        /// <returns>Collection of available strategies</returns>
        IEnumerable<IPinSelectionStrategy> GetAvailableStrategies();

        /// <summary>
        /// Get performance metrics for strategy selection decisions
        /// </summary>
        /// <returns>Performance metrics and statistics</returns>
        PinSelectionMetrics GetPerformanceMetrics();
    }

    /// <summary>
    /// Performance metrics for pin selection operations
    /// </summary>
    public class PinSelectionMetrics
    {
        public Dictionary<string, int> StrategyUsageCount { get; set; } = new();
        public Dictionary<string, double> AverageSelectionTimeMs { get; set; } = new();
        public Dictionary<string, double> SuccessRate { get; set; } = new();
        public int TotalSelections { get; set; }
        public DateTime LastResetTime { get; set; } = DateTime.UtcNow;
    }
}