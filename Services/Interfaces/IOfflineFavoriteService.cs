using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for managing favorites in offline mode with intelligent sync
    /// </summary>
    public interface IOfflineFavoriteService
    {
        /// <summary>
        /// Check if offline mode is active
        /// </summary>
        bool IsOfflineModeActive { get; }

        /// <summary>
        /// Enable offline mode with cached data
        /// </summary>
        Task<bool> EnableOfflineModeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Disable offline mode and sync pending changes
        /// </summary>
        Task<bool> DisableOfflineModeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get cached favorites for offline viewing
        /// </summary>
        Task<IEnumerable<UserFavoriteSpot>> GetOfflineFavoritesAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add favorite in offline mode (queued for sync)
        /// </summary>
        Task<bool> AddOfflineFavoriteAsync(Guid userId, Guid spotId, int priority = 5, string? notes = null, bool notificationEnabled = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove favorite in offline mode (queued for sync)
        /// </summary>
        Task<bool> RemoveOfflineFavoriteAsync(Guid userId, Guid spotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update favorite in offline mode (queued for sync)
        /// </summary>
        Task<bool> UpdateOfflineFavoriteAsync(Guid userId, Guid spotId, int? priority = null, string? notes = null, bool? notificationEnabled = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get pending sync operations count
        /// </summary>
        Task<int> GetPendingSyncCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get pending sync operations
        /// </summary>
        Task<IEnumerable<OfflineSyncOperation>> GetPendingSyncOperationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sync pending operations when network is available
        /// </summary>
        Task<OfflineSyncResult> SyncPendingOperationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear all offline data and pending operations
        /// </summary>
        Task ClearOfflineDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get offline storage size information
        /// </summary>
        Task<OfflineStorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Event fired when offline mode status changes
        /// </summary>
        event EventHandler<FavoriteOfflineModeChangedEventArgs> OfflineModeChanged;

        /// <summary>
        /// Event fired when sync operation completes
        /// </summary>
        event EventHandler<OfflineSyncCompletedEventArgs> SyncCompleted;

        /// <summary>
        /// Event fired when pending operations count changes
        /// </summary>
        event EventHandler<PendingOperationsChangedEventArgs> PendingOperationsChanged;
    }

    /// <summary>
    /// Offline sync operation model
    /// </summary>
    public class OfflineSyncOperation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public OfflineSyncOperationType Type { get; set; }
        public Guid UserId { get; set; }
        public Guid SpotId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public bool IsProcessing { get; set; } = false;

        public void AddParameter(string key, object value)
        {
            Parameters[key] = value;
        }

        public T GetParameter<T>(string key, T defaultValue = default)
        {
            if (Parameters.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// Types of offline sync operations
    /// </summary>
    public enum OfflineSyncOperationType
    {
        AddFavorite,
        RemoveFavorite,
        UpdateFavoritePriority,
        UpdateFavoriteNotes,
        UpdateFavoriteNotification
    }

    /// <summary>
    /// Result of offline sync operation
    /// </summary>
    public class OfflineSyncResult
    {
        public bool IsSuccess { get; set; }
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public int SkippedOperations { get; set; }
        public TimeSpan SyncDuration { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime SyncTimestamp { get; set; } = DateTime.UtcNow;

        public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations * 100 : 0;
    }

    /// <summary>
    /// Offline storage information
    /// </summary>
    public class OfflineStorageInfo
    {
        public long TotalSizeBytes { get; set; }
        public long FavoritesCacheSizeBytes { get; set; }
        public long PendingOperationsSizeBytes { get; set; }
        public int CachedFavoritesCount { get; set; }
        public int PendingOperationsCount { get; set; }
        public DateTime LastSyncDate { get; set; }
        public DateTime LastCacheUpdate { get; set; }

        public string GetFormattedSize()
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (TotalSizeBytes >= GB)
                return $"{TotalSizeBytes / (double)GB:F2} GB";
            if (TotalSizeBytes >= MB)
                return $"{TotalSizeBytes / (double)MB:F2} MB";
            if (TotalSizeBytes >= KB)
                return $"{TotalSizeBytes / (double)KB:F2} KB";
            return $"{TotalSizeBytes} bytes";
        }
    }

    /// <summary>
    /// Event arguments for offline mode changes (favorites-specific)
    /// </summary>
    public class FavoriteOfflineModeChangedEventArgs : EventArgs
    {
        public bool IsOfflineMode { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event arguments for sync completion
    /// </summary>
    public class OfflineSyncCompletedEventArgs : EventArgs
    {
        public OfflineSyncResult Result { get; set; } = new();
        public bool WasTriggeredByUser { get; set; }
    }

    /// <summary>
    /// Event arguments for pending operations changes
    /// </summary>
    public class PendingOperationsChangedEventArgs : EventArgs
    {
        public int PendingCount { get; set; }
        public int PreviousCount { get; set; }
        public OfflineSyncOperationType? LastOperationType { get; set; }
    }
}