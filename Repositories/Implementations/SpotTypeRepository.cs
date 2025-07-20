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
            // D'abord, désactiver tous les types non autorisés
            var allowedTypes = new[]
            {
                "Apnée",
                "Photo sous-marine", 
                "Plongée récréative",
                "Plongée technique",
                "Randonnée sous marine"
            };

            // Désactiver les types non autorisés
            var unwantedTypes = await _context.SpotTypes
                .Where(t => t.IsActive && !allowedTypes.Contains(t.Name))
                .ToListAsync();

            foreach (var type in unwantedTypes)
            {
                type.IsActive = false;
            }

            // Gérer les doublons : garder seulement le premier de chaque type autorisé
            foreach (var typeName in allowedTypes)
            {
                var duplicates = await _context.SpotTypes
                    .Where(t => t.Name == typeName && t.IsActive)
                    .OrderBy(t => t.Id)
                    .ToListAsync();

                // Si il y a des doublons, désactiver tous sauf le premier
                if (duplicates.Count > 1)
                {
                    for (int i = 1; i < duplicates.Count; i++)
                    {
                        duplicates[i].IsActive = false;
                        unwantedTypes.Add(duplicates[i]);
                    }
                }
            }

            if (unwantedTypes.Any())
            {
                await _context.SaveChangesAsync();
            }

            // Retourner uniquement les types autorisés sans doublons
            return await _context.SpotTypes
                .Where(t => t.IsActive && allowedTypes.Contains(t.Name))
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
