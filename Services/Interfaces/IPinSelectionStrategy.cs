using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Strategy interface for pin selection algorithms
    /// Enables platform-specific and optimized pin selection implementations
    /// </summary>
    public interface IPinSelectionStrategy
    {
        /// <summary>
        /// Select a pin based on click location using the strategy's algorithm
        /// </summary>
        /// <param name="clickLocation">The location where the user clicked</param>
        /// <param name="visiblePins">Collection of pins visible in current viewport</param>
        /// <param name="mapViewport">Current map viewport for context</param>
        /// <returns>Selected spot or null if no match found</returns>
        Task<Spot?> SelectPinAsync(Location clickLocation, IEnumerable<Pin> visiblePins, MapSpan? mapViewport = null);
        
        /// <summary>
        /// Get the name of this selection strategy for diagnostics
        /// </summary>
        string StrategyName { get; }
        
        /// <summary>
        /// Check if this strategy is suitable for the current platform and context
        /// </summary>
        /// <param name="platform">Current platform</param>
        /// <param name="context">Selection context information</param>
        /// <returns>True if strategy is optimal for this context</returns>
        bool IsApplicable(DevicePlatform platform, PinSelectionContext context);
    }

    /// <summary>
    /// Context information for pin selection to help strategies make optimal decisions
    /// </summary>
    public class PinSelectionContext
    {
        public int TotalPinCount { get; set; }
        public double MapZoomLevel { get; set; }
        public bool IsHighDensityArea { get; set; }
        public SelectionPrecision RequiredPrecision { get; set; } = SelectionPrecision.Standard;
    }

    /// <summary>
    /// Precision requirements for pin selection
    /// </summary>
    public enum SelectionPrecision
    {
        /// <summary>Coarse selection for overview maps</summary>
        Coarse,
        /// <summary>Standard selection for normal usage</summary>
        Standard,
        /// <summary>High precision for detailed maps</summary>
        High,
        /// <summary>Maximum precision for dense pin areas</summary>
        Maximum
    }
}