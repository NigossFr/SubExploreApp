using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Models.Enums;
using SubExplore.Helpers.Extensions;
using Microsoft.EntityFrameworkCore;

namespace SubExplore.Helpers
{
    /// <summary>
    /// Tool to debug filtering issues after migration
    /// </summary>
    public class FilterDebugTool
    {
        private readonly SubExploreDbContext _context;
        private readonly ILogger<FilterDebugTool> _logger;

        public FilterDebugTool(SubExploreDbContext context, ILogger<FilterDebugTool> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Comprehensive filter debugging analysis
        /// </summary>
        public async Task<string> AnalyzeFilteringIssuesAsync()
        {
            try
            {
                var report = "=== FILTER DEBUG ANALYSIS ===\n\n";

                // 1. Check SpotTypes and their categories
                var spotTypes = await _context.SpotTypes
                    .Where(st => st.IsActive)
                    .OrderBy(st => st.Category)
                    .ThenBy(st => st.Name)
                    .ToListAsync();

                report += "📊 SPOT TYPES AFTER MIGRATION:\n";
                foreach (var spotType in spotTypes)
                {
                    var mainCategory = spotType.GetMainCategory();
                    var belongsToActivities = spotType.BelongsToCategory("Activités");
                    var belongsToStructures = spotType.BelongsToCategory("Structures");
                    var belongsToBoutiques = spotType.BelongsToCategory("Boutiques");

                    report += $"  • {spotType.Name}\n";
                    report += $"    - DB Category: {spotType.Category}\n";
                    report += $"    - Main Category: {mainCategory}\n";
                    report += $"    - Activités: {belongsToActivities}\n";
                    report += $"    - Structures: {belongsToStructures}\n";
                    report += $"    - Boutiques: {belongsToBoutiques}\n\n";
                }

                // 2. Check actual spots and their type associations
                var spots = await _context.Spots
                    .Include(s => s.Type)
                    .Where(s => s.ValidationStatus == SpotValidationStatus.Approved)
                    .ToListAsync();

                report += "\n📍 SPOTS AND THEIR CATEGORIES:\n";
                var spotsByCategory = spots.GroupBy(s => s.Type?.GetMainCategory() ?? "Unknown");
                foreach (var group in spotsByCategory)
                {
                    report += $"  {group.Key}: {group.Count()} spots\n";
                    foreach (var spot in group.Take(3)) // Show first 3 spots per category
                    {
                        report += $"    - {spot.Name} (Type: {spot.Type?.Name}, DB Category: {spot.Type?.Category})\n";
                    }
                    if (group.Count() > 3)
                    {
                        report += $"    ... and {group.Count() - 3} more\n";
                    }
                    report += "\n";
                }

                // 3. Test filtering logic
                report += "🔍 FILTER TESTING:\n";
                
                var activitiesSpots = spots.Where(s => s.Type.BelongsToCategory("Activités")).Count();
                var structuresSpots = spots.Where(s => s.Type.BelongsToCategory("Structures")).Count();
                var boutiquesSpots = spots.Where(s => s.Type.BelongsToCategory("Boutiques")).Count();

                report += $"  • Activités filter would show: {activitiesSpots} spots\n";
                report += $"  • Structures filter would show: {structuresSpots} spots\n";
                report += $"  • Boutiques filter would show: {boutiquesSpots} spots\n\n";

                // 4. Check for potential issues
                report += "⚠️ POTENTIAL ISSUES:\n";
                
                var spotsWithNullType = spots.Where(s => s.Type == null).Count();
                if (spotsWithNullType > 0)
                {
                    report += $"  ❌ {spotsWithNullType} spots have null Type\n";
                }

                var spotsWithUnknownCategory = spots.Where(s => s.Type != null && s.Type.GetMainCategory() == "Autres").Count();
                if (spotsWithUnknownCategory > 0)
                {
                    report += $"  ⚠️ {spotsWithUnknownCategory} spots have 'Autres' category\n";
                }

                var inactiveSpotTypes = await _context.SpotTypes.Where(st => !st.IsActive).CountAsync();
                if (inactiveSpotTypes > 0)
                {
                    report += $"  ℹ️ {inactiveSpotTypes} inactive spot types in database\n";
                }

                if (spotsWithNullType == 0 && spotsWithUnknownCategory == 0)
                {
                    report += "  ✅ No obvious data issues found\n";
                }

                report += "\n=== END ANALYSIS ===";

                _logger.LogInformation("Filter debug analysis completed");
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze filtering issues");
                return $"❌ Analysis failed: {ex.Message}";
            }
        }
    }
}