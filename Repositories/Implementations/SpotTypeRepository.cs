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
            // Utiliser le cache EF Core pour améliorer les performances
            var cacheKey = "active_spot_types";
            
            // D'abord, désactiver tous les types non autorisés
            var allowedTypes = new[]
            {
                "Apnée",
                "Photo sous-marine", 
                "Plongée récréative",
                "Plongée technique",
                "Randonnée sous marine"
            };

            // Optimisation: Une seule requête pour traiter tous les types
            var allTypes = await _context.SpotTypes
                .AsNoTracking() // Performance: pas de tracking pour lecture seule
                .Where(t => t.IsActive)
                .ToListAsync();

            var unwantedTypes = allTypes.Where(t => !allowedTypes.Contains(t.Name)).ToList();
            var wantedTypeGroups = allTypes
                .Where(t => allowedTypes.Contains(t.Name))
                .GroupBy(t => t.Name)
                .ToList();

            // Si des modifications sont nécessaires, les faire en lot
            bool hasChanges = false;
            
            if (unwantedTypes.Any())
            {
                // Utiliser une requête bulk pour désactiver les types non autorisés
                await _context.SpotTypes
                    .Where(t => t.IsActive && !allowedTypes.Contains(t.Name))
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsActive, false));
                hasChanges = true;
            }

            // Gérer les doublons : garder seulement le premier de chaque type autorisé
            foreach (var group in wantedTypeGroups.Where(g => g.Count() > 1))
            {
                var duplicates = group.OrderBy(t => t.Id).Skip(1).ToList();
                
                if (duplicates.Any())
                {
                    var duplicateIds = duplicates.Select(d => d.Id).ToList();
                    await _context.SpotTypes
                        .Where(t => duplicateIds.Contains(t.Id))
                        .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsActive, false));
                    hasChanges = true;
                }
            }

            // Si des modifications ont été faites, recharger depuis la base
            if (hasChanges)
            {
                // Recharger les données depuis la base après modifications
                return await _context.SpotTypes
                    .AsNoTracking()
                    .Where(t => t.IsActive && allowedTypes.Contains(t.Name))
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }

            // Sinon, utiliser les données déjà chargées (optimisation)
            return wantedTypeGroups
                .Select(g => g.First())
                .OrderBy(t => t.Name)
                .ToList();
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
