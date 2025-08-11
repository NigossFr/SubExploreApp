using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Helpers.Extensions;

namespace SubExplore
{
    /// <summary>
    /// Simple test to verify filter functionality works correctly
    /// </summary>
    public class TestFilterFunctionality
    {
        public static void RunTests()
        {
            Console.WriteLine("=== Filter Functionality Test ===");
            
            // Create test spot types
            var divingSpotType = new SpotType
            {
                Id = 1,
                Name = "Plongée bouteille",
                Category = ActivityCategory.Diving,
                ColorCode = "#0077BE",
                IsActive = true
            };
            
            var apneeSpotType = new SpotType
            {
                Id = 2,
                Name = "Apnée",
                Category = ActivityCategory.Freediving,
                ColorCode = "#00AA00",
                IsActive = true
            };
            
            var structureSpotType = new SpotType
            {
                Id = 3,
                Name = "Clubs",
                Category = ActivityCategory.Structure,
                ColorCode = "#228B22",
                IsActive = true
            };
            
            var shopSpotType = new SpotType
            {
                Id = 4,
                Name = "Boutiques",
                Category = ActivityCategory.Shop,
                ColorCode = "#FF8C00",
                IsActive = true
            };

            var spotTypes = new List<SpotType>
            {
                divingSpotType,
                apneeSpotType,
                structureSpotType,
                shopSpotType
            };

            // Test BelongsToCategory method
            Console.WriteLine("Testing BelongsToCategory:");
            Console.WriteLine($"Diving belongs to Activités: {divingSpotType.BelongsToCategory("Activités")}"); // Should be true
            Console.WriteLine($"Apnée belongs to Activités: {apneeSpotType.BelongsToCategory("Activités")}"); // Should be true
            Console.WriteLine($"Structure belongs to Structures: {structureSpotType.BelongsToCategory("Structures")}"); // Should be true
            Console.WriteLine($"Shop belongs to Boutiques: {shopSpotType.BelongsToCategory("Boutiques")}"); // Should be true
            Console.WriteLine($"Diving belongs to Structures: {divingSpotType.BelongsToCategory("Structures")}"); // Should be false

            // Test FilterByMainCategory method
            Console.WriteLine("\nTesting FilterByMainCategory:");
            var activitiesSpots = spotTypes.FilterByMainCategory("Activités").ToList();
            var structureSpots = spotTypes.FilterByMainCategory("Structures").ToList();
            var shopSpots = spotTypes.FilterByMainCategory("Boutiques").ToList();

            Console.WriteLine($"Activités category contains {activitiesSpots.Count} spot types:");
            activitiesSpots.ForEach(s => Console.WriteLine($"  - {s.Name} ({s.Category})"));

            Console.WriteLine($"Structures category contains {structureSpots.Count} spot types:");
            structureSpots.ForEach(s => Console.WriteLine($"  - {s.Name} ({s.Category})"));

            Console.WriteLine($"Boutiques category contains {shopSpots.Count} spot types:");
            shopSpots.ForEach(s => Console.WriteLine($"  - {s.Name} ({s.Category})"));

            // Test GetCategoryBaseColor method
            Console.WriteLine("\nTesting GetCategoryBaseColor:");
            Console.WriteLine($"Activités color: {SpotTypeExtensions.GetCategoryBaseColor("Activités")}");
            Console.WriteLine($"Structures color: {SpotTypeExtensions.GetCategoryBaseColor("Structures")}");
            Console.WriteLine($"Boutiques color: {SpotTypeExtensions.GetCategoryBaseColor("Boutiques")}");

            Console.WriteLine("\n=== Tests completed ===");
        }
    }
}