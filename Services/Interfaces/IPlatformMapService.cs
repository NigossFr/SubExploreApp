using Microsoft.Maui.Controls.Maps;
using MapControl = Microsoft.Maui.Controls.Maps.Map;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Platform-specific map service for handling cross-platform map functionality
    /// </summary>
    public interface IPlatformMapService
    {
        /// <summary>
        /// Initialize platform-specific map configuration
        /// </summary>
        Task<bool> InitializePlatformMapAsync();
        
        /// <summary>
        /// Validate platform-specific map configuration
        /// </summary>
        Task<bool> ValidateMapConfigurationAsync();
        
        /// <summary>
        /// Get platform-specific map diagnostic information
        /// </summary>
        Task<string> GetPlatformDiagnosticsAsync();
        
        /// <summary>
        /// Check if map tiles are loading properly on this platform
        /// </summary>
        Task<bool> TestMapTileAccessibilityAsync();
        
        /// <summary>
        /// Get platform-specific map type recommendations (as string to avoid platform-specific enum issues)
        /// </summary>
        string GetRecommendedMapType();
        
        /// <summary>
        /// Apply platform-specific map optimizations
        /// </summary>
        void ApplyMapOptimizations(MapControl map);
        
        /// <summary>
        /// Refresh map display with platform-specific optimizations
        /// </summary>
        void RefreshMapDisplay(MapControl map);
    }
}