using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace SubExplore.Services.Implementations
{
    public class UnifiedMediaService : IUnifiedMediaService
    {
        private readonly IUnifiedMediaRepository _mediaRepository;
        private readonly ILogger<UnifiedMediaService> _logger;

        public UnifiedMediaService(IUnifiedMediaRepository mediaRepository, ILogger<UnifiedMediaService> logger)
        {
            _mediaRepository = mediaRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<UnifiedMedia>> GetEntityMediaAsync(EntityType entityType, int entityId)
        {
            try
            {
                return await _mediaRepository.GetByEntityAsync(entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media for entity {EntityType}:{EntityId}", entityType, entityId);
                throw;
            }
        }

        public async Task<UnifiedMedia?> GetPrimaryMediaAsync(EntityType entityType, int entityId)
        {
            try
            {
                return await _mediaRepository.GetPrimaryMediaAsync(entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting primary media for entity {EntityType}:{EntityId}", entityType, entityId);
                throw;
            }
        }

        public async Task<UnifiedMedia> AddMediaAsync(UnifiedMedia media)
        {
            try
            {
                media.CreatedAt = DateTime.UtcNow;

                await _mediaRepository.AddAsync(media);
                await _mediaRepository.SaveChangesAsync();

                return media;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding media for entity {EntityType}:{EntityId}", media.EntityType, media.EntityId);
                throw;
            }
        }

        public async Task<bool> SetPrimaryMediaAsync(int mediaId, EntityType entityType, int entityId)
        {
            try
            {
                return await _mediaRepository.SetPrimaryMediaAsync(mediaId, entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary media {MediaId} for entity {EntityType}:{EntityId}", 
                    mediaId, entityType, entityId);
                throw;
            }
        }

        public async Task<bool> DeleteMediaAsync(int mediaId)
        {
            try
            {
                var media = await _mediaRepository.GetByIdAsync(mediaId);
                if (media == null)
                    return false;

                await _mediaRepository.DeleteAsync(media);
                await _mediaRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting media {MediaId}", mediaId);
                throw;
            }
        }

        public async Task<bool> UpdateMediaOrderAsync(EntityType entityType, int entityId, Dictionary<int, int> mediaOrderMap)
        {
            try
            {
                var entityMedia = await _mediaRepository.GetByEntityAsync(entityType, entityId);
                
                foreach (var media in entityMedia)
                {
                    if (mediaOrderMap.TryGetValue(media.Id, out int newOrder))
                    {
                        media.DisplayOrder = newOrder;
                        await _mediaRepository.UpdateAsync(media);
                    }
                }

                await _mediaRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating media order for entity {EntityType}:{EntityId}", entityType, entityId);
                return false;
            }
        }
    }
}