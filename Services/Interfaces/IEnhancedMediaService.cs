using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    public interface IEnhancedMediaService
    {
        /// <summary>
        /// Add new photo to spot with automatic processing
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <param name="imageStream">Image stream</param>
        /// <param name="fileName">Original filename</param>
        /// <param name="caption">Optional caption</param>
        /// <param name="isPrimary">Set as primary photo</param>
        /// <returns>SpotMedia if successful</returns>
        Task<SpotMedia?> AddSpotPhotoAsync(Guid spotId, Stream imageStream, string fileName, 
            string? caption = null, bool isPrimary = false);

        /// <summary>
        /// Update existing photo metadata
        /// </summary>
        /// <param name="mediaId">Media ID</param>
        /// <param name="caption">New caption</param>
        /// <param name="isPrimary">Set as primary</param>
        /// <returns>True if successful</returns>
        Task<bool> UpdatePhotoMetadataAsync(Guid mediaId, string? caption, bool? isPrimary = null);

        /// <summary>
        /// Delete photo from spot
        /// </summary>
        /// <param name="mediaId">Media ID</param>
        /// <returns>True if successful</returns>
        Task<bool> DeletePhotoAsync(Guid mediaId);

        /// <summary>
        /// Reorder photos for a spot
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <param name="orderedMediaIds">Media IDs in new order</param>
        /// <returns>True if successful</returns>
        Task<bool> ReorderPhotosAsync(Guid spotId, List<Guid> orderedMediaIds);

        /// <summary>
        /// Set photo as primary for spot
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <param name="mediaId">Media ID to set as primary</param>
        /// <returns>True if successful</returns>
        Task<bool> SetPrimaryPhotoAsync(Guid spotId, Guid mediaId);

        /// <summary>
        /// Get all photos for a spot with metadata
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <returns>List of spot media</returns>
        Task<List<SpotMedia>> GetSpotPhotosAsync(Guid spotId);

        /// <summary>
        /// Compress and optimize image
        /// </summary>
        /// <param name="imageStream">Original image stream</param>
        /// <param name="maxWidth">Maximum width</param>
        /// <param name="maxHeight">Maximum height</param>
        /// <param name="quality">JPEG quality (0-100)</param>
        /// <returns>Optimized image stream</returns>
        Task<Stream> OptimizeImageAsync(Stream imageStream, int maxWidth = 1920, int maxHeight = 1080, int quality = 85);

        /// <summary>
        /// Generate thumbnail from image
        /// </summary>
        /// <param name="imageStream">Original image</param>
        /// <param name="thumbnailSize">Thumbnail size</param>
        /// <returns>Thumbnail stream</returns>
        Task<Stream> GenerateThumbnailAsync(Stream imageStream, int thumbnailSize = 200);

        /// <summary>
        /// Validate image before upload
        /// </summary>
        /// <param name="imageStream">Image to validate</param>
        /// <param name="fileName">Filename</param>
        /// <returns>Validation result</returns>
        Task<MediaValidationResult> ValidateImageAsync(Stream imageStream, string fileName);

        /// <summary>
        /// Get image metadata (EXIF data)
        /// </summary>
        /// <param name="imageStream">Image stream</param>
        /// <returns>Image metadata</returns>
        Task<ImageMetadata> ExtractImageMetadataAsync(Stream imageStream);

        /// <summary>
        /// Cache image locally for offline viewing
        /// </summary>
        /// <param name="imageUrl">Remote image URL</param>
        /// <param name="cacheKey">Cache identifier</param>
        /// <returns>Local cache path</returns>
        Task<string?> CacheImageAsync(string imageUrl, string cacheKey);

        /// <summary>
        /// Clear cached images older than specified days
        /// </summary>
        /// <param name="olderThanDays">Days threshold</param>
        /// <returns>Number of files cleared</returns>
        Task<int> ClearImageCacheAsync(int olderThanDays = 30);

        /// <summary>
        /// Check if user can manage media for this spot
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <returns>True if user has permission</returns>
        Task<bool> CanUserManageMediaAsync(Guid spotId);

        /// <summary>
        /// Get total storage used by user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Storage size in bytes</returns>
        Task<long> GetUserStorageUsageAsync(Guid userId);
    }

    public class MediaValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public long FileSize { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }

    public class ImageMetadata
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime? DateTaken { get; set; }
        public string? CameraModel { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Dictionary<string, object> ExifData { get; set; } = new();
    }
}