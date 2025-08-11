using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Models.Enums;

namespace SubExplore.Migrations
{
    /// <summary>
    /// Migration pour mettre à jour la structure des catégories d'activité
    /// Corrige le problème architectural où les boutiques apparaissent dans les structures
    /// </summary>
    public class UpdateActivityCategoryStructure
    {
        private readonly SubExploreDbContext _context;
        private readonly ILogger<UpdateActivityCategoryStructure> _logger;

        public UpdateActivityCategoryStructure(SubExploreDbContext context, ILogger<UpdateActivityCategoryStructure> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Exécute la migration pour corriger la structure des catégories
        /// </summary>
        public async Task ExecuteAsync()
        {
            try
            {
                _logger.LogInformation("🚀 Début de la migration de la structure des catégories ActivityCategory...");

                // Étape 1: Corriger les catégories des activités existantes
                _logger.LogInformation("📝 Étape 1: Mise à jour des activités vers ActivityCategory.Activity (0)");
                var activitiesUpdated = await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE SpotTypes 
                    SET Category = 0 
                    WHERE Name IN ('Plongée bouteille', 'Apnée', 'Randonnée sous-marine', 'Photo sous-marine')
                      AND IsActive = 1
                ");
                _logger.LogInformation($"✅ {activitiesUpdated} types d'activité mis à jour");

                // Étape 2: Corriger les catégories des structures existantes
                _logger.LogInformation("🏗️ Étape 2: Mise à jour des structures vers ActivityCategory.Structure (1)");
                var structuresUpdated = await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE SpotTypes 
                    SET Category = 1 
                    WHERE Name IN ('Clubs', 'Professionnels', 'Bases fédérales')
                      AND IsActive = 1
                ");
                _logger.LogInformation($"✅ {structuresUpdated} types de structure mis à jour");

                // Étape 3: Corriger la catégorie des boutiques
                _logger.LogInformation("🛍️ Étape 3: Mise à jour des boutiques vers ActivityCategory.Shop (2)");
                var boutiquesUpdated = await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE SpotTypes 
                    SET Category = 2 
                    WHERE Name = 'Boutiques'
                      AND IsActive = 1
                ");
                _logger.LogInformation($"✅ {boutiquesUpdated} type boutique mis à jour");

                // Étape 4: Diagnostiquer et réparer les spots existants
                _logger.LogInformation("🔍 Étape 4: Diagnostic et réparation des spots existants");
                
                // Diagnostiquer l'état actuel
                var spotTypesStatus = await _context.SpotTypes
                    .Where(st => st.IsActive)
                    .Select(st => new { st.Id, st.Name, st.Category })
                    .ToListAsync();

                _logger.LogInformation($"Types de spots actifs trouvés : {string.Join(", ", spotTypesStatus.Select(s => $"{s.Name}({s.Id})->Cat{(int)s.Category}"))}");

                // Vérifier les spots et leurs types
                var spotsStatus = await _context.Spots
                    .Include(s => s.Type)
                    .Select(s => new { s.Id, s.Name, s.TypeId, TypeName = s.Type.Name })
                    .ToListAsync();

                _logger.LogInformation($"Spots existants : {string.Join(", ", spotsStatus.Select(s => $"{s.Name}->Type:{s.TypeName}({s.TypeId})"))}");
                
                // Compter les types par catégorie
                var categoryStats = await _context.SpotTypes
                    .Where(st => st.IsActive)
                    .GroupBy(st => st.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToListAsync();

                foreach (var stat in categoryStats)
                {
                    var categoryName = stat.Category switch
                    {
                        Models.Enums.ActivityCategory.Activity => "Activités",
                        Models.Enums.ActivityCategory.Structure => "Structures",
                        Models.Enums.ActivityCategory.Shop => "Boutiques",
                        _ => "Autres"
                    };
                    _logger.LogInformation($"Catégorie {categoryName}: {stat.Count} types");
                }

                _logger.LogInformation("=== MIGRATION STRUCTURELLE TERMINÉE ===");

                _logger.LogInformation("✅ Migration de la structure ActivityCategory terminée avec succès!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la migration de la structure ActivityCategory");
                throw;
            }
        }

        /// <summary>
        /// Obtient un rapport de l'état actuel de la base de données
        /// </summary>
        public async Task<string> GetStatusReportAsync()
        {
            try
            {
                var spotTypes = await _context.SpotTypes
                    .Where(st => st.IsActive)
                    .ToListAsync();

                var report = "=== RAPPORT DE LA STRUCTURE ACTUELLE ===\n";
                
                foreach (var group in spotTypes.GroupBy(st => st.Category))
                {
                    var categoryName = group.Key switch
                    {
                        ActivityCategory.Activity => "Activités",
                        ActivityCategory.Structure => "Structures", 
                        ActivityCategory.Shop => "Boutiques",
                        ActivityCategory.Other => "Autres",
                        _ => $"Catégorie {(int)group.Key}"
                    };
                    
                    report += $"\n{categoryName} ({(int)group.Key}):\n";
                    foreach (var type in group.OrderBy(t => t.Name))
                    {
                        report += $"  - {type.Name}\n";
                    }
                }
                
                report += "\n=== FIN DU RAPPORT ===";
                return report;
            }
            catch (Exception ex)
            {
                return $"❌ Erreur lors de la génération du rapport: {ex.Message}";
            }
        }
    }
}