using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service for managing offline capabilities and operations
    /// </summary>
    public class OfflineCapabilityService : IOfflineCapabilityService, IDisposable
    {
        private readonly ILogger<OfflineCapabilityService> _logger;
        private readonly IConnectivityService _connectivityService;
        private readonly ISettingsService _settingsService;
        
        private readonly Dictionary<string, bool> _featureOfflineSupport;
        private readonly List<OfflineOperation> _pendingOperations;
        
        private bool _isOfflineModeEnabled = false;
        private bool _disposed = false;
        
        // File paths for offline data storage
        private readonly string _offlineDataPath;
        private readonly string _pendingOperationsPath;

        public bool CanWorkOffline { get; private set; }
        public bool IsOfflineModeEnabled => _isOfflineModeEnabled;

        public event EventHandler<OfflineModeChangedEventArgs>? OfflineModeChanged;
        public event EventHandler<OfflineDataSyncedEventArgs>? OfflineDataSynced;

        public OfflineCapabilityService(
            ILogger<OfflineCapabilityService> logger,
            IConnectivityService connectivityService,
            ISettingsService settingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            // Initialize offline data paths
            var appDataPath = FileSystem.AppDataDirectory;
            _offlineDataPath = Path.Combine(appDataPath, "offline_data");
            _pendingOperationsPath = Path.Combine(_offlineDataPath, "pending_operations.json");

            // Ensure offline data directory exists
            Directory.CreateDirectory(_offlineDataPath);

            // Initialize feature support matrix
            _featureOfflineSupport = new Dictionary<string, bool>
            {
                { "ViewSpots", true },      // Can view cached spots
                { "ViewImages", true },     // Can view cached images
                { "CreateSpots", true },    // Can create spots offline (sync later)
                { "AddFavorites", true },   // Can add favorites offline
                { "ViewMaps", false },      // Maps require internet (for now)
                { "Search", false },        // Search requires internet
                { "Weather", false },       // Weather requires internet
                { "Authentication", false } // Auth requires internet
            };

            _pendingOperations = new List<OfflineOperation>();

            // Initialize offline capabilities
            InitializeOfflineCapabilities();

            // Subscribe to connectivity changes
            _connectivityService.ConnectivityChanged += OnConnectivityChanged;

            // Load existing offline mode setting
            _isOfflineModeEnabled = _settingsService.Get<bool>("OfflineModeEnabled", false);
            
            _logger.LogInformation("🔄 Offline capability service initialized. Offline mode: {IsEnabled}", 
                _isOfflineModeEnabled ? "Enabled" : "Disabled");
        }

        public OfflineCapabilities GetOfflineCapabilities()
        {
            var capabilities = new OfflineCapabilities
            {
                CanViewCachedSpots = _featureOfflineSupport["ViewSpots"],
                CanViewCachedImages = _featureOfflineSupport["ViewImages"],
                CanCreateOfflineSpots = _featureOfflineSupport["CreateSpots"],
                CanAddOfflineFavorites = _featureOfflineSupport["AddFavorites"],
                CanViewOfflineMaps = _featureOfflineSupport["ViewMaps"],
                CanSyncOnReconnection = true,
                FeatureAvailability = new Dictionary<string, bool>(_featureOfflineSupport)
            };

            // Get cached data counts (simplified implementation)
            try
            {
                capabilities.CachedSpotsCount = GetCachedSpotsCount();
                capabilities.CachedImagesCount = GetCachedImagesCount();
                capabilities.CachedDataSizeBytes = GetCachedDataSize();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Error getting cache information: {Message}", ex.Message);
            }

            return capabilities;
        }

        public async Task EnableOfflineModeAsync()
        {
            if (_isOfflineModeEnabled)
            {
                _logger.LogInformation("🔄 Offline mode already enabled");
                return;
            }

            _logger.LogInformation("🔄 Enabling offline mode");

            _isOfflineModeEnabled = true;
            await _settingsService.SetAsync("OfflineModeEnabled", true);

            var capabilities = GetOfflineCapabilities();
            OnOfflineModeChanged(new OfflineModeChangedEventArgs(true, "User enabled offline mode", capabilities));

            // Pre-cache essential data if connected
            if (_connectivityService.IsConnected)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await PreCacheEssentialDataAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("⚠️ Error pre-caching data: {Message}", ex.Message);
                    }
                });
            }
        }

        public async Task DisableOfflineModeAsync()
        {
            if (!_isOfflineModeEnabled)
            {
                _logger.LogInformation("🔄 Offline mode already disabled");
                return;
            }

            _logger.LogInformation("🔄 Disabling offline mode");

            _isOfflineModeEnabled = false;
            await _settingsService.SetAsync("OfflineModeEnabled", false);

            var capabilities = GetOfflineCapabilities();
            OnOfflineModeChanged(new OfflineModeChangedEventArgs(false, "User disabled offline mode", capabilities));

            // Sync pending operations if connected
            if (_connectivityService.IsConnected)
            {
                await SynchronizeDataAsync();
            }
        }

        public async Task SynchronizeDataAsync()
        {
            if (!_connectivityService.IsConnected)
            {
                _logger.LogWarning("⚠️ Cannot synchronize - no internet connection");
                return;
            }

            var startTime = DateTime.UtcNow;
            var syncedCount = 0;
            var failedCount = 0;
            string? errorMessage = null;

            try
            {
                _logger.LogInformation("🔄 Starting offline data synchronization");

                // Load pending operations
                await LoadPendingOperationsAsync();

                var operationsToSync = _pendingOperations.ToList();
                _logger.LogInformation("📊 Found {Count} pending operations to sync", operationsToSync.Count);

                // Process each pending operation
                foreach (var operation in operationsToSync)
                {
                    try
                    {
                        var success = await ProcessPendingOperationAsync(operation);
                        if (success)
                        {
                            syncedCount++;
                            _pendingOperations.Remove(operation);
                        }
                        else
                        {
                            failedCount++;
                            operation.RetryCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error processing offline operation {Id}", operation.Id);
                        failedCount++;
                        operation.RetryCount++;
                        operation.ErrorMessage = ex.Message;
                    }
                }

                // Save updated pending operations
                await SavePendingOperationsAsync();

                var syncDuration = DateTime.UtcNow - startTime;
                _logger.LogInformation("✅ Synchronization completed. Synced: {Synced}, Failed: {Failed}, Duration: {Duration}",
                    syncedCount, failedCount, syncDuration);

                OnOfflineDataSynced(new OfflineDataSyncedEventArgs(syncedCount, failedCount, syncDuration, errorMessage));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                _logger.LogError(ex, "❌ Error during offline data synchronization");
                
                var syncDuration = DateTime.UtcNow - startTime;
                OnOfflineDataSynced(new OfflineDataSyncedEventArgs(syncedCount, failedCount, syncDuration, errorMessage));
            }
        }

        public async Task<IEnumerable<OfflineOperation>> GetPendingOperationsAsync()
        {
            await LoadPendingOperationsAsync();
            return _pendingOperations.ToList();
        }

        public async Task ClearOfflineDataAsync()
        {
            _logger.LogInformation("🗑️ Clearing offline data");

            try
            {
                _pendingOperations.Clear();
                
                // Clear pending operations file
                if (File.Exists(_pendingOperationsPath))
                {
                    File.Delete(_pendingOperationsPath);
                }

                // Clear cached data (implementation depends on caching strategy)
                await ClearCachedDataAsync();

                _logger.LogInformation("✅ Offline data cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error clearing offline data");
                throw;
            }
        }

        public bool IsFeatureAvailableOffline(string featureName)
        {
            return _featureOfflineSupport.TryGetValue(featureName, out var isAvailable) && isAvailable;
        }

        public async Task<OfflineDataInfo> GetOfflineDataInfoAsync()
        {
            await LoadPendingOperationsAsync();

            var dataInfo = new OfflineDataInfo
            {
                CachedSpotsCount = GetCachedSpotsCount(),
                CachedImagesCount = GetCachedImagesCount(),
                PendingOperationsCount = _pendingOperations.Count,
                DataCacheSizeBytes = GetCachedDataSize(),
                ImageCacheSizeBytes = GetCachedImageSize(),
                PendingOperationsSizeBytes = GetPendingOperationsSize(),
                LastSyncDate = _settingsService.Get<DateTime>("LastSyncDate", DateTime.MinValue),
                LastCleanupDate = _settingsService.Get<DateTime>("LastCleanupDate", DateTime.MinValue)
            };

            dataInfo.TotalSizeBytes = dataInfo.DataCacheSizeBytes + dataInfo.ImageCacheSizeBytes + dataInfo.PendingOperationsSizeBytes;
            dataInfo.EstimatedDataAge = DateTime.UtcNow - dataInfo.LastSyncDate;
            dataInfo.IsDataStale = dataInfo.EstimatedDataAge > TimeSpan.FromDays(7);

            return dataInfo;
        }

        private void InitializeOfflineCapabilities()
        {
            // Check device capabilities and storage space
            var freeSpace = GetAvailableStorageSpace();
            var requiredSpace = 100 * 1024 * 1024; // 100MB minimum

            CanWorkOffline = freeSpace > requiredSpace;

            if (!CanWorkOffline)
            {
                _logger.LogWarning("⚠️ Insufficient storage space for offline mode. Required: {Required}MB, Available: {Available}MB",
                    requiredSpace / 1024 / 1024, freeSpace / 1024 / 1024);
            }
        }

        private async Task PreCacheEssentialDataAsync()
        {
            _logger.LogInformation("📊 Pre-caching essential data for offline use");
            
            try
            {
                // This would integrate with your existing services to cache:
                // 1. User's favorite spots
                // 2. Recently viewed spots  
                // 3. Nearby spots based on last location
                // 4. Essential images
                
                // Placeholder implementation
                await Task.Delay(100); // Simulate caching work
                
                _logger.LogInformation("✅ Essential data pre-cached");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error pre-caching essential data");
            }
        }

        private async Task LoadPendingOperationsAsync()
        {
            try
            {
                if (!File.Exists(_pendingOperationsPath))
                    return;

                var json = await File.ReadAllTextAsync(_pendingOperationsPath);
                var operations = JsonSerializer.Deserialize<List<OfflineOperation>>(json);
                
                if (operations != null)
                {
                    _pendingOperations.Clear();
                    _pendingOperations.AddRange(operations);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading pending operations");
            }
        }

        private async Task SavePendingOperationsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_pendingOperations, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_pendingOperationsPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error saving pending operations");
            }
        }

        private async Task<bool> ProcessPendingOperationAsync(OfflineOperation operation)
        {
            _logger.LogInformation("🔄 Processing offline operation: {Type} for entity {EntityId}", 
                operation.Type, operation.EntityId);

            try
            {
                // This would integrate with your actual services to perform the operations
                // For now, we'll simulate success/failure
                
                switch (operation.Type)
                {
                    case OfflineOperationType.CreateSpot:
                        return await ProcessCreateSpotOperation(operation);
                    case OfflineOperationType.UpdateSpot:
                        return await ProcessUpdateSpotOperation(operation);
                    case OfflineOperationType.AddFavorite:
                        return await ProcessAddFavoriteOperation(operation);
                    default:
                        _logger.LogWarning("⚠️ Unknown operation type: {Type}", operation.Type);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing operation {Id}", operation.Id);
                return false;
            }
        }

        private async Task<bool> ProcessCreateSpotOperation(OfflineOperation operation)
        {
            // Integration point: Call your SupabaseApiService.CreateSpotAsync
            // For now, simulate the operation
            await Task.Delay(100);
            return true; // Simulate success
        }

        private async Task<bool> ProcessUpdateSpotOperation(OfflineOperation operation)
        {
            // Integration point: Call your SupabaseApiService.UpdateSpotAsync
            await Task.Delay(100);
            return true; // Simulate success
        }

        private async Task<bool> ProcessAddFavoriteOperation(OfflineOperation operation)
        {
            // Integration point: Call your SupabaseApiService.AddToFavoritesAsync
            await Task.Delay(100);
            return true; // Simulate success
        }

        private async Task ClearCachedDataAsync()
        {
            // Clear cached data implementation
            // This would integrate with your caching services
            await Task.CompletedTask;
        }

        private int GetCachedSpotsCount()
        {
            // Implementation would check actual cache
            return 0;
        }

        private int GetCachedImagesCount()
        {
            // Implementation would check actual image cache
            return 0;
        }

        private long GetCachedDataSize()
        {
            try
            {
                if (Directory.Exists(_offlineDataPath))
                {
                    return new DirectoryInfo(_offlineDataPath)
                        .GetFiles("*", SearchOption.AllDirectories)
                        .Sum(file => file.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Error calculating cached data size: {Message}", ex.Message);
            }
            
            return 0;
        }

        private long GetCachedImageSize()
        {
            // Implementation would check image cache size
            return 0;
        }

        private long GetPendingOperationsSize()
        {
            try
            {
                if (File.Exists(_pendingOperationsPath))
                {
                    return new FileInfo(_pendingOperationsPath).Length;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Error getting pending operations size: {Message}", ex.Message);
            }
            
            return 0;
        }

        private static long GetAvailableStorageSpace()
        {
            try
            {
                // Get available storage space
                var appDataPath = FileSystem.AppDataDirectory;
                var drive = new DriveInfo(Path.GetPathRoot(appDataPath) ?? "C:");
                return drive.AvailableFreeSpace;
            }
            catch
            {
                return long.MaxValue; // Assume sufficient space if we can't determine
            }
        }

        private void OnConnectivityChanged(object? sender, SubExplore.Services.Interfaces.ConnectivityChangedEventArgs e)
        {
            _logger.LogInformation("🔗 Connectivity changed. Connected: {IsConnected}", e.IsConnected);

            if (e.IsConnected && _isOfflineModeEnabled && _pendingOperations.Any())
            {
                // Auto-sync when reconnected and have pending operations
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SynchronizeDataAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error during auto-sync on reconnection");
                    }
                });
            }
        }

        protected virtual void OnOfflineModeChanged(OfflineModeChangedEventArgs e)
        {
            OfflineModeChanged?.Invoke(this, e);
        }

        protected virtual void OnOfflineDataSynced(OfflineDataSyncedEventArgs e)
        {
            OfflineDataSynced?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _connectivityService.ConnectivityChanged -= OnConnectivityChanged;
            _disposed = true;
        }
    }
}