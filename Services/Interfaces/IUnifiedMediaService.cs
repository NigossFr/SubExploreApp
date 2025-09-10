using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    public interface IUnifiedMediaService
    {
        Task<IEnumerable<UnifiedMedia>> GetEntityMediaAsync(EntityType entityType, int entityId);
        Task<UnifiedMedia?> GetPrimaryMediaAsync(EntityType entityType, int entityId);
        Task<UnifiedMedia> AddMediaAsync(UnifiedMedia media);
        Task<bool> SetPrimaryMediaAsync(int mediaId, EntityType entityType, int entityId);
        Task<bool> DeleteMediaAsync(int mediaId);
        Task<bool> UpdateMediaOrderAsync(EntityType entityType, int entityId, Dictionary<int, int> mediaOrderMap);
    }
}