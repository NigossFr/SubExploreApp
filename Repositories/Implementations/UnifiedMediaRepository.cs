using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;

namespace SubExplore.Repositories.Implementations
{
    public class UnifiedMediaRepository : GenericRepository<UnifiedMedia>, IUnifiedMediaRepository
    {
        public UnifiedMediaRepository(SubExploreDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<UnifiedMedia>> GetByEntityAsync(EntityType entityType, int entityId)
        {
            return await _context.UnifiedMedia
                .Where(m => m.EntityType == entityType && m.EntityId == entityId)
                .OrderBy(m => m.DisplayOrder)
                .ThenByDescending(m => m.IsPrimary)
                .ToListAsync();
        }

        public async Task<IEnumerable<UnifiedMedia>> GetByEntityTypeAsync(EntityType entityType)
        {
            return await _context.UnifiedMedia
                .Where(m => m.EntityType == entityType)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<UnifiedMedia?> GetPrimaryMediaAsync(EntityType entityType, int entityId)
        {
            return await _context.UnifiedMedia
                .FirstOrDefaultAsync(m => m.EntityType == entityType && 
                                         m.EntityId == entityId && 
                                         m.IsPrimary);
        }

        public async Task<bool> SetPrimaryMediaAsync(int mediaId, EntityType entityType, int entityId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Reset all primary flags for this entity
                var existingMedia = await _context.UnifiedMedia
                    .Where(m => m.EntityType == entityType && m.EntityId == entityId)
                    .ToListAsync();

                foreach (var media in existingMedia)
                {
                    media.IsPrimary = false;
                }

                // Set the new primary media
                var targetMedia = await _context.UnifiedMedia
                    .FirstOrDefaultAsync(m => m.Id == mediaId && 
                                             m.EntityType == entityType && 
                                             m.EntityId == entityId);

                if (targetMedia == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                targetMedia.IsPrimary = true;
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}