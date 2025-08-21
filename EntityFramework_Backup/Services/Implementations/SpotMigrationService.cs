using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using System.Diagnostics;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service to migrate existing admin spots to approved status
    /// </summary>
    public class SpotMigrationService
    {
        private readonly SubExploreDbContext _context;
        private readonly ILogger<SpotMigrationService> _logger;

        public SpotMigrationService(SubExploreDbContext context, ILogger<SpotMigrationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Apply spot validation migration using existing DbContext
        /// </summary>
        public async Task ApplySpotValidationMigrationAsync()
        {
            try
            {
                _logger.LogInformation("[SpotMigrationService] Starting spot validation migration...");

                // 1. Update existing admin spots to Approved status
                await UpdateAdminSpotsToApprovedAsync();

                // 2. Create a new pending spot for validation testing
                await CreatePendingTestSpotAsync();

                _logger.LogInformation("[SpotMigrationService] ✓ Spot validation migration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpotMigrationService] Error during spot validation migration");
                throw;
            }
        }

        private async Task UpdateAdminSpotsToApprovedAsync()
        {
            try
            {
                // Find admin and moderator users
                _logger.LogInformation("[SpotMigrationService] Searching for admin/moderator users...");
                
                var allUsers = await _context.Users.ToListAsync();
                _logger.LogInformation("[SpotMigrationService] Total users in database: {Count}", allUsers.Count);
                
                foreach (var user in allUsers)
                {
                    _logger.LogInformation("[SpotMigrationService] User: {FirstName} {LastName} ({Email}) - Type: {AccountType}", 
                        user.FirstName, user.LastName, user.Email, user.AccountType);
                }
                
                var adminUsers = await _context.Users
                    .Where(u => u.AccountType == AccountType.Administrator || u.AccountType == AccountType.ExpertModerator)
                    .Select(u => u.Id)
                    .ToListAsync();

                if (!adminUsers.Any())
                {
                    _logger.LogWarning("[SpotMigrationService] No admin/moderator users found");
                    return;
                }

                _logger.LogInformation("[SpotMigrationService] Found {Count} admin/moderator users: {Users}", 
                    adminUsers.Count, string.Join(", ", adminUsers));

                // Get all spots to see their current status
                var allSpots = await _context.Spots.ToListAsync();
                _logger.LogInformation("[SpotMigrationService] Total spots in database: {Count}", allSpots.Count);
                
                foreach (var spot in allSpots.Take(10)) // Show first 10 for diagnosis
                {
                    _logger.LogInformation("[SpotMigrationService] Spot: {Name} - Creator: {CreatorId} - Status: {ValidationStatus} ({StatusValue})", 
                        spot.Name, spot.CreatorId, spot.ValidationStatus, (int)spot.ValidationStatus);
                }
                
                // Update spots created by admin/moderator users to Approved status
                var spotsToUpdate = await _context.Spots
                    .Where(s => adminUsers.Contains(s.CreatorId) && s.ValidationStatus != SpotValidationStatus.Approved)
                    .ToListAsync();

                if (spotsToUpdate.Any())
                {
                    _logger.LogInformation("[SpotMigrationService] Updating {Count} admin spots to Approved status", spotsToUpdate.Count);
                    
                    foreach (var spot in spotsToUpdate)
                    {
                        _logger.LogInformation("[SpotMigrationService] - Updating spot '{Name}' from {OldStatus} to Approved", 
                            spot.Name, spot.ValidationStatus);
                        spot.ValidationStatus = SpotValidationStatus.Approved;
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("[SpotMigrationService] ✓ Updated {Count} admin spots to Approved status", spotsToUpdate.Count);
                }
                else
                {
                    _logger.LogInformation("[SpotMigrationService] No admin spots need status updates");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpotMigrationService] Error updating admin spots");
                throw;
            }
        }

        private async Task CreatePendingTestSpotAsync()
        {
            try
            {
                // Create multiple test users and spots for better testing
                await CreateTestUsersAsync();
                await CreateMultiplePendingSpotsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpotMigrationService] Error creating pending test spots");
                throw;
            }
        }

        private async Task CreateTestUsersAsync()
        {
            try
            {
                // Create regular test users if they don't exist
                var testUsers = new[]
                {
                    new { Email = "user1@subexplore.com", FirstName = "Marie", LastName = "Dupont" },
                    new { Email = "user2@subexplore.com", FirstName = "Pierre", LastName = "Martin" },
                    new { Email = "user3@subexplore.com", FirstName = "Julie", LastName = "Bernard" }
                };

                foreach (var userInfo in testUsers)
                {
                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userInfo.Email);
                    if (existingUser == null)
                    {
                        var newUser = new User
                        {
                            Email = userInfo.Email,
                            FirstName = userInfo.FirstName,
                            LastName = userInfo.LastName,
                            PasswordHash = "dummy_hash_for_testing",
                            AccountType = AccountType.Standard,
                            ExpertiseLevel = ExpertiseLevel.Intermediate,
                            IsEmailConfirmed = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Users.Add(newUser);
                        _logger.LogInformation("[SpotMigrationService] Created test user: {Email}", userInfo.Email);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpotMigrationService] Error creating test users");
                throw;
            }
        }

        private async Task CreateMultiplePendingSpotsAsync()
        {
            try
            {
                // Get spot type
                var spotType = await _context.SpotTypes.FirstOrDefaultAsync();
                if (spotType == null)
                {
                    _logger.LogWarning("[SpotMigrationService] No spot types found, cannot create test spots");
                    return;
                }

                // Get regular users
                var regularUsers = await _context.Users
                    .Where(u => u.AccountType == AccountType.Standard)
                    .ToListAsync();

                if (!regularUsers.Any())
                {
                    _logger.LogWarning("[SpotMigrationService] No regular users found for creating test spots");
                    return;
                }

                // Create multiple pending spots with different statuses
                var pendingSpots = new[]
                {
                    new
                    {
                        Name = "[TEST] Épave du Donator - En attente",
                        Description = "Épave de cargo à 35m de profondeur. Site exceptionnel pour plongeurs expérimentés.",
                        Status = SpotValidationStatus.Pending,
                        Lat = 43.2951m,
                        Lng = 5.3656m,
                        Depth = 35
                    },
                    new
                    {
                        Name = "[TEST] Tombant de la Cassidaigne - Révision",
                        Description = "Magnifique tombant avec faune méditerranéenne. Attention aux courants.",
                        Status = SpotValidationStatus.NeedsRevision,
                        Lat = 43.2108m,
                        Lng = 5.4286m,
                        Depth = 28
                    },
                    new
                    {
                        Name = "[TEST] Grotte bleue - Sécurité",
                        Description = "Grotte sous-marine accessible aux débutants. Très belle luminosité.",
                        Status = SpotValidationStatus.SafetyReview,
                        Lat = 43.1570m,
                        Lng = 5.5477m,
                        Depth = 15
                    },
                    new
                    {
                        Name = "[TEST] Récif des Moyades - En cours",
                        Description = "Récif artificiel avec belle biodiversité. Idéal pour formation.",
                        Status = SpotValidationStatus.UnderReview,
                        Lat = 43.2691m,
                        Lng = 5.3427m,
                        Depth = 22
                    }
                };

                for (int i = 0; i < pendingSpots.Length; i++)
                {
                    var spotInfo = pendingSpots[i];
                    var user = regularUsers[i % regularUsers.Count];

                    // Check if spot already exists
                    var existingSpot = await _context.Spots.FirstOrDefaultAsync(s => s.Name == spotInfo.Name);
                    if (existingSpot != null)
                    {
                        _logger.LogInformation("[SpotMigrationService] Test spot already exists: {Name}", spotInfo.Name);
                        continue;
                    }

                    var newSpot = new Spot
                    {
                        Name = spotInfo.Name,
                        Description = spotInfo.Description,
                        Latitude = spotInfo.Lat,
                        Longitude = spotInfo.Lng,
                        DifficultyLevel = spotInfo.Depth > 30 ? DifficultyLevel.Advanced : 
                                         spotInfo.Depth > 20 ? DifficultyLevel.Intermediate : DifficultyLevel.Beginner,
                        TypeId = spotType.Id,
                        CreatorId = user.Id,
                        RequiredEquipment = "Équipement plongée standard",
                        SafetyNotes = "Respect des consignes de sécurité",
                        BestConditions = "Conditions météo favorables, mer calme",
                        ValidationStatus = spotInfo.Status,
                        MaxDepth = spotInfo.Depth,
                        CurrentStrength = CurrentStrength.Moderate,
                        HasMooring = true,
                        BottomType = "Sable et rochers",
                        CreatedAt = DateTime.UtcNow.AddDays(-new Random().Next(1, 30)) // Random creation date
                    };

                    _context.Spots.Add(newSpot);
                    _logger.LogInformation("[SpotMigrationService] Created test spot: {Name} with status {Status}", 
                        spotInfo.Name, spotInfo.Status);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("[SpotMigrationService] ✓ Created multiple pending test spots for validation workflow");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpotMigrationService] Error creating multiple pending spots");
                throw;
            }
        }
    }
}