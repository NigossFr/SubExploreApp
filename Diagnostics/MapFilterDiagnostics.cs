using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using System.Diagnostics;

namespace SubExplore.Diagnostics
{
    /// <summary>
    /// Diagnostic utility to troubleshoot map filter issues
    /// </summary>
    public class MapFilterDiagnostics
    {
        private readonly ISpotRepository _spotRepository;
        private readonly ISpotTypeRepository _spotTypeRepository;
        private readonly ILogger<MapFilterDiagnostics> _logger;

        public MapFilterDiagnostics(
            ISpotRepository spotRepository,
            ISpotTypeRepository spotTypeRepository,
            ILogger<MapFilterDiagnostics> logger)
        {
            _spotRepository = spotRepository;
            _spotTypeRepository = spotTypeRepository;
            _logger = logger;
        }

        /// <summary>
        /// Run comprehensive diagnostics for map filter functionality
        /// </summary>
        public async Task<FilterDiagnosticReport> RunDiagnosticsAsync()
        {
            var report = new FilterDiagnosticReport();
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Starting map filter diagnostics...");

            try
            {
                // Test 1: Database connectivity
                await TestDatabaseConnectivity(report);

                // Test 2: Spot types loading
                await TestSpotTypesLoading(report);

                // Test 3: Spots loading
                await TestSpotsLoading(report);

                // Test 4: Filter functionality
                await TestFilterFunctionality(report);

                // Test 5: Repository methods
                await TestRepositoryMethods(report);

                report.TotalTestTimeMs = stopwatch.ElapsedMilliseconds;
                report.OverallResult = report.FailedTests.Count == 0 ? "PASS" : "FAIL";

                _logger.LogInformation("Map filter diagnostics completed in {TimeMs}ms. Result: {Result}", 
                    report.TotalTestTimeMs, report.OverallResult);

                return report;
            }
            catch (Exception ex)
            {
                report.CriticalError = ex.Message;
                report.OverallResult = "CRITICAL_FAILURE";
                _logger.LogError(ex, "Critical failure during map filter diagnostics");
                return report;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        private async Task TestDatabaseConnectivity(FilterDiagnosticReport report)
        {
            try
            {
                _logger.LogDebug("Testing database connectivity...");
                
                var testSpots = await _spotRepository.GetAllAsync();
                var testTypes = await _spotTypeRepository.GetAllAsync();

                report.PassedTests.Add("Database connectivity");
                report.DatabaseConnected = true;
                report.SpotsInDatabase = testSpots?.Count() ?? 0;
                report.SpotTypesInDatabase = testTypes?.Count() ?? 0;

                _logger.LogInformation("Database connectivity: PASS ({SpotCount} spots, {TypeCount} types)", 
                    report.SpotsInDatabase, report.SpotTypesInDatabase);
            }
            catch (Exception ex)
            {
                report.FailedTests.Add($"Database connectivity: {ex.Message}");
                report.DatabaseConnected = false;
                _logger.LogError(ex, "Database connectivity test failed");
            }
        }

        private async Task TestSpotTypesLoading(FilterDiagnosticReport report)
        {
            try
            {
                _logger.LogDebug("Testing spot types loading...");

                var activeTypes = await _spotTypeRepository.GetActiveTypesAsync();
                var allTypes = await _spotTypeRepository.GetAllAsync();

                if (activeTypes?.Any() == true)
                {
                    report.PassedTests.Add("Spot types loading");
                    report.ActiveSpotTypes = activeTypes.ToList();
                    
                    _logger.LogInformation("Spot types loading: PASS ({ActiveCount} active, {TotalCount} total)", 
                        activeTypes.Count(), allTypes?.Count() ?? 0);
                    
                    foreach (var type in activeTypes)
                    {
                        _logger.LogDebug("Active spot type: {TypeName} (ID: {TypeId})", type.Name, type.Id);
                    }
                }
                else
                {
                    report.FailedTests.Add("Spot types loading: No active types found");
                    _logger.LogWarning("Spot types loading: FAIL - No active types found");
                }
            }
            catch (Exception ex)
            {
                report.FailedTests.Add($"Spot types loading: {ex.Message}");
                _logger.LogError(ex, "Spot types loading test failed");
            }
        }

        private async Task TestSpotsLoading(FilterDiagnosticReport report)
        {
            try
            {
                _logger.LogDebug("Testing spots loading...");

                var spots = await _spotRepository.GetAllAsync();
                var approvedSpots = spots?.Where(s => s.ValidationStatus == Models.Enums.SpotValidationStatus.Approved);

                if (spots?.Any() == true)
                {
                    report.PassedTests.Add("Spots loading");
                    report.TotalSpots = spots.Count();
                    report.ApprovedSpots = approvedSpots?.Count() ?? 0;
                    
                    _logger.LogInformation("Spots loading: PASS ({TotalCount} total, {ApprovedCount} approved)", 
                        report.TotalSpots, report.ApprovedSpots);

                    // Check spot distribution by type
                    var spotsByType = spots.GroupBy(s => s.TypeId).ToList();
                    foreach (var group in spotsByType)
                    {
                        _logger.LogDebug("Spots by type {TypeId}: {Count} spots", group.Key, group.Count());
                    }
                }
                else
                {
                    report.FailedTests.Add("Spots loading: No spots found in database");
                    _logger.LogWarning("Spots loading: FAIL - No spots found");
                }
            }
            catch (Exception ex)
            {
                report.FailedTests.Add($"Spots loading: {ex.Message}");
                _logger.LogError(ex, "Spots loading test failed");
            }
        }

        private async Task TestFilterFunctionality(FilterDiagnosticReport report)
        {
            try
            {
                _logger.LogDebug("Testing filter functionality...");

                if (report.ActiveSpotTypes?.Any() != true)
                {
                    report.FailedTests.Add("Filter functionality: No active spot types to test with");
                    return;
                }

                var testType = report.ActiveSpotTypes.First();
                var filteredSpots = await _spotRepository.GetSpotsByTypeAsync(testType.Id);

                report.PassedTests.Add("Filter functionality");
                report.FilterTestResults = $"Type {testType.Name} returned {filteredSpots?.Count() ?? 0} spots";
                
                _logger.LogInformation("Filter functionality: PASS - Type {TypeName} returned {Count} spots", 
                    testType.Name, filteredSpots?.Count() ?? 0);
            }
            catch (Exception ex)
            {
                report.FailedTests.Add($"Filter functionality: {ex.Message}");
                _logger.LogError(ex, "Filter functionality test failed");
            }
        }

        private async Task TestRepositoryMethods(FilterDiagnosticReport report)
        {
            try
            {
                _logger.LogDebug("Testing repository methods...");

                // Test search functionality
                var searchResults = await _spotRepository.SearchSpotsAsync("test");
                
                // Test geospatial queries
                var nearbySpots = await _spotRepository.GetNearbySpots(43.2969m, 5.3811m, 10.0);

                report.PassedTests.Add("Repository methods");
                report.RepositoryTestResults = $"Search returned {searchResults?.Count() ?? 0} results, " +
                    $"Nearby query returned {nearbySpots?.Count() ?? 0} spots";
                
                _logger.LogInformation("Repository methods: PASS - Search: {SearchCount}, Nearby: {NearbyCount}", 
                    searchResults?.Count() ?? 0, nearbySpots?.Count() ?? 0);
            }
            catch (Exception ex)
            {
                report.FailedTests.Add($"Repository methods: {ex.Message}");
                _logger.LogError(ex, "Repository methods test failed");
            }
        }

        /// <summary>
        /// Generate troubleshooting recommendations based on diagnostic results
        /// </summary>
        public List<string> GenerateRecommendations(FilterDiagnosticReport report)
        {
            var recommendations = new List<string>();

            if (!report.DatabaseConnected)
            {
                recommendations.Add("CRITICAL: Check database connection string and ensure MySQL server is running");
                recommendations.Add("Verify connection string configuration in appsettings.json");
                recommendations.Add("Test database connectivity manually using MySQL client");
            }

            if (report.SpotTypesInDatabase == 0)
            {
                recommendations.Add("No spot types in database - run database seed/migration to populate SpotTypes table");
                recommendations.Add("Check if database initialization service is properly configured");
            }

            if (report.SpotsInDatabase == 0)
            {
                recommendations.Add("No spots in database - consider importing sample data for testing");
                recommendations.Add("Check if real_spots.json resource is being loaded correctly");
            }

            if (report.ActiveSpotTypes?.Count == 0 && report.SpotTypesInDatabase > 0)
            {
                recommendations.Add("Spot types exist but none are active - check IsActive flag in SpotTypes table");
            }

            if (report.ApprovedSpots == 0 && report.TotalSpots > 0)
            {
                recommendations.Add("Spots exist but none are approved - check ValidationStatus in Spots table");
                recommendations.Add("Consider updating spots to Approved status for testing");
            }

            if (report.FailedTests.Any(f => f.Contains("Repository methods")))
            {
                recommendations.Add("Repository method issues - check Entity Framework configuration");
                recommendations.Add("Verify database indexes are properly created");
            }

            if (report.FailedTests.Count == 0)
            {
                recommendations.Add("All tests passed - issue may be in ViewModel initialization or UI binding");
                recommendations.Add("Check if MapViewModel.InitializeAsync() is being called");
                recommendations.Add("Verify that SpotTypes collection is properly bound to UI CollectionView");
                recommendations.Add("Check browser developer tools for any JavaScript/binding errors");
            }

            return recommendations;
        }
    }

    public class FilterDiagnosticReport
    {
        public DateTime TestTimestamp { get; set; } = DateTime.UtcNow;
        public long TotalTestTimeMs { get; set; }
        public string OverallResult { get; set; } = "UNKNOWN";
        public string? CriticalError { get; set; }

        // Database connectivity
        public bool DatabaseConnected { get; set; }
        public int SpotsInDatabase { get; set; }
        public int SpotTypesInDatabase { get; set; }

        // Data loading
        public List<SpotType> ActiveSpotTypes { get; set; } = new();
        public int TotalSpots { get; set; }
        public int ApprovedSpots { get; set; }

        // Test results
        public List<string> PassedTests { get; set; } = new();
        public List<string> FailedTests { get; set; } = new();
        public string? FilterTestResults { get; set; }
        public string? RepositoryTestResults { get; set; }

        public override string ToString()
        {
            return $"Filter Diagnostics Report\n" +
                   $"========================\n" +
                   $"Timestamp: {TestTimestamp:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Total Time: {TotalTestTimeMs}ms\n" +
                   $"Overall Result: {OverallResult}\n" +
                   $"Database Connected: {DatabaseConnected}\n" +
                   $"Spots in DB: {SpotsInDatabase} (Approved: {ApprovedSpots})\n" +
                   $"Spot Types in DB: {SpotTypesInDatabase} (Active: {ActiveSpotTypes.Count})\n" +
                   $"Tests Passed: {PassedTests.Count}\n" +
                   $"Tests Failed: {FailedTests.Count}\n" +
                   (!string.IsNullOrEmpty(CriticalError) ? $"Critical Error: {CriticalError}\n" : "") +
                   (FailedTests.Any() ? $"Failed Tests:\n{string.Join("\n", FailedTests.Select(f => $"  - {f}"))}\n" : "");
        }
    }
}