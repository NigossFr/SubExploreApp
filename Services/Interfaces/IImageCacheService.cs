using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    public interface IImageCacheService
    {
        Task<string> GetCachedImagePathAsync(string imageUrl);
        Task<bool> IsCachedAsync(string imageUrl);
        Task CacheImageAsync(string imageUrl, string localPath);
        Task ClearCacheAsync();
        Task<long> GetCacheSizeAsync();
        Task<bool> RemoveFromCacheAsync(string imageUrl);
        Task<IEnumerable<string>> GetCachedImagesAsync();
        Task CleanupExpiredCacheAsync(TimeSpan maxAge);
    }
}