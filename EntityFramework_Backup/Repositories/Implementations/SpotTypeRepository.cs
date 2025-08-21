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
            // NOUVELLE STRUCTURE HIÉRARCHIQUE : Retourner tous les types actifs (8 nouveaux types)
            // Plus besoin de filtrer, la migration a créé la bonne structure
            System.Diagnostics.Debug.WriteLine("[DEBUG] SpotTypeRepository: Loading all active spot types (hierarchical structure)");
            
            var activeTypes = await _context.SpotTypes
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();

            System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotTypeRepository: Found {activeTypes.Count} active types:");
            foreach (var type in activeTypes)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG]   - {type.Name} ({type.ColorCode}) - Category: {type.Category}");
            }

            return activeTypes;
        }

        public async Task<IEnumerable<SpotType>> GetByActivityCategoryAsync(ActivityCategory category)
        {
            return await _context.SpotTypes
                .AsNoTracking()
                .Where(t => t.Category == category && t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Alias pour GetActiveTypesAsync avec optimisations performances
        /// </summary>
        public async Task<IEnumerable<SpotType>> GetActiveSpotTypesAsync()
        {
            return await GetActiveTypesAsync();
        }
    }
}
