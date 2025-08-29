using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for managing offline capabilities and data synchronization
    /// </summary>
    public interface IOfflineCapabilityService
    {
        /// <summary>
        /// Check if the app can operate in offline mode
        /// </summary>
        bool CanWorkOffline { get; }

        /// <summary>
        /// Current offline mode status
        /// </summary>
        bool IsOfflineModeEnabled { get; }

        /// <summary>
        /// Get offline capabilities for specific features
        /// </summary>
        OfflineCapabilities GetOfflineCapabilities();

        /// <summary>
        /// Enable offline mode
        /// </summary>
        Task EnableOfflineModeAsync();

        /// <summary>
        /// Disable offline mode
        /// </summary>
        Task DisableOfflineModeAsync();

        /// <summary>
        /// Synchronize offline data when connection is available
        /// </summary>
        Task SynchronizeDataAsync();

        /// <summary>
        /// Get pending offline operations
        /// </summary>
        Task<IEnumerable<OfflineOperation>> GetPendingOperationsAsync();

        /// <summary>
        /// Clear all offline data
        /// </summary>
        Task ClearOfflineDataAsync();

        /// <summary>
        /// Check if specific feature is available offline
        /// </summary>
        bool IsFeatureAvailableOffline(string featureName);

        /// <summary>
        /// Get offline data size information
        /// </summary>
        Task<OfflineDataInfo> GetOfflineDataInfoAsync();

        /// <summary>
        /// Event fired when offline mode status changes
        /// </summary>
        event EventHandler<OfflineModeChangedEventArgs> OfflineModeChanged;

        /// <summary>
        /// Event fired when offline data is synchronized
        /// </summary>
        event EventHandler<OfflineDataSyncedEventArgs> OfflineDataSynced;
    }

    /// <summary>
    /// Offline capabilities for different app features
    /// </summary>
    public class OfflineCapabilities
    {
        public bool CanViewCachedSpots { get; set; }
        public bool CanViewCachedImages { get; set; }
        public bool CanCreateOfflineSpots { get; set; }
        public bool CanAddOfflineFavorites { get; set; }
        public bool CanViewOfflineMaps { get; set; }
        public bool CanSyncOnReconnection { get; set; }
        
        public Dictionary<string, bool> FeatureAvailability { get; set; } = new();
        
        public int CachedSpotsCount { get; set; }
        public int CachedImagesCount { get; set; }
        public long CachedDataSizeBytes { get; set; }
    }

    /// <summary>
    /// Offline operation types
    /// </summary>
    public enum OfflineOperationType
    {
        CreateSpot,
        UpdateSpot,
        DeleteSpot,
        AddFavorite,
        RemoveFavorite,
        AddSpotImage,
        UpdateUserProfile,
        SubmitSpotReport
    }

    /// <summary>
    /// Pending offline operation
    /// </summary>
    public class OfflineOperation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public OfflineOperationType Type { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty; // JSON data
        public DateTime CreatedAt { get; set; }
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; set; }
        public bool RequiresUserConfirmation { get; set; }
    }

    /// <summary>
    /// Offline data size and usage information
    /// </summary>
    public class OfflineDataInfo
    {
        public long TotalSizeBytes { get; set; }
        public long ImageCacheSizeBytes { get; set; }
        public long DataCacheSizeBytes { get; set; }
        public long PendingOperationsSizeBytes { get; set; }
        
        public int CachedSpotsCount { get; set; }
        public int CachedImagesCount { get; set; }
        public int PendingOperationsCount { get; set; }
        
        public DateTime LastSyncDate { get; set; }
        public DateTime LastCleanupDate { get; set; }
        
        public TimeSpan EstimatedDataAge { get; set; }
        public bool IsDataStale { get; set; }
    }

    /// <summary>
    /// Event arguments for offline mode changes
    /// </summary>
    public class OfflineModeChangedEventArgs : EventArgs
    {
        public bool IsOfflineModeEnabled { get; set; }
        public string? Reason { get; set; }
        public OfflineCapabilities Capabilities { get; set; }

        public OfflineModeChangedEventArgs(bool isOfflineModeEnabled, string? reason, OfflineCapabilities capabilities)
        {
            IsOfflineModeEnabled = isOfflineModeEnabled;
            Reason = reason;
            Capabilities = capabilities;
        }
    }

    /// <summary>
    /// Event arguments for offline data synchronization
    /// </summary>
    public class OfflineDataSyncedEventArgs : EventArgs
    {
        public int SyncedOperationsCount { get; set; }
        public int FailedOperationsCount { get; set; }
        public TimeSpan SyncDuration { get; set; }
        public DateTime SyncCompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        
        public bool IsSuccessful => FailedOperationsCount == 0 && string.IsNullOrEmpty(ErrorMessage);

        public OfflineDataSyncedEventArgs(
            int syncedOperationsCount, 
            int failedOperationsCount, 
            TimeSpan syncDuration, 
            string? errorMessage = null)
        {
            SyncedOperationsCount = syncedOperationsCount;
            FailedOperationsCount = failedOperationsCount;
            SyncDuration = syncDuration;
            SyncCompletedAt = DateTime.UtcNow;
            ErrorMessage = errorMessage;
        }
    }
}