using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace SubExplore.Migrations
{
    /// <summary>
    /// Migration to fix SpotType category mappings and add new enum values
    /// </summary>
    public class FixSpotTypeCategoryMapping
    {
        private readonly SubExploreDbContext _context;
        private readonly ILogger<FixSpotTypeCategoryMapping> _logger;

        public FixSpotTypeCategoryMapping(SubExploreDbContext context, ILogger<FixSpotTypeCategoryMapping> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Execute the migration to fix category mappings
        /// </summary>
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting SpotType category mapping migration...");

            try
            {
                // Get all spot types that need category updates based on their names
                var spotTypes = await _context.SpotTypes.ToListAsync();
                var updatedCount = 0;

                foreach (var spotType in spotTypes)
                {
                    var originalCategory = spotType.Category;
                    var newCategory = DetermineCorrectCategory(spotType.Name);

                    if (originalCategory != newCategory)
                    {
                        spotType.Category = newCategory;
                        spotType.UpdatedAt = DateTime.UtcNow;
                        updatedCount++;
                        
                        _logger.LogInformation(
                            "Updated SpotType '{Name}' category from {OldCategory} to {NewCategory}", 
                            spotType.Name, originalCategory, newCategory);
                    }
                }

                if (updatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully updated {Count} SpotType categories", updatedCount);
                }
                else
                {
                    _logger.LogInformation("No SpotType categories needed updates");
                }

                _logger.LogInformation("SpotType category mapping migration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute SpotType category mapping migration");
                throw;
            }
        }

        /// <summary>
        /// Determine the correct ActivityCategory based on SpotType name
        /// </summary>
        private ActivityCategory DetermineCorrectCategory(string spotTypeName)
        {
            if (string.IsNullOrEmpty(spotTypeName))
                return ActivityCategory.Other;

            return spotTypeName.ToLower() switch
            {
                // Diving activities
                "plongée bouteille" => ActivityCategory.Diving,
                "plongée technique" => ActivityCategory.Diving,
                "plongée profonde" => ActivityCategory.Diving,
                
                // Freediving activities
                "apnée" => ActivityCategory.Freediving,
                "apnée statique" => ActivityCategory.Freediving,
                "apnée dynamique" => ActivityCategory.Freediving,
                
                // Snorkeling activities
                "randonnée sous-marine" => ActivityCategory.Snorkeling,
                "snorkeling" => ActivityCategory.Snorkeling,
                
                // Photography activities
                "photo sous-marine" => ActivityCategory.UnderwaterPhotography,
                "photographie sous-marine" => ActivityCategory.UnderwaterPhotography,
                
                // Structures
                "clubs" => ActivityCategory.Structure,
                "club de plongée" => ActivityCategory.Structure,
                "professionnels" => ActivityCategory.Structure,
                "bases fédérales" => ActivityCategory.Structure,
                "centre de plongée" => ActivityCategory.Structure,
                "école de plongée" => ActivityCategory.Structure,
                
                // Shops
                "boutiques" => ActivityCategory.Shop,
                "magasin plongée" => ActivityCategory.Shop,
                "boutique plongée" => ActivityCategory.Shop,
                
                // Default
                _ => ActivityCategory.Other
            };
        }

        /// <summary>
        /// Rollback the migration (revert categories to Other if needed)
        /// </summary>
        public async Task RollbackAsync()
        {
            _logger.LogInformation("Rolling back SpotType category mapping migration...");

            try
            {
                var spotTypes = await _context.SpotTypes
                    .Where(st => st.Category == ActivityCategory.Structure || st.Category == ActivityCategory.Shop)
                    .ToListAsync();

                foreach (var spotType in spotTypes)
                {
                    spotType.Category = ActivityCategory.Other;
                    spotType.UpdatedAt = DateTime.UtcNow;
                }

                if (spotTypes.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Rolled back {Count} SpotType categories to Other", spotTypes.Count);
                }

                _logger.LogInformation("SpotType category mapping rollback completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rollback SpotType category mapping migration");
                throw;
            }
        }
    }
}