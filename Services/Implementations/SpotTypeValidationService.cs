using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service for validating and ensuring SpotType data integrity
    /// Helps diagnose and fix issues with missing or inactive SpotTypes
    /// </summary>
    public class SpotTypeValidationService : ISpotTypeValidationService
    {
        private readonly SubExploreDbContext _context;
        private readonly ILogger<SpotTypeValidationService> _logger;
        
        // Expected SpotTypes for each category
        private static readonly Dictionary<ActivityCategory, List<SpotTypeTemplate>> ExpectedSpotTypes = new()
        {
            {
                ActivityCategory.Diving, new List<SpotTypeTemplate>
                {
                    new("Plongée bouteille", "marker_scuba.png", "#0077BE", true, "Sites de plongée avec bouteille")
                }
            },
            {
                ActivityCategory.Freediving, new List<SpotTypeTemplate>
                {
                    new("Apnée", "marker_freediving.png", "#4A90E2", true, "Sites adaptés à la plongée en apnée")
                }
            },
            {
                ActivityCategory.Snorkeling, new List<SpotTypeTemplate>
                {
                    new("Randonnée sous-marine", "marker_snorkeling.png", "#87CEEB", false, "Sites de randonnée sous-marine")
                }
            },
            {
                ActivityCategory.UnderwaterPhotography, new List<SpotTypeTemplate>
                {
                    new("Photo sous-marine", "marker_photography.png", "#5DADE2", false, "Sites pour la photographie sous-marine")
                }
            },
            {
                ActivityCategory.Other, new List<SpotTypeTemplate>
                {
                    new("Clubs", "marker_club.png", "#228B22", false, "Clubs de plongée"),
                    new("Professionnels", "marker_pro.png", "#32CD32", true, "Centres professionnels"),
                    new("Bases fédérales", "marker_federal.png", "#90EE90", true, "Structures officielles"),
                    new("Boutiques", "marker_shop.png", "#FF8C00", false, "Magasins d'équipement")
                }
            }
        };
        
        public SpotTypeValidationService(SubExploreDbContext context, ILogger<SpotTypeValidationService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<SpotTypeValidationResult> ValidateSpotTypesAsync()
        {
            try
            {
                _logger.LogInformation("Starting SpotType validation");
                
                var result = new SpotTypeValidationResult();
                var allTypes = await _context.SpotTypes.AsNoTracking().ToListAsync();
                
                result.TotalActiveTypes = allTypes.Count(t => t.IsActive);
                result.TotalInactiveTypes = allTypes.Count(t => !t.IsActive);
                
                // Check each category
                foreach (var category in Enum.GetValues<ActivityCategory>())
                {
                    var categoryTypes = allTypes.Where(t => t.Category == category && t.IsActive).ToList();
                    result.CategoryCounts[category] = categoryTypes.Count;
                    
                    if (!categoryTypes.Any())
                    {
                        result.Issues.Add($"No active SpotTypes found for category {category}");
                        result.Recommendations.Add($"Create or activate SpotTypes for {category}");
                    }
                    else
                    {
                        _logger.LogDebug("Category {Category} has {Count} active SpotTypes: {Types}",
                            category, categoryTypes.Count, string.Join(", ", categoryTypes.Select(t => t.Name)));
                    }
                }
                
                // Check for orphaned spots
                var orphanedSpots = await _context.Spots
                    .AsNoTracking()
                    .Where(s => s.Type == null || !s.Type.IsActive)
                    .CountAsync();
                    
                if (orphanedSpots > 0)
                {
                    result.Issues.Add($"{orphanedSpots} spots have inactive or missing SpotTypes");
                    result.Recommendations.Add("Run repair to fix orphaned spots");
                }
                
                result.IsValid = !result.Issues.Any();
                
                _logger.LogInformation("SpotType validation completed. Valid: {IsValid}, Issues: {IssueCount}", 
                    result.IsValid, result.Issues.Count);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SpotType validation");
                return new SpotTypeValidationResult
                {
                    IsValid = false,
                    Issues = { $"Validation failed: {ex.Message}" }
                };
            }
        }
        
        public async Task<bool> EnsureCategorySpotTypesAsync()
        {
            try
            {
                var allCategories = Enum.GetValues<ActivityCategory>();
                var activeTypeCounts = await GetActiveTypeCountsByCategoryAsync();
                
                foreach (var category in allCategories)
                {
                    if (!activeTypeCounts.ContainsKey(category) || activeTypeCounts[category] == 0)
                    {
                        _logger.LogWarning("Category {Category} has no active SpotTypes", category);
                        return false;
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring category SpotTypes");
                return false;
            }
        }
        
        public async Task<SpotTypeDiagnostics> GetDiagnosticsAsync()
        {
            try
            {
                var diagnostics = new SpotTypeDiagnostics
                {
                    DiagnosticTime = DateTime.UtcNow
                };
                
                var allTypes = await _context.SpotTypes.AsNoTracking().ToListAsync();
                
                diagnostics.TotalSpotTypes = allTypes.Count;
                diagnostics.ActiveSpotTypes = allTypes.Count(t => t.IsActive);
                diagnostics.InactiveSpotTypes = allTypes.Count(t => !t.IsActive);
                
                // Group by category
                foreach (var category in Enum.GetValues<ActivityCategory>())
                {
                    var categoryTypes = allTypes.Where(t => t.Category == category).ToList();
                    diagnostics.TypesByCategory[category] = categoryTypes;
                    
                    if (!categoryTypes.Any(t => t.IsActive))
                    {
                        diagnostics.MissingCategories.Add(category);
                        diagnostics.IssuesFound.Add($"No active SpotTypes for category {category}");
                    }
                }
                
                // Check for spots with missing types
                var spotsWithMissingTypes = await _context.Spots
                    .AsNoTracking()
                    .Where(s => s.Type == null || !s.Type.IsActive)
                    .CountAsync();
                    
                if (spotsWithMissingTypes > 0)
                {
                    diagnostics.IssuesFound.Add($"{spotsWithMissingTypes} spots have inactive or missing SpotTypes");
                }
                
                _logger.LogInformation("Diagnostics completed: {ActiveTypes}/{TotalTypes} active, {IssueCount} issues found",
                    diagnostics.ActiveSpotTypes, diagnostics.TotalSpotTypes, diagnostics.IssuesFound.Count);
                
                return diagnostics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SpotType diagnostics");
                return new SpotTypeDiagnostics
                {
                    DiagnosticTime = DateTime.UtcNow,
                    IssuesFound = { $"Diagnostic failed: {ex.Message}" }
                };
            }
        }
        
        public async Task<SpotTypeRepairResult> RepairSpotTypesAsync()
        {
            var result = new SpotTypeRepairResult();
            
            try
            {
                _logger.LogInformation("Starting SpotType repair");
                
                // 1. Activate inactive expected types
                var inactiveExpectedTypes = await _context.SpotTypes
                    .Where(t => !t.IsActive && ExpectedSpotTypes.Values
                        .SelectMany(templates => templates)
                        .Select(template => template.Name)
                        .Contains(t.Name))
                    .ToListAsync();
                
                foreach (var type in inactiveExpectedTypes)
                {
                    type.IsActive = true;
                    type.UpdatedAt = DateTime.UtcNow;
                    result.TypesActivated++;
                    result.ActionsPerformed.Add($"Activated SpotType: {type.Name}");
                }
                
                // 2. Create missing expected types
                foreach (var categoryTypes in ExpectedSpotTypes)
                {
                    var category = categoryTypes.Key;
                    var templates = categoryTypes.Value;
                    
                    foreach (var template in templates)
                    {
                        var exists = await _context.SpotTypes
                            .AnyAsync(t => t.Name == template.Name && t.Category == category);
                            
                        if (!exists)
                        {
                            var newType = new SpotType
                            {
                                Name = template.Name,
                                IconPath = template.IconPath,
                                ColorCode = template.ColorCode,
                                Category = category,
                                Description = template.Description,
                                RequiresExpertValidation = template.RequiresExpertValidation,
                                ValidationCriteria = "{}",
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            
                            _context.SpotTypes.Add(newType);
                            result.TypesCreated++;
                            result.ActionsPerformed.Add($"Created SpotType: {template.Name} for category {category}");
                        }
                    }
                }
                
                // 3. Fix orphaned spots
                var defaultDivingType = await _context.SpotTypes
                    .FirstOrDefaultAsync(t => t.Category == ActivityCategory.Diving && t.IsActive);
                    
                if (defaultDivingType != null)
                {
                    var orphanedSpots = await _context.Spots
                        .Where(s => s.Type == null || !s.Type.IsActive)
                        .ToListAsync();
                        
                    foreach (var spot in orphanedSpots)
                    {
                        spot.TypeId = defaultDivingType.Id;
                        result.ActionsPerformed.Add($"Fixed orphaned spot: {spot.Name}");
                    }
                }
                
                // Save all changes
                var changesSaved = await _context.SaveChangesAsync();
                
                result.Success = true;
                result.PostRepairState = await GetDiagnosticsAsync();
                
                _logger.LogInformation("SpotType repair completed successfully. Changes: {ChangeCount}, Activated: {Activated}, Created: {Created}",
                    changesSaved, result.TypesActivated, result.TypesCreated);
                    
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SpotType repair");
                result.Success = false;
                result.Errors.Add($"Repair failed: {ex.Message}");
                
                // Try to rollback any partial changes
                try
                {
                    await _context.Database.RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Failed to rollback transaction during repair failure");
                    result.Errors.Add($"Rollback failed: {rollbackEx.Message}");
                }
            }
            
            return result;
        }
        
        public async Task<bool> HasActiveSpotTypesForCategoryAsync(ActivityCategory category)
        {
            try
            {
                return await _context.SpotTypes
                    .AsNoTracking()
                    .AnyAsync(t => t.Category == category && t.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active SpotTypes for category {Category}", category);
                return false;
            }
        }
        
        public async Task<Dictionary<ActivityCategory, int>> GetActiveTypeCountsByCategoryAsync()
        {
            try
            {
                var counts = await _context.SpotTypes
                    .AsNoTracking()
                    .Where(t => t.IsActive)
                    .GroupBy(t => t.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count);
                
                // Ensure all categories are represented
                foreach (var category in Enum.GetValues<ActivityCategory>())
                {
                    if (!counts.ContainsKey(category))
                    {
                        counts[category] = 0;
                    }
                }
                
                return counts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active type counts by category");
                return new Dictionary<ActivityCategory, int>();
            }
        }
        
        /// <summary>
        /// Template for creating SpotTypes
        /// </summary>
        private record SpotTypeTemplate(string Name, string IconPath, string ColorCode, bool RequiresExpertValidation, string Description);
    }
}