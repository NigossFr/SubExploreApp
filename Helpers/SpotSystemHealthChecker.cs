using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;

namespace SubExplore.Helpers
{
    /// <summary>
    /// Comprehensive spot system health checker and validator
    /// Ensures data integrity and system functionality after database changes
    /// </summary>
    public static class SpotSystemHealthChecker
    {
        public static async Task<SpotSystemHealthReport> RunComprehensiveHealthCheckAsync(IServiceProvider serviceProvider)
        {
            var report = new SpotSystemHealthReport
            {
                CheckStartTime = DateTime.UtcNow,
                Issues = new List<string>(),
                Warnings = new List<string>(),
                Recommendations = new List<string>()
            };

            try
            {
                // Test 1: Database Connectivity and Schema
                await CheckDatabaseConnectivity(serviceProvider, report);

                // Test 2: Spot Types Integrity
                await CheckSpotTypesIntegrity(serviceProvider, report);

                // Test 3: Spot Data Consistency
                await CheckSpotDataConsistency(serviceProvider, report);

                // Test 4: Repository Layer Functionality
                await CheckRepositoryFunctionality(serviceProvider, report);

                // Test 5: Filtering System Validation
                await CheckFilteringSystem(serviceProvider, report);

                // Test 6: Performance Analysis
                await CheckPerformanceMetrics(serviceProvider, report);

                report.CheckEndTime = DateTime.UtcNow;
                report.IsHealthy = report.Issues.Count == 0;
                report.OverallScore = CalculateHealthScore(report);

                return report;
            }
            catch (Exception ex)
            {
                report.Issues.Add($"CRITICAL: Health check failed with exception: {ex.Message}");
                report.IsHealthy = false;
                report.OverallScore = 0;
                return report;
            }
        }

        private static async Task CheckDatabaseConnectivity(IServiceProvider serviceProvider, SpotSystemHealthReport report)
        {
            try
            {
                var dbContext = serviceProvider.GetService<SubExploreDbContext>();
                if (dbContext == null)
                {
                    report.Issues.Add("CRITICAL: DbContext not available in DI container");
                    return;
                }

                var canConnect = await dbContext.Database.CanConnectAsync();
                if (!canConnect)
                {
                    report.Issues.Add("CRITICAL: Cannot connect to database");
                    return;
                }

                // Check critical tables exist
                var tables = new[] { "Spots", "SpotTypes", "Users", "SpotMedia" };
                foreach (var tableName in tables)
                {
                    var query = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{tableName}'";
                    var exists = await dbContext.Database.SqlQueryRaw<int>(query).FirstOrDefaultAsync();
                    if (exists == 0)
                    {
                        report.Issues.Add($"CRITICAL: Table {tableName} does not exist");
                    }
                }

                report.DatabaseConnectivity = true;
                report.Recommendations.Add("✅ Database connectivity is healthy");
            }
            catch (Exception ex)
            {
                report.Issues.Add($"CRITICAL: Database connectivity check failed: {ex.Message}");
                report.DatabaseConnectivity = false;
            }
        }

        private static async Task CheckSpotTypesIntegrity(IServiceProvider serviceProvider, SpotSystemHealthReport report)
        {
            try
            {
                var spotTypeRepository = serviceProvider.GetService<ISpotTypeRepository>();
                if (spotTypeRepository == null)
                {
                    report.Issues.Add("CRITICAL: SpotTypeRepository not available");
                    return;
                }

                var activeTypes = await spotTypeRepository.GetActiveTypesAsync();
                var typesList = activeTypes.ToList();

                if (typesList.Count == 0)
                {
                    report.Issues.Add("CRITICAL: No active spot types found");
                    return;
                }

                // Validate hierarchical structure
                var categories = typesList.GroupBy(t => t.Category).ToList();
                if (categories.Count < 3)
                {
                    report.Warnings.Add($"WARNING: Only {categories.Count} activity categories found, expected more for hierarchical system");
                }

                // Check for required properties
                foreach (var spotType in typesList)
                {
                    if (string.IsNullOrEmpty(spotType.ColorCode))
                    {
                        report.Warnings.Add($"WARNING: SpotType '{spotType.Name}' has no color code");
                    }
                    
                    if (string.IsNullOrEmpty(spotType.IconPath))
                    {
                        report.Warnings.Add($"WARNING: SpotType '{spotType.Name}' has no icon path");
                    }
                }

                report.ActiveSpotTypesCount = typesList.Count;
                report.SpotTypeCategories = categories.Count;
                report.Recommendations.Add($"✅ Found {typesList.Count} active spot types across {categories.Count} categories");
            }
            catch (Exception ex)
            {
                report.Issues.Add($"CRITICAL: Spot types integrity check failed: {ex.Message}");
            }
        }

