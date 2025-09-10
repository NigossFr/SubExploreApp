using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    public class PostGisTestService
    {
        private readonly IPracticeSpotService _practiceSpotService;
        private readonly IOrganizationService _organizationService;
        private readonly IBusinessService _businessService;
        private readonly ILogger<PostGisTestService> _logger;

        // Coordonnées de test : Concarneau (France)
        private const decimal TestLatitude = 47.8735m;
        private const decimal TestLongitude = -3.9075m;
        private const int TestRadius = 50; // 50km

        public PostGisTestService(
            IPracticeSpotService practiceSpotService,
            IOrganizationService organizationService,
            IBusinessService businessService,
            ILogger<PostGisTestService> logger)
        {
            _practiceSpotService = practiceSpotService;
            _organizationService = organizationService;
            _businessService = businessService;
            _logger = logger;
        }

        /// <summary>
        /// Test complet des fonctions PostGIS
        /// </summary>
        public async Task<PostGisTestResult> TestAllPostGisFunctionsAsync()
        {
            var result = new PostGisTestResult
            {
                TestStartTime = DateTime.UtcNow,
                TestLocation = $"{TestLatitude}, {TestLongitude}",
                TestRadius = TestRadius
            };

            _logger.LogInformation($"🧪 Début des tests PostGIS près de Concarneau ({TestLatitude}, {TestLongitude}) dans un rayon de {TestRadius}km");

            // Test PracticeSpots
            try
            {
                var practiceSpots = await _practiceSpotService.GetNearbyPracticeSpotsAsync(TestLatitude, TestLongitude, TestRadius);
                result.PracticeSpotsFound = practiceSpots.Count();
                result.PracticeSpotsSuccess = true;
                _logger.LogInformation($"✅ PracticeSpots: {result.PracticeSpotsFound} spots trouvés");
                
                // Log quelques détails
                foreach (var spot in practiceSpots.Take(3))
                {
                    _logger.LogInformation($"   - {spot.Name} ({spot.DifficultyLevel})");
                }
            }
            catch (Exception ex)
            {
                result.PracticeSpotsSuccess = false;
                result.PracticeSpotsError = ex.Message;
                _logger.LogError(ex, "❌ Erreur PracticeSpots");
            }

            // Test Organizations
            try
            {
                var organizations = await _organizationService.GetNearbyOrganizationsAsync(TestLatitude, TestLongitude, TestRadius);
                result.OrganizationsFound = organizations.Count();
                result.OrganizationsSuccess = true;
                _logger.LogInformation($"✅ Organizations: {result.OrganizationsFound} organisations trouvées");
                
                // Log quelques détails
                foreach (var org in organizations.Take(2))
                {
                    _logger.LogInformation($"   - {org.Name} ({org.OrganizationType})");
                }
            }
            catch (Exception ex)
            {
                result.OrganizationsSuccess = false;
                result.OrganizationsError = ex.Message;
                _logger.LogError(ex, "❌ Erreur Organizations");
            }

            // Test Businesses
            try
            {
                var businesses = await _businessService.GetNearbyBusinessesAsync(TestLatitude, TestLongitude, TestRadius);
                result.BusinessesFound = businesses.Count();
                result.BusinessesSuccess = true;
                _logger.LogInformation($"✅ Businesses: {result.BusinessesFound} commerces trouvés");
                
                // Log quelques détails
                foreach (var business in businesses.Take(2))
                {
                    _logger.LogInformation($"   - {business.Name} ({business.BusinessType})");
                }
            }
            catch (Exception ex)
            {
                result.BusinessesSuccess = false;
                result.BusinessesError = ex.Message;
                _logger.LogError(ex, "❌ Erreur Businesses");
            }

            result.TestEndTime = DateTime.UtcNow;
            result.TestDurationMs = (int)(result.TestEndTime - result.TestStartTime).TotalMilliseconds;

            // Résumé final
            var totalFound = result.PracticeSpotsFound + result.OrganizationsFound + result.BusinessesFound;
            var successCount = (result.PracticeSpotsSuccess ? 1 : 0) + (result.OrganizationsSuccess ? 1 : 0) + (result.BusinessesSuccess ? 1 : 0);
            
            _logger.LogInformation($"🏁 Test terminé en {result.TestDurationMs}ms - {successCount}/3 fonctions réussies - {totalFound} entités trouvées au total");

            result.OverallSuccess = successCount == 3;
            return result;
        }

        /// <summary>
        /// Test avec filtres spécifiques
        /// </summary>
        public async Task<string> TestWithFiltersAsync()
        {
            var results = new List<string>();

            // Test avec filtre spot type (à adapter selon vos données)
            try
            {
                var guidTest = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"); // Exemple
                var filteredSpots = await _practiceSpotService.GetNearbyPracticeSpotsAsync(TestLatitude, TestLongitude, TestRadius, guidTest);
                results.Add($"Spots avec filtre GUID: {filteredSpots.Count()}");
            }
            catch (Exception ex)
            {
                results.Add($"Spots avec filtre GUID: ERREUR - {ex.Message}");
            }

            // Test avec filtre organization type
            try
            {
                var clubsFFESSM = await _organizationService.GetNearbyOrganizationsAsync(TestLatitude, TestLongitude, TestRadius, OrganizationType.ClubFFESSM);
                results.Add($"Clubs FFESSM: {clubsFFESSM.Count()}");
            }
            catch (Exception ex)
            {
                results.Add($"Clubs FFESSM: ERREUR - {ex.Message}");
            }

            // Test avec filtre business type
            try
            {
                var diveShops = await _businessService.GetNearbyBusinessesAsync(TestLatitude, TestLongitude, TestRadius, BusinessType.DiveShop);
                results.Add($"Magasins de plongée: {diveShops.Count()}");
            }
            catch (Exception ex)
            {
                results.Add($"Magasins de plongée: ERREUR - {ex.Message}");
            }

            return string.Join("\n", results);
        }
    }

    public class PostGisTestResult
    {
        public DateTime TestStartTime { get; set; }
        public DateTime TestEndTime { get; set; }
        public int TestDurationMs { get; set; }
        public string TestLocation { get; set; } = string.Empty;
        public int TestRadius { get; set; }

        public bool PracticeSpotsSuccess { get; set; }
        public int PracticeSpotsFound { get; set; }
        public string? PracticeSpotsError { get; set; }

        public bool OrganizationsSuccess { get; set; }
        public int OrganizationsFound { get; set; }
        public string? OrganizationsError { get; set; }

        public bool BusinessesSuccess { get; set; }
        public int BusinessesFound { get; set; }
        public string? BusinessesError { get; set; }

        public bool OverallSuccess { get; set; }

        public string GetSummary()
        {
            var total = PracticeSpotsFound + OrganizationsFound + BusinessesFound;
            var successCount = (PracticeSpotsSuccess ? 1 : 0) + (OrganizationsSuccess ? 1 : 0) + (BusinessesSuccess ? 1 : 0);
            
            return $"PostGIS Test Results:\n" +
                   $"Location: {TestLocation} (radius: {TestRadius}km)\n" +
                   $"Duration: {TestDurationMs}ms\n" +
                   $"Success: {successCount}/3 functions\n" +
                   $"Total entities found: {total}\n" +
                   $"- Practice Spots: {PracticeSpotsFound} {(PracticeSpotsSuccess ? "✅" : "❌")}\n" +
                   $"- Organizations: {OrganizationsFound} {(OrganizationsSuccess ? "✅" : "❌")}\n" +
                   $"- Businesses: {BusinessesFound} {(BusinessesSuccess ? "✅" : "❌")}\n" +
                   $"Overall: {(OverallSuccess ? "✅ SUCCESS" : "❌ FAILURE")}";
        }
    }
}