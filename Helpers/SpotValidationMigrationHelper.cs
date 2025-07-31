using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using System.Diagnostics;

namespace SubExplore.Helpers
{
    /// <summary>
    /// Helper class to migrate existing admin spots to approved status
    /// and create test spots for validation workflow
    /// </summary>
    public static class SpotValidationMigrationHelper
    {
        /// <summary>
        /// Apply spot validation migration to existing data
        /// </summary>
        public static async Task ApplySpotValidationMigrationAsync()
        {
            try
            {
                Debug.WriteLine("[SpotValidationMigrationHelper] Starting spot validation migration...");

                var optionsBuilder = new DbContextOptionsBuilder<SubExploreDbContext>();
                
                // Get connection string - this should match the logic in MauiProgram.cs
                string connectionString = GetConnectionString();
                
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                using var context = new SubExploreDbContext(optionsBuilder.Options);

                // 1. Update existing admin spots to Approved status
                await UpdateAdminSpotsToApprovedAsync(context);

                // 2. Create a new pending spot for validation testing
                await CreatePendingTestSpotAsync(context);

                Debug.WriteLine("[SpotValidationMigrationHelper] ✓ Spot validation migration completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SpotValidationMigrationHelper] ⚠️ Migration error: {ex.Message}");
                throw;
            }
        }

        private static async Task UpdateAdminSpotsToApprovedAsync(SubExploreDbContext context)
        {
            try
            {
                // Find admin and moderator users
                Debug.WriteLine("[SpotValidationMigrationHelper] Searching for admin/moderator users...");
                
                var allUsers = await context.Users.ToListAsync();
                Debug.WriteLine($"[SpotValidationMigrationHelper] Total users in database: {allUsers.Count}");
                
                foreach (var user in allUsers)
                {
                    Debug.WriteLine($"[SpotValidationMigrationHelper] User: {user.FirstName} {user.LastName} ({user.Email}) - Type: {user.AccountType}");
                }
                
                var adminUsers = await context.Users
                    .Where(u => u.AccountType == AccountType.Administrator || u.AccountType == AccountType.ExpertModerator)
                    .Select(u => u.Id)
                    .ToListAsync();

                if (!adminUsers.Any())
                {
                    Debug.WriteLine("[SpotValidationMigrationHelper] No admin/moderator users found");
                    return;
                }

                Debug.WriteLine($"[SpotValidationMigrationHelper] Found {adminUsers.Count} admin/moderator users: {string.Join(", ", adminUsers)}");

                // Update spots created by admin/moderator users to Approved status
                Debug.WriteLine("[SpotValidationMigrationHelper] Searching for admin spots to update...");
                
                var allSpots = await context.Spots.ToListAsync();
                Debug.WriteLine($"[SpotValidationMigrationHelper] Total spots in database: {allSpots.Count}");
                
                foreach (var spot in allSpots.Take(10)) // Show first 10 for diagnosis
                {
                    Debug.WriteLine($"[SpotValidationMigrationHelper] Spot: {spot.Name} - Creator: {spot.CreatorId} - Status: {spot.ValidationStatus} ({(int)spot.ValidationStatus})");
                }
                
                var spotsToUpdate = await context.Spots
                    .Where(s => adminUsers.Contains(s.CreatorId) && s.ValidationStatus != SpotValidationStatus.Approved)
                    .ToListAsync();

                if (spotsToUpdate.Any())
                {
                    Debug.WriteLine($"[SpotValidationMigrationHelper] Updating {spotsToUpdate.Count} admin spots to Approved status");
                    
                    foreach (var spot in spotsToUpdate)
                    {
                        Debug.WriteLine($"[SpotValidationMigrationHelper] - Updating spot '{spot.Name}' from {spot.ValidationStatus} to Approved");
                        spot.ValidationStatus = SpotValidationStatus.Approved;
                    }

                    await context.SaveChangesAsync();
                    Debug.WriteLine($"[SpotValidationMigrationHelper] ✓ Updated {spotsToUpdate.Count} admin spots to Approved status");
                }
                else
                {
                    Debug.WriteLine("[SpotValidationMigrationHelper] No admin spots need status updates");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SpotValidationMigrationHelper] Error updating admin spots: {ex.Message}");
                throw;
            }
        }

        private static async Task CreatePendingTestSpotAsync(SubExploreDbContext context)
        {
            try
            {
                // Check if pending test spot already exists
                var existingPendingSpot = await context.Spots
                    .AnyAsync(s => s.Name == "[TEST] Spot en attente de validation" && s.ValidationStatus == SpotValidationStatus.Pending);

                if (existingPendingSpot)
                {
                    Debug.WriteLine("[SpotValidationMigrationHelper] Pending test spot already exists");
                    return;
                }

                // Get a regular user (not admin) for the pending spot
                var regularUser = await context.Users
                    .FirstOrDefaultAsync(u => u.AccountType == AccountType.Standard);

                if (regularUser == null)
                {
                    // Create a test regular user
                    regularUser = new User
                    {
                        Email = "testuser@subexplore.com",
                        FirstName = "Utilisateur",
                        LastName = "Test",
                        PasswordHash = "dummy_hash_for_testing",
                        AccountType = AccountType.Standard,
                        ExpertiseLevel = ExpertiseLevel.Intermediate,
                        IsEmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Users.Add(regularUser);
                    await context.SaveChangesAsync();
                    Debug.WriteLine("[SpotValidationMigrationHelper] Created test regular user");
                }

                // Get spot type
                var spotType = await context.SpotTypes.FirstOrDefaultAsync();
                if (spotType == null)
                {
                    Debug.WriteLine("[SpotValidationMigrationHelper] No spot types found, cannot create test spot");
                    return;
                }

                // Create pending test spot
                var pendingSpot = new Spot
                {
                    Name = "[TEST] Spot en attente de validation",
                    Description = "Nouveau spot créé par un utilisateur standard nécessitant validation par un modérateur.",
                    Latitude = 43.3m,
                    Longitude = 5.4m,
                    DifficultyLevel = DifficultyLevel.Intermediate,
                    TypeId = spotType.Id,
                    CreatorId = regularUser.Id,
                    RequiredEquipment = "Équipement plongée standard",
                    SafetyNotes = "Site nécessitant validation de sécurité",
                    BestConditions = "Conditions météo favorables",
                    ValidationStatus = SpotValidationStatus.Pending,
                    MaxDepth = 25,
                    CurrentStrength = CurrentStrength.Moderate,
                    HasMooring = true,
                    BottomType = "Rochers et sable",
                    CreatedAt = DateTime.UtcNow
                };

                context.Spots.Add(pendingSpot);
                await context.SaveChangesAsync();

                Debug.WriteLine("[SpotValidationMigrationHelper] ✓ Created pending test spot for validation workflow");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SpotValidationMigrationHelper] Error creating pending test spot: {ex.Message}");
                throw;
            }
        }

        private static string GetConnectionString()
        {
            // Use the same connection string logic as the main application
            Debug.WriteLine("[SpotValidationMigrationHelper] Using AndroidEmulatorConnection");
            return "Server=10.0.2.2;Port=3306;Database=subexplore_dev;User ID=subexplore_user;Password=subexplore123;Pooling=True;Minimum Pool Size=1;Maximum Pool Size=10;Allow User Variables=True;Character Set=utf8mb4;Connection Timeout=30;Use Affected Rows=False";
        }
    }
}