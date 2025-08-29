using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    public class EnhancedMediaService : IEnhancedMediaService
    {
        private readonly IMediaService _baseMediaService;
        private readonly ISimpleAuthenticationService _authService;
        private readonly IDialogService _dialogService;
        private readonly string _cacheDirectory;
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private const int MAX_PHOTOS_PER_SPOT = 10;

        public EnhancedMediaService(
            IMediaService baseMediaService,
            ISimpleAuthenticationService authService,
            IDialogService dialogService)
        {
            _baseMediaService = baseMediaService;
            _authService = authService;
            _dialogService = dialogService;
            _cacheDirectory = Path.Combine(FileSystem.CacheDirectory, "images");
            Directory.CreateDirectory(_cacheDirectory);
        }

        public async Task<SpotMedia?> AddSpotPhotoAsync(Guid spotId, Stream imageStream, string fileName, 
            string? caption = null, bool isPrimary = false)
        {
            try
            {
                // Validate permissions
                if (!await CanUserManageMediaAsync(spotId))
                {
                    await _dialogService.ShowAlertAsync("Permission refusée", 
                        "Vous n'avez pas l'autorisation de gérer les médias de ce spot.", "OK");
                    return null;
                }

                // Validate image
                var validation = await ValidateImageAsync(imageStream, fileName);
                if (!validation.IsValid)
                {
                    var errors = string.Join("\n", validation.Errors);
                    await _dialogService.ShowAlertAsync("Image invalide", errors, "OK");
                    return null;
                }

                // Check photo count limit
                var existingPhotos = await GetSpotPhotosAsync(spotId);
                if (existingPhotos.Count >= MAX_PHOTOS_PER_SPOT)
                {
                    await _dialogService.ShowAlertAsync("Limite atteinte", 
                        $"Vous ne pouvez pas ajouter plus de {MAX_PHOTOS_PER_SPOT} photos par spot.", "OK");
                    return null;
                }

                // Optimize image
                imageStream.Position = 0;
                using var optimizedStream = await OptimizeImageAsync(imageStream);
                
                // Generate thumbnail
                optimizedStream.Position = 0;
                using var thumbnailStream = await GenerateThumbnailAsync(optimizedStream);

                // Extract metadata
                imageStream.Position = 0;
                var metadata = await ExtractImageMetadataAsync(imageStream);

                var mediaId = Guid.NewGuid();
                var spotMedia = new SpotMedia
                {
                    Id = mediaId,
                    SpotId = spotId,
                    MediaType = MediaType.Photo,
                    MediaUrl = $"spot_{spotId}/{mediaId}.jpg", // Placeholder URL
                    Caption = caption?.Trim(),
                    IsPrimary = isPrimary,
                    Width = metadata.Width,
                    Height = metadata.Height,
                    FileSize = validation.FileSize,
                    ContentType = validation.ContentType,
                    Status = MediaStatus.Processing,
                    CreatedAt = DateTime.UtcNow
                };

                // TODO: Upload to Supabase Storage
                System.Diagnostics.Debug.WriteLine($"[MEDIA] Adding photo to spot {spotId}");
                System.Diagnostics.Debug.WriteLine($"[MEDIA] - File: {fileName} ({validation.FileSize} bytes)");
                System.Diagnostics.Debug.WriteLine($"[MEDIA] - Dimensions: {metadata.Width}x{metadata.Height}");
                System.Diagnostics.Debug.WriteLine($"[MEDIA] - Caption: {caption}");
                System.Diagnostics.Debug.WriteLine($"[MEDIA] - Primary: {isPrimary}");

                // If setting as primary, unset other primary photos
                if (isPrimary)
                {
                    await SetPrimaryPhotoAsync(spotId, mediaId);
                }

                // TODO: Save to Supabase database
                // await _supabaseApiService.CreateSpotMediaAsync(spotMedia);

                return spotMedia;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] AddSpotPhotoAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible d'ajouter la photo: {ex.Message}", "OK");
                return null;
            }
        }

        public async Task<bool> UpdatePhotoMetadataAsync(Guid mediaId, string? caption, bool? isPrimary = null)
        {
            try
            {
                // TODO: Implement Supabase update
                System.Diagnostics.Debug.WriteLine($"[MEDIA] Updating photo metadata {mediaId}");
                System.Diagnostics.Debug.WriteLine($"[MEDIA] - Caption: {caption}");
                System.Diagnostics.Debug.WriteLine($"[MEDIA] - Primary: {isPrimary}");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] UpdatePhotoMetadataAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de mettre à jour la photo: {ex.Message}", "OK");
                return false;
            }
        }

        public async Task<bool> DeletePhotoAsync(Guid mediaId)
        {
            try
            {
                bool confirmed = await _dialogService.ShowConfirmationAsync(
                    "Supprimer la photo",
                    "Êtes-vous sûr de vouloir supprimer définitivement cette photo ?",
                    "Supprimer",
                    "Annuler");

                if (!confirmed)
                    return false;

                // TODO: Delete from Supabase Storage and database
                System.Diagnostics.Debug.WriteLine($"[MEDIA] Deleting photo {mediaId}");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] DeletePhotoAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de supprimer la photo: {ex.Message}", "OK");
                return false;
            }
        }

        public async Task<bool> ReorderPhotosAsync(Guid spotId, List<Guid> orderedMediaIds)
        {
            try
            {
                if (!await CanUserManageMediaAsync(spotId))
                {
                    await _dialogService.ShowAlertAsync("Permission refusée", 
                        "Vous n'avez pas l'autorisation de réorganiser les photos.", "OK");
                    return false;
                }

                // TODO: Implement photo ordering in database
                System.Diagnostics.Debug.WriteLine($"[MEDIA] Reordering photos for spot {spotId}");
                for (int i = 0; i < orderedMediaIds.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"[MEDIA] - Position {i + 1}: {orderedMediaIds[i]}");
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ReorderPhotosAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de réorganiser les photos: {ex.Message}", "OK");
                return false;
            }
        }

        public async Task<bool> SetPrimaryPhotoAsync(Guid spotId, Guid mediaId)
        {
            try
            {
                if (!await CanUserManageMediaAsync(spotId))
                    return false;

                // TODO: Implement primary photo update
                System.Diagnostics.Debug.WriteLine($"[MEDIA] Setting primary photo for spot {spotId}: {mediaId}");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SetPrimaryPhotoAsync failed: {ex.Message}");
                return false;
            }
        }

        public async Task<List<SpotMedia>> GetSpotPhotosAsync(Guid spotId)
        {
            try
            {
                // TODO: Implement Supabase query
                return new List<SpotMedia>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetSpotPhotosAsync failed: {ex.Message}");
                return new List<SpotMedia>();
            }
        }

        public async Task<Stream> OptimizeImageAsync(Stream imageStream, int maxWidth = 1920, int maxHeight = 1080, int quality = 85)
        {
            try
            {
                // TODO: Implement image optimization
                // For now, return the original stream
                System.Diagnostics.Debug.WriteLine($"[MEDIA] Optimizing image: max {maxWidth}x{maxHeight}, quality {quality}%");
                
                var outputStream = new MemoryStream();
                imageStream.Position = 0;
                await imageStream.CopyToAsync(outputStream);
                outputStream.Position = 0;
                
                return outputStream;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] OptimizeImageAsync failed: {ex.Message}");
                throw;
            }
        }

        public async Task<Stream> GenerateThumbnailAsync(Stream imageStream, int thumbnailSize = 200)
        {
            try
            {
                // TODO: Implement thumbnail generation
                System.Diagnostics.Debug.WriteLine($"[MEDIA] Generating thumbnail: {thumbnailSize}x{thumbnailSize}");
                
                var thumbnailStream = new MemoryStream();
                imageStream.Position = 0;
                await imageStream.CopyToAsync(thumbnailStream);
                thumbnailStream.Position = 0;
                
                return thumbnailStream;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GenerateThumbnailAsync failed: {ex.Message}");
                throw;
            }
        }

        public async Task<MediaValidationResult> ValidateImageAsync(Stream imageStream, string fileName)
        {
            var result = new MediaValidationResult { IsValid = true };

            try
            {
                // Get file size
                result.FileSize = imageStream.Length;

                // Validate file size
                if (result.FileSize > MAX_FILE_SIZE)
                {
                    result.Errors.Add($"Fichier trop volumineux (max {MAX_FILE_SIZE / 1024 / 1024}MB)");
                    result.IsValid = false;
                }

                if (result.FileSize == 0)
                {
                    result.Errors.Add("Fichier vide ou corrompu");
                    result.IsValid = false;
                }

                // Validate file extension
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                
                if (!allowedExtensions.Contains(extension))
                {
                    result.Errors.Add("Format de fichier non supporté (JPG, PNG, WebP uniquement)");
                    result.IsValid = false;
                }

                // Set content type
                result.ContentType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                };

                // TODO: Validate image dimensions and format
                result.Width = 1920; // Placeholder
                result.Height = 1080; // Placeholder

                System.Diagnostics.Debug.WriteLine($"[MEDIA] Validation result: {result.IsValid}");
                if (!result.IsValid)
                {
                    foreach (var error in result.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MEDIA] - Error: {error}");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ValidateImageAsync failed: {ex.Message}");
                result.Errors.Add("Erreur lors de la validation de l'image");
                result.IsValid = false;
                return result;
            }
        }

        public async Task<ImageMetadata> ExtractImageMetadataAsync(Stream imageStream)
        {
            try
            {
                // TODO: Implement EXIF metadata extraction
                var metadata = new ImageMetadata
                {
                    Width = 1920,
                    Height = 1080,
                    FileSize = imageStream.Length,
                    ContentType = "image/jpeg"
                };

                System.Diagnostics.Debug.WriteLine($"[MEDIA] Extracted metadata: {metadata.Width}x{metadata.Height}");
                
                return metadata;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ExtractImageMetadataAsync failed: {ex.Message}");
                return new ImageMetadata();
            }
        }

        public async Task<string?> CacheImageAsync(string imageUrl, string cacheKey)
        {
            try
            {
                var fileName = $"{cacheKey}.jpg";
                var filePath = Path.Combine(_cacheDirectory, fileName);
                
                if (File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[CACHE] Image already cached: {fileName}");
                    return filePath;
                }

                // TODO: Download and cache image
                System.Diagnostics.Debug.WriteLine($"[CACHE] Caching image: {imageUrl} -> {fileName}");
                
                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CacheImageAsync failed: {ex.Message}");
                return null;
            }
        }

        public async Task<int> ClearImageCacheAsync(int olderThanDays = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-olderThanDays);
                var files = Directory.GetFiles(_cacheDirectory);
                var deletedCount = 0;

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastAccessTime < cutoffDate)
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[CACHE] Cleared {deletedCount} cached images older than {olderThanDays} days");
                return deletedCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ClearImageCacheAsync failed: {ex.Message}");
                return 0;
            }
        }

        public async Task<bool> CanUserManageMediaAsync(Guid spotId)
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                    return false;

                // TODO: Check spot ownership or admin privileges
                System.Diagnostics.Debug.WriteLine($"[MEDIA] Checking media permissions for user {currentUser.Id} on spot {spotId}");
                return true; // For now, allow all authenticated users
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CanUserManageMediaAsync failed: {ex.Message}");
                return false;
            }
        }

        public async Task<long> GetUserStorageUsageAsync(Guid userId)
        {
            try
            {
                // TODO: Implement storage usage calculation
                return 0; // Placeholder
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetUserStorageUsageAsync failed: {ex.Message}");
                return 0;
            }
        }
    }
}