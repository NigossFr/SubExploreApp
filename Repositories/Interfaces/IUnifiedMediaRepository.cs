using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Repositories.Interfaces
{
    public interface IUnifiedMediaRepository : IGenericRepository<UnifiedMedia>
    {
        Task<IEnumerable<UnifiedMedia>> GetByEntityAsync(EntityType entityType, int entityId);
        Task<IEnumerable<UnifiedMedia>> GetByEntityTypeAsync(EntityType entityType);
        Task<UnifiedMedia?> GetPrimaryMediaAsync(EntityType entityType, int entityId);
        Task<bool> SetPrimaryMediaAsync(int mediaId, EntityType entityType, int entityId);
    }
}