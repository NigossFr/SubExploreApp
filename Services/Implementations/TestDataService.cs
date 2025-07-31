using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service to create test data for validation workflow
    /// </summary>
    public class TestDataService
    {
        private readonly SubExploreDbContext _context;

        public TestDataService(SubExploreDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Create test spots for validation workflow testing
        /// </summary>
        public async Task CreateTestSpotsAsync()
        {
            try
            {
                // Vérifier s'il y a déjà des spots de test
                var existingTestSpots = await _context.Spots
                    .Where(s => s.Name.StartsWith("[TEST]"))
                    .CountAsync();

                if (existingTestSpots > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[TestDataService] {existingTestSpots} test spots already exist, skipping creation");
                    return;
                }

                // Obtenir un utilisateur créateur (ou créer un utilisateur de test)
                var creator = await GetOrCreateTestUserAsync();
                if (creator == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TestDataService] Could not create test user");
                    return;
                }

                // Obtenir l'utilisateur admin pour des spots déjà approuvés
                var adminUser = await GetOrCreateAdminUserAsync();
                if (adminUser == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TestDataService] Could not get admin user");
                }

                // Obtenir un type de spot par défaut
                var spotType = await _context.SpotTypes.FirstOrDefaultAsync();
                if (spotType == null)
                {
                    // Créer un type de spot par défaut si nécessaire
                    spotType = new SpotType
                    {
                        Name = "Plongée récréative",
                        Category = ActivityCategory.Diving,
                        IsActive = true,
                        RequiresExpertValidation = true,
                        Description = "Site de plongée pour tous niveaux"
                    };
                    _context.SpotTypes.Add(spotType);
                    await _context.SaveChangesAsync();
                }

                // Créer des spots de test avec différents statuts
                var testSpots = new List<Spot>
                {
                    new Spot
                    {
                        Name = "[TEST] Épave du Donator - Marseille",
                        Description = "Magnifique épave accessible aux plongeurs niveau 2. Profondeur 35m, riche en vie marine.",
                        Latitude = 43.2965m,
                        Longitude = 5.3698m,
                        DifficultyLevel = DifficultyLevel.Intermediate,
                        TypeId = spotType.Id,
                        CreatorId = adminUser?.Id ?? creator.Id, // Admin créé - auto-approuvé
                        RequiredEquipment = "Équipement plongée niveau 2, combinaison 5mm",
                        SafetyNotes = "Courant possible, plongée encadrée recommandée",
                        BestConditions = "Mer calme, visibilité >10m",
                        ValidationStatus = adminUser != null ? SpotValidationStatus.Approved : SpotValidationStatus.Pending,
                        MaxDepth = 35,
                        CurrentStrength = CurrentStrength.Moderate,
                        HasMooring = true,
                        BottomType = "Sable et roches",
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    },
                    new Spot
                    {
                        Name = "[TEST] Calanque de Sormiou - Snorkeling",
                        Description = "Site parfait pour la randonnée palmée avec une biodiversité exceptionnelle.",
                        Latitude = 43.2089m,
                        Longitude = 5.4236m,
                        DifficultyLevel = DifficultyLevel.Beginner,
                        TypeId = spotType.Id,
                        CreatorId = adminUser?.Id ?? creator.Id, // Admin créé - auto-approuvé
                        RequiredEquipment = "Masque, tuba, palmes",
                        SafetyNotes = "Attention aux oursins sur les rochers",
                        BestConditions = "Eau calme, température >18°C",
                        ValidationStatus = adminUser != null ? SpotValidationStatus.Approved : SpotValidationStatus.Pending,
                        MaxDepth = 8,
                        CurrentStrength = CurrentStrength.Weak,
                        HasMooring = false,
                        BottomType = "Rochers et herbiers",
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new Spot
                    {
                        Name = "[TEST] Tombant de la Gabinière - Port-Cros",
                        Description = "Tombant spectaculaire dans le parc national de Port-Cros. Plongée technique recommandée.",
                        Latitude = 43.0131m,
                        Longitude = 6.3889m,
                        DifficultyLevel = DifficultyLevel.Advanced,
                        TypeId = spotType.Id,
                        CreatorId = creator.Id,
                        RequiredEquipment = "Équipement plongée tek, nitrox recommandé",
                        SafetyNotes = "ATTENTION: Zone protégée, plongée encadrée obligatoire",
                        BestConditions = "Mer d'huile, visibilité excellente",
                        ValidationStatus = SpotValidationStatus.SafetyReview,
                        MaxDepth = 60,
                        CurrentStrength = CurrentStrength.Strong,
                        HasMooring = true,
                        BottomType = "Tombant rocheux",
                        SafetyFlags = "[\"Signalé pour révision de sécurité - profondeur importante\"]",
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new Spot
                    {
                        Name = "[TEST] Les Moyades - Apnée",
                        Description = "Site d'apnée pour plongeurs confirmés. Records personnels interdits sans encadrement.",
                        Latitude = 43.2758m,
                        Longitude = 5.3347m,
                        DifficultyLevel = DifficultyLevel.Expert,
                        TypeId = spotType.Id,
                        CreatorId = creator.Id,
                        RequiredEquipment = "Équipement apnée, combinaison épaisse",
                        SafetyNotes = "Plongée en binôme obligatoire, profondeur max 40m",
                        BestConditions = "Mer plate, pas de vent",
                        ValidationStatus = SpotValidationStatus.UnderReview,
                        MaxDepth = 40,
                        CurrentStrength = CurrentStrength.Moderate,
                        HasMooring = false,
                        BottomType = "Fond sableux",
                        CreatedAt = DateTime.UtcNow.AddHours(-8)
                    },
                    new Spot
                    {
                        Name = "[TEST] Baie des Anges - Formation débutants",
                        Description = "Site idéal pour la formation des débutants en plongée. Eau claire et peu profonde.",
                        Latitude = 43.6947m,
                        Longitude = 7.2662m,
                        DifficultyLevel = DifficultyLevel.Beginner,
                        TypeId = spotType.Id,
                        CreatorId = creator.Id,
                        RequiredEquipment = "Équipement plongée de base",
                        SafetyNotes = "Site sécurisé, idéal pour l'apprentissage",
                        BestConditions = "Conditions normales acceptables",
                        ValidationStatus = SpotValidationStatus.Pending,
                        MaxDepth = 12,
                        CurrentStrength = CurrentStrength.Weak,
                        HasMooring = true,
                        BottomType = "Sable fin",
                        CreatedAt = DateTime.UtcNow.AddHours(-4)
                    }
                };

                _context.Spots.AddRange(testSpots);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[TestDataService] Created {testSpots.Count} test spots for validation workflow");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TestDataService] Error creating test spots: {ex.Message}");
            }
        }

        private async Task<User?> GetOrCreateTestUserAsync()
        {
            try
            {
                // Chercher un utilisateur de test existant
                var testUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == "testcreator@subexplore.com");

                if (testUser != null)
                {
                    return testUser;
                }

                // Créer un utilisateur de test
                testUser = new User
                {
                    Email = "testcreator@subexplore.com",
                    FirstName = "Test",
                    LastName = "Creator",
                    AccountType = AccountType.Standard,
                    Permissions = UserPermissions.CreateSpots,
                    PasswordHash = "test_hash", // Pour les tests seulement
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                };

                _context.Users.Add(testUser);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine("[TestDataService] Created test user for spot creation");
                return testUser;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TestDataService] Error creating test user: {ex.Message}");
                return null;
            }
        }

        private async Task<User?> GetOrCreateAdminUserAsync()
        {
            try
            {
                // Chercher l'utilisateur admin existant
                var adminUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == "admin@subexplore.com");

                if (adminUser != null)
                {
                    System.Diagnostics.Debug.WriteLine("[TestDataService] Found existing admin user");
                    return adminUser;
                }

                System.Diagnostics.Debug.WriteLine("[TestDataService] No admin user found for test data");
                return null; // Don't create admin here as it should already exist from migrations
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TestDataService] Error finding admin user: {ex.Message}");
                return null;
            }
        }
    }
}