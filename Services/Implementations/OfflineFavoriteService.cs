using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Enhanced offline favorites service with intelligent sync and caching
    /// </summary>
    public class OfflineFavoriteService : IOfflineFavoriteService
    {
        private readonly IFavoriteSpotService? _favoriteSpotService;
        private readonly IFavoriteSpotCacheService _cacheService;
        private readonly INetworkHealthService _networkService;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<OfflineFavoriteService> _logger;

        private readonly SemaphoreSlim _syncSemaphore = new(1, 1);
        private readonly List<OfflineSyncOperation> _pendingOperations = new();
        private bool _isOfflineModeActive = false;

        // Settings keys
        private const string OfflineModeEnabledKey = "offline_favorites_enabled";
        private const string LastSyncDateKey = "offline_favorites_last_sync";
        private const string PendingOperationsKey = "offline_favorites_pending_operations";

        public bool IsOfflineModeActive => _isOfflineModeActive;

        public event EventHandler<FavoriteOfflineModeChangedEventArgs>? OfflineModeChanged;
        public event EventHandler<OfflineSyncCompletedEventArgs>? SyncCompleted;
        public event EventHandler<PendingOperationsChangedEventArgs>? PendingOperationsChanged;

        public OfflineFavoriteService(
            IFavoriteSpotService? favoriteSpotService,
            IFavoriteSpotCacheService cacheService,
            INetworkHealthService networkService,
            ISettingsService settingsService,
            ILogger<OfflineFavoriteService> logger)
        {
            _favoriteSpotService = favoriteSpotService; // Peut être null pour éviter dépendance circulaire
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize offline mode state
            _isOfflineModeActive = _settingsService.Get(OfflineModeEnabledKey, false);
            LoadPendingOperations();

            // Subscribe to network changes for auto-sync
            _networkService.HealthStatusChanged += OnNetworkStatusChanged;
        }

        /// <summary>
        /// Enable offline mode with cached data
        /// </summary>
        public async Task<bool> EnableOfflineModeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Enabling offline mode for favorites");

                // Check if we have network to cache fresh data
                if (_networkService.CurrentStatus.IsConnected)
                {
                    _logger.LogInformation("Network available - caching latest favorites data");
                    // This would cache current user's favorites for offline use
                }

                _isOfflineModeActive = true;
                _settingsService.Set(OfflineModeEnabledKey, true);

                OnOfflineModeChanged(true, "User enabled offline mode");
                _logger.LogInformation("Offline mode enabled successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable offline mode");
                return false;
            }
        }

        /// <summary>
        /// Disable offline mode and sync pending changes
        /// </summary>
        public async Task<bool> DisableOfflineModeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Disabling offline mode for favorites");

                // Sync pending operations before disabling
                if (_pendingOperations.Any() && _networkService.CurrentStatus.IsConnected)
                {
                    _logger.LogInformation("Syncing {Count} pending operations before disabling offline mode", _pendingOperations.Count);
                    await SyncPendingOperationsAsync(cancellationToken);
                }

                _isOfflineModeActive = false;
                _settingsService.Set(OfflineModeEnabledKey, false);

                OnOfflineModeChanged(false, "User disabled offline mode");
                _logger.LogInformation("Offline mode disabled successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disable offline mode");
                return false;
            }
        }

        /// <summary>
        /// Get cached favorites for offline viewing
        /// </summary>
        public async Task<IEnumerable<UserFavoriteSpot>> GetOfflineFavoritesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving offline favorites for user {UserId}", userId);

                // Try cache first (priority order)
                var cachedFavorites = await _cacheService.GetCachedUserFavoritesAsync(userId, true, cancellationToken);
                if (cachedFavorites != null)
                {
                    _logger.LogDebug("Found {Count} cached favorites for user {UserId}", cachedFavorites.Count(), userId);
                    return cachedFavorites;
                }

                // Fallback to regular favorites if network is available and not in offline mode
                if (!_isOfflineModeActive && _networkService.CurrentStatus.IsConnected)
                {
                    _logger.LogDebug("No cached favorites found, fetching from network");
                    var freshFavorites = await _favoriteSpotService.GetUserFavoritesByPriorityAsync(userId, cancellationToken);
                    
                    // Cache for future offline use
                    await _cacheService.SetUserFavoritesCacheAsync(userId, freshFavorites, true, cancellationToken);
                    
                    return freshFavorites;
                }

                _logger.LogWarning("No cached favorites available for user {UserId} in offline mode", userId);
                return Enumerable.Empty<UserFavoriteSpot>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve offline favorites for user {UserId}", userId);
                return Enumerable.Empty<UserFavoriteSpot>();
            }
        }

        /// <summary>
        /// Add favorite in offline mode (queued for sync)
        /// </summary>
        public async Task<bool> AddOfflineFavoriteAsync(Guid userId, Guid spotId, int priority = 5, string? notes = null, bool notificationEnabled = true, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Adding favorite in offline mode: user {UserId}, spot {SpotId}", userId, spotId);

                var operation = new OfflineSyncOperation
                {
                    Type = OfflineSyncOperationType.AddFavorite,
                    UserId = userId,
                    SpotId = spotId
                };
                operation.AddParameter("priority", priority);
                operation.AddParameter("notes", notes ?? "");
                operation.AddParameter("notificationEnabled", notificationEnabled);

                await AddPendingOperationAsync(operation);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add offline favorite: user {UserId}, spot {SpotId}", userId, spotId);
                return false;
            }
        }

        /// <summary>
        /// Remove favorite in offline mode (queued for sync)
        /// </summary>
        public async Task<bool> RemoveOfflineFavoriteAsync(Guid userId, Guid spotId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Removing favorite in offline mode: user {UserId}, spot {SpotId}", userId, spotId);

                var operation = new OfflineSyncOperation
                {
                    Type = OfflineSyncOperationType.RemoveFavorite,
                    UserId = userId,
                    SpotId = spotId
                };

                await AddPendingOperationAsync(operation);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove offline favorite: user {UserId}, spot {SpotId}", userId, spotId);
                return false;
            }
        }

        /// <summary>
        /// Update favorite in offline mode (queued for sync)
        /// </summary>
        public async Task<bool> UpdateOfflineFavoriteAsync(Guid userId, Guid spotId, int? priority = null, string? notes = null, bool? notificationEnabled = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Updating favorite in offline mode: user {UserId}, spot {SpotId}", userId, spotId);

                if (priority.HasValue)
                {
                    var priorityOperation = new OfflineSyncOperation
                    {
                        Type = OfflineSyncOperationType.UpdateFavoritePriority,
                        UserId = userId,
                        SpotId = spotId
                    };
                    priorityOperation.AddParameter("priority", priority.Value);
                    await AddPendingOperationAsync(priorityOperation);
                }

                if (notes != null)
                {
                    var notesOperation = new OfflineSyncOperation
                    {
                        Type = OfflineSyncOperationType.UpdateFavoriteNotes,
                        UserId = userId,
                        SpotId = spotId
                    };
                    notesOperation.AddParameter("notes", notes);
                    await AddPendingOperationAsync(notesOperation);
                }

                if (notificationEnabled.HasValue)
                {
                    var notificationOperation = new OfflineSyncOperation
                    {
                        Type = OfflineSyncOperationType.UpdateFavoriteNotification,
                        UserId = userId,
                        SpotId = spotId
                    };
                    notificationOperation.AddParameter("enabled", notificationEnabled.Value);
                    await AddPendingOperationAsync(notificationOperation);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update offline favorite: user {UserId}, spot {SpotId}", userId, spotId);
                return false;
            }
        }

        /// <summary>
        /// Get pending sync operations count
        /// </summary>
        public async Task<int> GetPendingSyncCountAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_pendingOperations.Count);
        }

        /// <summary>
        /// Get pending sync operations
        /// </summary>
        public async Task<IEnumerable<OfflineSyncOperation>> GetPendingSyncOperationsAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_pendingOperations.ToList());
        }

        /// <summary>
        /// Sync pending operations when network is available
        /// </summary>
        public async Task<OfflineSyncResult> SyncPendingOperationsAsync(CancellationToken cancellationToken = default)
        {
            var result = new OfflineSyncResult();
            var startTime = DateTime.UtcNow;

            try
            {
                await _syncSemaphore.WaitAsync(cancellationToken);

                _logger.LogInformation("Starting sync of {Count} pending operations", _pendingOperations.Count);
                result.TotalOperations = _pendingOperations.Count;

                if (!_networkService.CurrentStatus.IsConnected)
                {
                    result.Errors.Add("No network connection available");
                    result.SkippedOperations = result.TotalOperations;
                    _logger.LogWarning("Sync aborted - no network connection");
                    return result;
                }

                var operationsToSync = _pendingOperations.Where(op => !op.IsProcessing && op.RetryCount < op.MaxRetries).ToList();

                foreach (var operation in operationsToSync)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    operation.IsProcessing = true;

                    try
                    {
                        bool success = await ExecuteSyncOperationAsync(operation);
                        
                        if (success)
                        {
                            result.SuccessfulOperations++;
                            _pendingOperations.Remove(operation);
                            _logger.LogDebug("Successfully synced operation {OperationId} ({Type})", operation.Id, operation.Type);
                        }
                        else
                        {
                            operation.RetryCount++;
                            if (operation.RetryCount >= operation.MaxRetries)
                            {
                                result.FailedOperations++;
                                _pendingOperations.Remove(operation);
                                _logger.LogWarning("Operation {OperationId} failed after {RetryCount} attempts", operation.Id, operation.RetryCount);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        operation.ErrorMessage = ex.Message;
                        operation.RetryCount++;
                        result.Errors.Add($"Operation {operation.Id}: {ex.Message}");
                        
                        if (operation.RetryCount >= operation.MaxRetries)
                        {
                            result.FailedOperations++;
                            _pendingOperations.Remove(operation);
                        }
                        
                        _logger.LogError(ex, "Error syncing operation {OperationId}", operation.Id);
                    }
                    finally
                    {
                        operation.IsProcessing = false;
                    }
                }

                // Save updated pending operations
                await SavePendingOperationsAsync();
                _settingsService.Set(LastSyncDateKey, DateTime.UtcNow.ToString("O"));

                result.IsSuccess = result.FailedOperations == 0;
                result.SyncDuration = DateTime.UtcNow - startTime;

                OnPendingOperationsChanged(_pendingOperations.Count);
                OnSyncCompleted(result, false);

                _logger.LogInformation("Sync completed: {Successful} successful, {Failed} failed, {Skipped} skipped in {Duration}ms", 
                    result.SuccessfulOperations, result.FailedOperations, result.SkippedOperations, result.SyncDuration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Sync process failed: {ex.Message}");
                result.SyncDuration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Sync process failed");
                return result;
            }
            finally
            {
                _syncSemaphore.Release();
            }
        }

        /// <summary>
        /// Clear all offline data and pending operations
        /// </summary>
        public async Task ClearOfflineDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Clearing all offline favorites data");

                _pendingOperations.Clear();
                await SavePendingOperationsAsync();
                
                // Clear settings
                _settingsService.Remove(OfflineModeEnabledKey);
                _settingsService.Remove(LastSyncDateKey);
                _settingsService.Remove(PendingOperationsKey);

                OnPendingOperationsChanged(0);
                _logger.LogInformation("Offline data cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear offline data");
            }
        }

        /// <summary>
        /// Get offline storage size information
        /// </summary>
        public async Task<OfflineStorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var info = new OfflineStorageInfo
                {
                    PendingOperationsCount = _pendingOperations.Count,
                    LastSyncDate = DateTime.TryParse(_settingsService.Get(LastSyncDateKey, ""), out var lastSync) ? lastSync : DateTime.MinValue
                };

                // Estimate storage sizes
                var pendingOperationsJson = JsonSerializer.Serialize(_pendingOperations);
                info.PendingOperationsSizeBytes = System.Text.Encoding.UTF8.GetByteCount(pendingOperationsJson);

                // This would require additional cache size calculation
                info.FavoritesCacheSizeBytes = 0; // Placeholder
                info.TotalSizeBytes = info.PendingOperationsSizeBytes + info.FavoritesCacheSizeBytes;

                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get storage info");
                return new OfflineStorageInfo();
            }
        }

        #region Private Methods

        private async Task<bool> ExecuteSyncOperationAsync(OfflineSyncOperation operation)
        {
            return operation.Type switch
            {
                OfflineSyncOperationType.AddFavorite => await _favoriteSpotService.AddToFavoritesAsync(
                    operation.UserId,
                    operation.SpotId,
                    operation.GetParameter("priority", 5),
                    operation.GetParameter<string>("notes"),
                    operation.GetParameter("notificationEnabled", true)),

                OfflineSyncOperationType.RemoveFavorite => await _favoriteSpotService.RemoveFromFavoritesAsync(
                    operation.UserId,
                    operation.SpotId),

                OfflineSyncOperationType.UpdateFavoritePriority => await _favoriteSpotService.UpdateFavoritePriorityAsync(
                    operation.UserId,
                    operation.SpotId,
                    operation.GetParameter("priority", 5)),

                OfflineSyncOperationType.UpdateFavoriteNotes => await _favoriteSpotService.UpdateFavoriteNotesAsync(
                    operation.UserId,
                    operation.SpotId,
                    operation.GetParameter<string>("notes")),

                OfflineSyncOperationType.UpdateFavoriteNotification => await _favoriteSpotService.UpdateFavoriteNotificationAsync(
                    operation.UserId,
                    operation.SpotId,
                    operation.GetParameter("enabled", true)),

                _ => false
            };
        }

        private async Task AddPendingOperationAsync(OfflineSyncOperation operation)
        {
            var previousCount = _pendingOperations.Count;
            _pendingOperations.Add(operation);
            await SavePendingOperationsAsync();
            OnPendingOperationsChanged(_pendingOperations.Count, previousCount, operation.Type);
        }

        private async Task SavePendingOperationsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_pendingOperations);
                _settingsService.Set(PendingOperationsKey, json);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save pending operations");
            }
        }

        private void LoadPendingOperations()
        {
            try
            {
                var json = _settingsService.Get(PendingOperationsKey, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var operations = JsonSerializer.Deserialize<List<OfflineSyncOperation>>(json);
                    if (operations != null)
                    {
                        _pendingOperations.AddRange(operations);
                        _logger.LogInformation("Loaded {Count} pending operations from storage", _pendingOperations.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load pending operations");
            }
        }

        private async void OnNetworkStatusChanged(object? sender, NetworkHealthChangedEventArgs e)
        {
            if (e.CurrentStatus.IsConnected && _pendingOperations.Any())
            {
                _logger.LogInformation("Network reconnected - auto-syncing {Count} pending operations", _pendingOperations.Count);
                _ = Task.Run(async () => await SyncPendingOperationsAsync());
            }
        }

        private void OnOfflineModeChanged(bool isOfflineMode, string reason)
        {
            OfflineModeChanged?.Invoke(this, new FavoriteOfflineModeChangedEventArgs
            {
                IsOfflineMode = isOfflineMode,
                Reason = reason
            });
        }

        private void OnSyncCompleted(OfflineSyncResult result, bool wasTriggeredByUser)
        {
            SyncCompleted?.Invoke(this, new OfflineSyncCompletedEventArgs
            {
                Result = result,
                WasTriggeredByUser = wasTriggeredByUser
            });
        }

        private void OnPendingOperationsChanged(int currentCount, int previousCount = 0, OfflineSyncOperationType? lastOperationType = null)
        {
            PendingOperationsChanged?.Invoke(this, new PendingOperationsChangedEventArgs
            {
                PendingCount = currentCount,
                PreviousCount = previousCount,
                LastOperationType = lastOperationType
            });
        }

        #endregion
    }
}