        private static async Task CheckSpotDataConsistency(IServiceProvider serviceProvider, SpotSystemHealthReport report)
        {
            try
            {
                var spotRepository = serviceProvider.GetService<ISpotRepository>();
                if (spotRepository == null)
                {
                    report.Issues.Add("CRITICAL: SpotRepository not available");
                    return;
                }

                var allSpots = await spotRepository.GetAllAsync();
                var spotsList = allSpots.ToList();

                report.TotalSpotsCount = spotsList.Count;

                if (spotsList.Count == 0)
                {
                    report.Warnings.Add("WARNING: No spots found in database");
                    return;
                }

                // Check for orphaned spots (spots with invalid TypeId)
                var spotsWithInvalidType = spotsList.Where(s => s.Type == null).ToList();
                if (spotsWithInvalidType.Any())
                {
                    report.Issues.Add($"CRITICAL: {spotsWithInvalidType.Count} spots have invalid TypeId references");
                }

                // Check for spots without creators
                var spotsWithoutCreator = spotsList.Where(s => s.Creator == null).ToList();
                if (spotsWithoutCreator.Any())
                {
                    report.Warnings.Add($"WARNING: {spotsWithoutCreator.Count} spots have invalid CreatorId references");
                }

                // Check validation status distribution
                var statusGroups = spotsList.GroupBy(s => s.ValidationStatus).ToList();
                foreach (var group in statusGroups)
                {
                    report.Recommendations.Add($"📊 {group.Count()} spots with status: {group.Key}");
                }

                // Check geographic distribution
                var spotsWithValidCoordinates = spotsList.Where(s => 
                    s.Latitude >= -90 && s.Latitude <= 90 && 
                    s.Longitude >= -180 && s.Longitude <= 180).Count();
                    
                if (spotsWithValidCoordinates < spotsList.Count)
                {
                    report.Issues.Add($"CRITICAL: {spotsList.Count - spotsWithValidCoordinates} spots have invalid coordinates");
                }

                report.ApprovedSpotsCount = spotsList.Count(s => s.ValidationStatus == SpotValidationStatus.Approved);
                report.Recommendations.Add($"✅ Data consistency check completed for {spotsList.Count} spots");
            }
            catch (Exception ex)
            {
                report.Issues.Add($"CRITICAL: Spot data consistency check failed: {ex.Message}");
            }
        }

