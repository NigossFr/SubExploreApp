using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    public class ImageCacheService : IImageCacheService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ImageCacheService> _logger;
        private readonly string _cacheDirectory;
        private readonly Dictionary<string, string> _memoryCache;
        private readonly SemaphoreSlim _cacheLock;

        // Configuration
        private const int MAX_CACHE_SIZE_MB = 100;
        private const int MAX_MEMORY_CACHE_SIZE = 50;
        private static readonly TimeSpan DEFAULT_CACHE_EXPIRY = TimeSpan.FromDays(7);

        public ImageCacheService(HttpClient httpClient, ILogger<ImageCacheService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _memoryCache = new Dictionary<string, string>();
            _cacheLock = new SemaphoreSlim(1, 1);

            // Initialize cache directory
            _cacheDirectory = Path.Combine(FileSystem.Current.CacheDirectory, "images");
            Directory.CreateDirectory(_cacheDirectory);
        }

        public async Task<string> GetCachedImagePathAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return null;

            try
            {
                await _cacheLock.WaitAsync();

                // Check memory cache first
                if (_memoryCache.TryGetValue(imageUrl, out string cachedPath))
                {
                    if (File.Exists(cachedPath))
                    {
                        // Update file access time
                        File.SetLastAccessTime(cachedPath, DateTime.Now);
                        return cachedPath;
                    }
                    else
                    {
                        // Remove from memory cache if file doesn't exist
                        _memoryCache.Remove(imageUrl);
                    }
                }

                // Generate cache file path
                string fileName = GenerateFileNameFromUrl(imageUrl);
                string filePath = Path.Combine(_cacheDirectory, fileName);

                // Check if file exists and is not expired
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    if (DateTime.Now - fileInfo.LastWriteTime < DEFAULT_CACHE_EXPIRY)
                    {
                        // Add to memory cache
                        AddToMemoryCache(imageUrl, filePath);
                        File.SetLastAccessTime(filePath, DateTime.Now);
                        return filePath;
                    }
                    else
                    {
                        // File is expired, delete it
                        File.Delete(filePath);
                    }
                }

                // Download and cache the image
                return await DownloadAndCacheImageAsync(imageUrl, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cached image for URL: {ImageUrl}", imageUrl);
                return null;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task<bool> IsCachedAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return false;

            try
            {
                await _cacheLock.WaitAsync();

                // Check memory cache first
                if (_memoryCache.TryGetValue(imageUrl, out string cachedPath))
                {
                    return File.Exists(cachedPath);
                }

                // Check file system
                string fileName = GenerateFileNameFromUrl(imageUrl);
                string filePath = Path.Combine(_cacheDirectory, fileName);

                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    if (DateTime.Now - fileInfo.LastWriteTime < DEFAULT_CACHE_EXPIRY)
                    {
                        return true;
                    }
                    else
                    {
                        // File is expired
                        File.Delete(filePath);
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if image is cached for URL: {ImageUrl}", imageUrl);
                return false;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task CacheImageAsync(string imageUrl, string localPath)
        {
            if (string.IsNullOrEmpty(imageUrl) || string.IsNullOrEmpty(localPath))
                return;

            try
            {
                await _cacheLock.WaitAsync();

                string fileName = GenerateFileNameFromUrl(imageUrl);
                string targetPath = Path.Combine(_cacheDirectory, fileName);

                // Copy local file to cache
                if (File.Exists(localPath))
                {
                    File.Copy(localPath, targetPath, true);
                    AddToMemoryCache(imageUrl, targetPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching image from local path: {LocalPath}", localPath);
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task ClearCacheAsync()
        {
            try
            {
                await _cacheLock.WaitAsync();

                // Clear memory cache
                _memoryCache.Clear();

                // Clear file cache
                if (Directory.Exists(_cacheDirectory))
                {
                    var files = Directory.GetFiles(_cacheDirectory);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error deleting cache file: {FilePath}", file);
                        }
                    }
                }

                _logger.LogInformation("Image cache cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing image cache");
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task<long> GetCacheSizeAsync()
        {
            try
            {
                await _cacheLock.WaitAsync();

                if (!Directory.Exists(_cacheDirectory))
                    return 0;

                var files = Directory.GetFiles(_cacheDirectory);
                long totalSize = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error getting file size for: {FilePath}", file);
                    }
                }

                return totalSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache size");
                return 0;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task<bool> RemoveFromCacheAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return false;

            try
            {
                await _cacheLock.WaitAsync();

                // Remove from memory cache
                _memoryCache.Remove(imageUrl);

                // Remove from file cache
                string fileName = GenerateFileNameFromUrl(imageUrl);
                string filePath = Path.Combine(_cacheDirectory, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing image from cache: {ImageUrl}", imageUrl);
                return false;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task<IEnumerable<string>> GetCachedImagesAsync()
        {
            try
            {
                await _cacheLock.WaitAsync();

                if (!Directory.Exists(_cacheDirectory))
                    return Enumerable.Empty<string>();

                var files = Directory.GetFiles(_cacheDirectory);
                return files.Select(f => Path.GetFileName(f));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cached images list");
                return Enumerable.Empty<string>();
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task CleanupExpiredCacheAsync(TimeSpan maxAge)
        {
            try
            {
                await _cacheLock.WaitAsync();

                if (!Directory.Exists(_cacheDirectory))
                    return;

                var files = Directory.GetFiles(_cacheDirectory);
                var cutoffTime = DateTime.Now - maxAge;
                int deletedCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.LastAccessTime < cutoffTime)
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting expired cache file: {FilePath}", file);
                    }
                }

                // Clean up memory cache
                var expiredUrls = _memoryCache.Where(kvp => !File.Exists(kvp.Value)).Select(kvp => kvp.Key).ToList();
                foreach (var url in expiredUrls)
                {
                    _memoryCache.Remove(url);
                }

                _logger.LogInformation("Cleaned up {DeletedCount} expired cache files", deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired cache");
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async Task<string> DownloadAndCacheImageAsync(string imageUrl, string filePath)
        {
            try
            {
                // Check cache size and clean up if necessary
                await EnsureCacheSizeAsync();

                // Download image
                using var response = await _httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();

                // Save to file
                using var fileStream = File.Create(filePath);
                await response.Content.CopyToAsync(fileStream);

                // Add to memory cache
                AddToMemoryCache(imageUrl, filePath);

                _logger.LogDebug("Successfully cached image: {ImageUrl}", imageUrl);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading and caching image: {ImageUrl}", imageUrl);
                
                // Clean up partial file if it exists
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch { }
                }

                return null;
            }
        }

        private async Task EnsureCacheSizeAsync()
        {
            var cacheSize = await GetCacheSizeAsync();
            var maxSizeBytes = MAX_CACHE_SIZE_MB * 1024 * 1024;

            if (cacheSize > maxSizeBytes)
            {
                // Clean up oldest files
                var files = Directory.GetFiles(_cacheDirectory)
                    .Select(f => new FileInfo(f))
                    .OrderBy(f => f.LastAccessTime)
                    .Take((int)(Directory.GetFiles(_cacheDirectory).Length * 0.3)) // Remove 30% of files
                    .ToList();

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file.FullName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting old cache file: {FilePath}", file.FullName);
                    }
                }
            }
        }

        private void AddToMemoryCache(string imageUrl, string filePath)
        {
            if (_memoryCache.Count >= MAX_MEMORY_CACHE_SIZE)
            {
                // Remove oldest entry
                var oldestKey = _memoryCache.Keys.First();
                _memoryCache.Remove(oldestKey);
            }

            _memoryCache[imageUrl] = filePath;
        }

        private string GenerateFileNameFromUrl(string imageUrl)
        {
            // Create a hash of the URL to generate a unique filename
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(imageUrl));
            var hashString = Convert.ToHexString(hash);

            // Try to get file extension from URL
            var uri = new Uri(imageUrl);
            var extension = Path.GetExtension(uri.LocalPath);
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".jpg"; // Default extension
            }

            return $"{hashString}{extension}";
        }
    }
}