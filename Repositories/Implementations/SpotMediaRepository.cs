using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;

namespace SubExplore.Repositories.Implementations
{
    public class SpotMediaRepository : GenericRepository<SpotMedia>, ISpotMediaRepository
    {
        public SpotMediaRepository(SubExploreDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SpotMedia>> GetBySpotIdAsync(int spotId)
        {
            return await _context.SpotMedia
                .Where(m => m.SpotId == spotId && m.Status == MediaStatus.Active)
                .OrderBy(m => !m.IsPrimary) // Tri pour avoir les images primaires en premier
                .ToListAsync();
        }

        public async Task<SpotMedia?> GetPrimaryMediaForSpotAsync(int spotId)
        {
            return await _context.SpotMedia
                .Where(m => m.SpotId == spotId && m.IsPrimary && m.Status == MediaStatus.Active)
                .FirstOrDefaultAsync();
        }
    }
}