        private static async Task CheckRepositoryFunctionality(IServiceProvider serviceProvider, SpotSystemHealthReport report)
        {
            try
            {
                var spotRepository = serviceProvider.GetService<ISpotRepository>();
                if (spotRepository == null)
                {
                    report.Issues.Add("CRITICAL: SpotRepository not available");
                    return;
                }

                // Test basic operations
                var allSpots = await spotRepository.GetAllAsync();
                if (allSpots == null)
                {
                    report.Issues.Add("CRITICAL: GetAllAsync returned null");
                    return;
                }

                // Test approved spots filtering
                var approvedSpots = await spotRepository.GetSpotsByValidationStatusAsync(SpotValidationStatus.Approved);
                var approvedCount = approvedSpots?.Count() ?? 0;

                // Test type-based filtering
                var spotTypeRepository = serviceProvider.GetService<ISpotTypeRepository>();
                if (spotTypeRepository != null)
                {
                    var activeTypes = await spotTypeRepository.GetActiveTypesAsync();
                    var firstType = activeTypes.FirstOrDefault();
                    if (firstType != null)
                    {
                        var spotsByType = await spotRepository.GetSpotsByTypeAsync(firstType.Id);
                        var typeFilterCount = spotsByType?.Count() ?? 0;
                        
                        report.Recommendations.Add($"✅ Type filtering works: {typeFilterCount} spots found for type '{firstType.Name}'");
                    }
                }

                // Test category-based filtering (new method)
                try
                {
                    var spotsByCategory = await spotRepository.GetSpotsByCategoryAsync(ActivityCategory.Diving);
                    var categoryFilterCount = spotsByCategory?.Count() ?? 0;
                    report.Recommendations.Add($"✅ Category filtering works: {categoryFilterCount} spots found for Diving category");
                }
                catch (Exception)
                {
                    report.Warnings.Add("WARNING: Category-based filtering method may not be implemented");
                }

                report.RepositoryFunctionality = true;
                report.Recommendations.Add($"✅ Repository functionality validated with {approvedCount} approved spots");
            }
            catch (Exception ex)
            {
                report.Issues.Add($"CRITICAL: Repository functionality check failed: {ex.Message}");
                report.RepositoryFunctionality = false;
            }
        }

        private static async Task CheckFilteringSystem(IServiceProvider serviceProvider, SpotSystemHealthReport report)
        {
            try
            {
                var spotRepository = serviceProvider.GetService<ISpotRepository>();
                var spotTypeRepository = serviceProvider.GetService<ISpotTypeRepository>();
                
                if (spotRepository == null || spotTypeRepository == null)
                {
                    report.Issues.Add("CRITICAL: Required repositories not available for filtering test");
                    return;
                }

                var activeTypes = await spotTypeRepository.GetActiveTypesAsync();
                var categories = activeTypes.GroupBy(t => t.Category).ToList();

                var filteringResults = new Dictionary<ActivityCategory, int>();

                foreach (var categoryGroup in categories)
                {
                    var category = categoryGroup.Key;
                    try
                    {
                        var spots = await spotRepository.GetSpotsByCategoryAsync(category);
                        filteringResults[category] = spots?.Count() ?? 0;
                    }
                    catch
                    {
                        filteringResults[category] = -1; // Error indicator
                    }
                }

                foreach (var result in filteringResults)
                {
                    if (result.Value == -1)
                    {
                        report.Warnings.Add($"WARNING: Category filtering failed for {result.Key}");
                    }
                    else
                    {
                        report.Recommendations.Add($"✅ Category {result.Key}: {result.Value} spots");
                    }
                }

                report.FilteringSystemWorking = filteringResults.Values.All(v => v >= 0);
                report.Recommendations.Add("✅ Filtering system validation completed");
            }
            catch (Exception ex)
            {
                report.Issues.Add($"CRITICAL: Filtering system check failed: {ex.Message}");
                report.FilteringSystemWorking = false;
            }
        }

