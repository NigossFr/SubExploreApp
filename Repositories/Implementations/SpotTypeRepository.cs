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
    public class SpotTypeRepository : GenericRepository<SpotType>, ISpotTypeRepository
    {
        public SpotTypeRepository(SubExploreDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SpotType>> GetActiveTypesAsync()
        {
            return await _context.SpotTypes
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<SpotType>> GetByActivityCategoryAsync(ActivityCategory category)
        {
            return await _context.SpotTypes
                .Where(t => t.Category == category && t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
    }
}
