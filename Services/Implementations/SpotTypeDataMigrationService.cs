using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service pour migrer les types de spots existants vers la nouvelle organisation
    /// </summary>
    public class SpotTypeDataMigrationService
    {
        private readonly SubExploreDbContext _context;
        private readonly ILogger<SpotTypeDataMigrationService> _logger;

        public SpotTypeDataMigrationService(SubExploreDbContext context, ILogger<SpotTypeDataMigrationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Migre les types de spots vers la nouvelle organisation
        /// </summary>
        public async Task MigrateSpotTypesAsync()
        {
            try
            {
                _logger.LogInformation("Début de la migration des types de spots...");

                // Vérifier s'il y a des types existants à migrer
                var existingTypes = await _context.SpotTypes.ToListAsync();
                
                _logger.LogInformation($"Types existants trouvés: {string.Join(", ", existingTypes.Select(t => $"{t.Name} (Active: {t.IsActive})"))}");
                
                // Toujours ajouter les nouveaux types s'ils n'existent pas
                await AddNewSpotTypesAsync();
                
                var needsMigration = false;

                // Marquer les anciens types comme inactifs
                foreach (var type in existingTypes)
                {
                    if (ShouldMigrateType(type.Name))
                    {
                        type.IsActive = false;
                        needsMigration = true;
                        _logger.LogInformation($"Désactivation de l'ancien type: {type.Name}");
                    }
                }

                if (needsMigration)
                {
                    // Sauvegarder les changements des anciens types
                    await _context.SaveChangesAsync();
                    
                    // Migrer les spots existants vers les nouveaux types
                    await MigrateExistingSpotsAsync();

                    _logger.LogInformation("Migration des spots existants terminée avec succès");
                }
                else
                {
                    _logger.LogInformation("Aucun ancien type à migrer, mais nouveaux types créés si nécessaire");
                }
                
                // Vérifier les types après migration
                var finalTypes = await _context.SpotTypes.Where(t => t.IsActive).ToListAsync();
                _logger.LogInformation($"Types actifs après migration: {string.Join(", ", finalTypes.Select(t => t.Name))}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la migration des types de spots");
                throw;
            }
        }

        private bool ShouldMigrateType(string typeName)
        {
            var oldTypes = new[] { "Plongée récréative", "Plongée technique" };
            return oldTypes.Contains(typeName);
        }

        private async Task AddNewSpotTypesAsync()
        {
            var newTypes = GetNewSpotTypes();
            
            foreach (var newType in newTypes)
            {
                var existingType = await _context.SpotTypes
                    .FirstOrDefaultAsync(st => st.Name == newType.Name && st.IsActive);

                if (existingType == null)
                {
                    _context.SpotTypes.Add(newType);
                    _logger.LogInformation($"Ajout du nouveau type: {newType.Name}");
                }
            }

            await _context.SaveChangesAsync();
        }

        private List<SpotType> GetNewSpotTypes()
        {
            return new List<SpotType>
            {
                // === ACTIVITÉS (variations de bleus) ===
                new SpotType
                {
                    Name = "Plongée bouteille",
                    IconPath = "marker_scuba.png", 
                    ColorCode = "#0077BE", // Bleu principal
                    Category = ActivityCategory.Activity,
                    Description = "Sites de plongée avec bouteille (tous niveaux - récréative et technique)",
                    RequiresExpertValidation = true,
                    ValidationCriteria = JsonSerializer.Serialize(new {
                        RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" },
                        MaxDepthRange = new[] { 0, 200 }
                    }),
                    IsActive = true
                },
                new SpotType
                {
                    Name = "Apnée",
                    IconPath = "marker_freediving.png",
                    ColorCode = "#4A90E2", // Bleu moyen
                    Category = ActivityCategory.Activity,
                    Description = "Sites adaptés à la plongée en apnée",
                    RequiresExpertValidation = true,
                    ValidationCriteria = JsonSerializer.Serialize(new {
                        RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" },
                        MaxDepthRange = new[] { 0, 30 }
                    }),
                    IsActive = true
                },
                new SpotType
                {
                    Name = "Randonnée sous-marine",
                    IconPath = "marker_snorkeling.png",
                    ColorCode = "#87CEEB", // Bleu clair
                    Category = ActivityCategory.Activity,
                    Description = "Sites de surface accessibles pour la randonnée sous-marine",
                    RequiresExpertValidation = false,
                    ValidationCriteria = JsonSerializer.Serialize(new {
                        RequiredFields = new[] { "DifficultyLevel", "SafetyNotes" },
                        MaxDepthRange = new[] { 0, 5 }
                    }),
                    IsActive = true
                },
                new SpotType
                {
                    Name = "Photo sous-marine",
                    IconPath = "marker_photography.png",
                    ColorCode = "#5DADE2", // Bleu photo
                    Category = ActivityCategory.Activity,
                    Description = "Sites d'intérêt pour la photographie sous-marine",
                    RequiresExpertValidation = false,
                    ValidationCriteria = JsonSerializer.Serialize(new {
                        RequiredFields = new[] { "DifficultyLevel" }
                    }),
                    IsActive = true
                },

                // === STRUCTURES (variations de verts) ===
                new SpotType
                {
                    Name = "Clubs",
                    IconPath = "marker_club.png",
                    ColorCode = "#228B22", // Vert foncé
                    Category = ActivityCategory.Structure,
                    Description = "Clubs de plongée et associations",
                    RequiresExpertValidation = false,
                    ValidationCriteria = JsonSerializer.Serialize(new {
                        RequiredFields = new[] { "Description" }
                    }),
                    IsActive = true
                },
                new SpotType
                {
                    Name = "Professionnels",
                    IconPath = "marker_pro.png",
                    ColorCode = "#32CD32", // Vert lime
                    Category = ActivityCategory.Structure,
                    Description = "Centres de plongée, instructeurs et guides professionnels",
                    RequiresExpertValidation = true,
                    ValidationCriteria = JsonSerializer.Serialize(new {
                        RequiredFields = new[] { "Description", "SafetyNotes" }
                    }),
                    IsActive = true
                },
                new SpotType
                {
                    Name = "Bases fédérales",
                    IconPath = "marker_federal.png",
                    ColorCode = "#90EE90", // Vert clair
                    Category = ActivityCategory.Structure,
                    Description = "Bases fédérales et structures officielles",
                    RequiresExpertValidation = true,
                    ValidationCriteria = JsonSerializer.Serialize(new {
                        RequiredFields = new[] { "Description", "SafetyNotes" }
                    }),
                    IsActive = true
                },

                // === BOUTIQUES (tons oranges) ===
                new SpotType
                {
                    Name = "Boutiques",
                    IconPath = "marker_shop.png",
                    ColorCode = "#FF8C00", // Orange principal
                    Category = ActivityCategory.Shop,
                    Description = "Magasins de matériel de plongée et équipements sous-marins",
                    RequiresExpertValidation = false,
                    ValidationCriteria = JsonSerializer.Serialize(new {
                        RequiredFields = new[] { "Description" }
                    }),
                    IsActive = true
                }
            };
        }

        private async Task MigrateExistingSpotsAsync()
        {
            // Récupérer les nouveaux types
            var newDivingType = await _context.SpotTypes
                .FirstOrDefaultAsync(st => st.Name == "Plongée bouteille" && st.IsActive);
            
            if (newDivingType == null)
            {
                _logger.LogError("Type 'Plongée bouteille' non trouvé pour la migration");
                return;
            }

            // Migrer les spots de plongée récréative et technique vers plongée bouteille
            var spotsToMigrate = await _context.Spots
                .Include(s => s.Type)
                .Where(s => s.Type.Name == "Plongée récréative" || s.Type.Name == "Plongée technique")
                .ToListAsync();

            foreach (var spot in spotsToMigrate)
            {
                spot.TypeId = newDivingType.Id;
                _logger.LogInformation($"Migration du spot '{spot.Name}' de '{spot.Type?.Name}' vers 'Plongée bouteille'");
            }

            if (spotsToMigrate.Count > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Migration de {spotsToMigrate.Count} spots terminée");
            }
        }
    }
}