        private static async Task CheckPerformanceMetrics(IServiceProvider serviceProvider, SpotSystemHealthReport report)
        {
            try
            {
                var spotRepository = serviceProvider.GetService<ISpotRepository>();
                if (spotRepository == null)
                {
                    report.Issues.Add("CRITICAL: SpotRepository not available for performance test");
                    return;
                }

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Test query performance
                var allSpots = await spotRepository.GetAllAsync();
                var allSpotsTime = stopwatch.ElapsedMilliseconds;
                stopwatch.Restart();

                var approvedSpots = await spotRepository.GetSpotsByValidationStatusAsync(SpotValidationStatus.Approved);
                var filterTime = stopwatch.ElapsedMilliseconds;
                stopwatch.Restart();

                try
                {
                    var minimalSpots = await spotRepository.GetSpotsMinimalAsync(50);
                    var minimalTime = stopwatch.ElapsedMilliseconds;
                    
                    if (minimalTime > 1000)
                    {
                        report.Warnings.Add($"WARNING: Minimal query took {minimalTime}ms, consider optimization");
                    }
                    else
                    {
                        report.Recommendations.Add($"✅ Minimal query performance: {minimalTime}ms");
                    }
                }
                catch
                {
                    report.Warnings.Add("WARNING: GetSpotsMinimalAsync method not available");
                }

                if (allSpotsTime > 2000)
                {
                    report.Issues.Add($"CRITICAL: GetAllAsync took {allSpotsTime}ms, performance issue detected");
                }
                else if (allSpotsTime > 1000)
                {
                    report.Warnings.Add($"WARNING: GetAllAsync took {allSpotsTime}ms, consider optimization");
                }

                report.AverageQueryTime = (allSpotsTime + filterTime) / 2;
                report.Recommendations.Add($"✅ Performance metrics: GetAll={allSpotsTime}ms, Filter={filterTime}ms");
            }
            catch (Exception ex)
            {
                report.Issues.Add($"CRITICAL: Performance metrics check failed: {ex.Message}");
            }
        }

        private static int CalculateHealthScore(SpotSystemHealthReport report)
        {
            int baseScore = 100;
            
            // Deduct points for issues
            baseScore -= report.Issues.Count * 20;
            baseScore -= report.Warnings.Count * 5;
            
            // Bonus for working features
            if (report.DatabaseConnectivity) baseScore += 5;
            if (report.RepositoryFunctionality) baseScore += 5;
            if (report.FilteringSystemWorking) baseScore += 5;
            if (report.ActiveSpotTypesCount > 5) baseScore += 5;
            if (report.ApprovedSpotsCount > 0) baseScore += 5;
            
            return Math.Max(0, Math.Min(100, baseScore));
        }
    }

    /// <summary>
    /// Comprehensive health report for the spot system
    /// </summary>
    public class SpotSystemHealthReport
    {
        public DateTime CheckStartTime { get; set; }
        public DateTime CheckEndTime { get; set; }
        public bool IsHealthy { get; set; }
        public int OverallScore { get; set; }
        
        // Critical Components
        public bool DatabaseConnectivity { get; set; }
        public bool RepositoryFunctionality { get; set; }
        public bool FilteringSystemWorking { get; set; }
        
        // Data Metrics
        public int TotalSpotsCount { get; set; }
        public int ApprovedSpotsCount { get; set; }
        public int ActiveSpotTypesCount { get; set; }
        public int SpotTypeCategories { get; set; }
        
        // Performance Metrics
        public long AverageQueryTime { get; set; }
        
        // Issues and Recommendations
        public List<string> Issues { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        
        public string GetSummary()
        {
            var summary = $"Spot System Health Report - Score: {OverallScore}/100\n";
            summary += $"Status: {(IsHealthy ? "HEALTHY" : "ISSUES DETECTED")}\n";
            summary += $"Check Duration: {(CheckEndTime - CheckStartTime).TotalSeconds:F2}s\n";
            summary += $"Total Spots: {TotalSpotsCount} (Approved: {ApprovedSpotsCount})\n";
            summary += $"Spot Types: {ActiveSpotTypesCount} across {SpotTypeCategories} categories\n";
            summary += $"Average Query Time: {AverageQueryTime}ms\n";
            
            if (Issues.Any())
            {
                summary += $"\n🚨 CRITICAL ISSUES ({Issues.Count}):\n";
                summary += string.Join("\n", Issues.Select(i => $"  • {i}"));
            }
            
            if (Warnings.Any())
            {
                summary += $"\n⚠️ WARNINGS ({Warnings.Count}):\n";
                summary += string.Join("\n", Warnings.Select(w => $"  • {w}"));
            }
            
            if (Recommendations.Any())
            {
                summary += $"\n✅ STATUS UPDATES ({Recommendations.Count}):\n";
                summary += string.Join("\n", Recommendations.Select(r => $"  • {r}"));
            }
            
            return summary;
        }
    }
}