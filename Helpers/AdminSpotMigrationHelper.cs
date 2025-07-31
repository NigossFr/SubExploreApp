using MySqlConnector;
using System.Diagnostics;

namespace SubExplore.Helpers
{
    /// <summary>
    /// Helper to migrate existing admin spots to Approved status using direct SQL
    /// </summary>
    public static class AdminSpotMigrationHelper
    {
        /// <summary>
        /// Update all admin and moderator spots to Approved status
        /// </summary>
        public static async Task MigrateAdminSpotsToApprovedAsync()
        {
            try
            {
                Debug.WriteLine("[AdminSpotMigrationHelper] 🔧 Starting admin spot migration to Approved status");
                
                // Use Android emulator connection string
                var connectionString = "Server=10.0.2.2;Port=3306;User ID=subexplore_user;Password=SubExplore2024!;Database=subexplore_dev;Allow User Variables=True;Character Set=utf8mb4;Connection Timeout=30;";
                Debug.WriteLine("[AdminSpotMigrationHelper] 🔧 Using AndroidEmulatorConnection for migration");

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                Debug.WriteLine("[AdminSpotMigrationHelper] ✅ Database connected for admin spot migration");

                // First, get admin/moderator user IDs
                var getAdminUsersQuery = @"
                    SELECT Id, FirstName, LastName, Email, AccountType 
                    FROM Users 
                    WHERE AccountType IN (1, 4)"; // Administrator = 1, ExpertModerator = 4

                var adminUserIds = new List<int>();
                using (var adminCmd = new MySqlCommand(getAdminUsersQuery, connection))
                using (var reader = await adminCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var userId = reader.GetInt32("Id");
                        var firstName = reader.GetString("FirstName");
                        var lastName = reader.GetString("LastName");
                        var email = reader.GetString("Email");
                        var accountType = reader.GetInt32("AccountType");
                        
                        adminUserIds.Add(userId);
                        Debug.WriteLine($"[AdminSpotMigrationHelper] Found admin user: {firstName} {lastName} ({email}) - Type: {accountType} - ID: {userId}");
                    }
                }

                if (!adminUserIds.Any())
                {
                    Debug.WriteLine("[AdminSpotMigrationHelper] ⚠️ No admin/moderator users found");
                    return;
                }

                Debug.WriteLine($"[AdminSpotMigrationHelper] Found {adminUserIds.Count} admin/moderator users");

                // Get current spot status for diagnostic
                var diagnosticQuery = @"
                    SELECT s.Id, s.Name, s.CreatorId, s.ValidationStatus, u.FirstName, u.LastName, u.AccountType
                    FROM Spots s 
                    JOIN Users u ON s.CreatorId = u.Id 
                    ORDER BY s.CreatedAt DESC
                    LIMIT 20";

                using (var diagCmd = new MySqlCommand(diagnosticQuery, connection))
                using (var reader = await diagCmd.ExecuteReaderAsync())
                {
                    Debug.WriteLine("[AdminSpotMigrationHelper] === CURRENT SPOT STATUS DIAGNOSTIC ===");
                    while (await reader.ReadAsync())
                    {
                        var spotId = reader.GetInt32("Id");
                        var spotName = reader.GetString("Name");
                        var creatorId = reader.GetInt32("CreatorId");
                        var validationStatus = reader.GetInt32("ValidationStatus");
                        var firstName = reader.GetString("FirstName");
                        var lastName = reader.GetString("LastName");
                        var accountType = reader.GetInt32("AccountType");
                        
                        Debug.WriteLine($"[AdminSpotMigrationHelper] Spot: {spotName} | Creator: {firstName} {lastName} (ID:{creatorId}, Type:{accountType}) | Status: {validationStatus}");
                    }
                }

                // Update admin spots to Approved status (5)
                var adminUserIdsString = string.Join(",", adminUserIds);
                var updateQuery = $@"
                    UPDATE Spots 
                    SET ValidationStatus = 5 
                    WHERE CreatorId IN ({adminUserIdsString}) 
                    AND ValidationStatus != 5";

                using var updateCmd = new MySqlCommand(updateQuery, connection);
                var updatedCount = await updateCmd.ExecuteNonQueryAsync();
                Debug.WriteLine($"[AdminSpotMigrationHelper] ✅ Updated {updatedCount} admin spots to Approved status (5)");

                // Create a test pending spot for validation workflow testing
                await CreateTestPendingSpotAsync(connection);

                Debug.WriteLine("[AdminSpotMigrationHelper] 🎉 Admin spot migration completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminSpotMigrationHelper] ❌ Error during admin spot migration: {ex.Message}");
                Debug.WriteLine($"[AdminSpotMigrationHelper] Stack trace: {ex.StackTrace}");
                // Don't throw - allow app to continue
            }
        }

        private static async Task CreateTestPendingSpotAsync(MySqlConnection connection)
        {
            try
            {
                // Get a regular user for creating test spot
                var getUserQuery = @"
                    SELECT Id FROM Users 
                    WHERE AccountType = 0 
                    LIMIT 1"; // Standard user

                int? regularUserId = null;
                using (var userCmd = new MySqlCommand(getUserQuery, connection))
                using (var reader = await userCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        regularUserId = reader.GetInt32("Id");
                    }
                }

                if (!regularUserId.HasValue)
                {
                    Debug.WriteLine("[AdminSpotMigrationHelper] No regular users found, creating test user");
                    
                    var createUserQuery = @"
                        INSERT INTO Users (Email, FirstName, LastName, PasswordHash, AccountType, ExpertiseLevel, IsEmailConfirmed, CreatedAt)
                        VALUES ('testuser@subexplore.com', 'Test', 'User', 'dummy_hash', 0, 2, 1, NOW())";
                    
                    using var createUserCmd = new MySqlCommand(createUserQuery, connection);
                    await createUserCmd.ExecuteNonQueryAsync();
                    
                    regularUserId = (int)createUserCmd.LastInsertedId;
                    Debug.WriteLine($"[AdminSpotMigrationHelper] Created test user with ID: {regularUserId}");
                }

                // Get a spot type
                var getSpotTypeQuery = "SELECT Id FROM SpotTypes LIMIT 1";
                int? spotTypeId = null;
                using (var typeCmd = new MySqlCommand(getSpotTypeQuery, connection))
                using (var reader = await typeCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        spotTypeId = reader.GetInt32("Id");
                    }
                }

                if (!spotTypeId.HasValue)
                {
                    Debug.WriteLine("[AdminSpotMigrationHelper] ⚠️ No spot types found, cannot create test spot");
                    return;
                }

                // Check if test spot already exists
                var checkExistingQuery = "SELECT COUNT(*) FROM Spots WHERE Name = '[TEST VALIDATION] Spot en attente de validation'";
                using var checkCmd = new MySqlCommand(checkExistingQuery, connection);
                var existingCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                if (existingCount > 0)
                {
                    Debug.WriteLine("[AdminSpotMigrationHelper] Test pending spot already exists");
                    return;
                }

                // Create test pending spot
                var createSpotQuery = @"
                    INSERT INTO Spots (
                        Name, Description, Latitude, Longitude, DifficultyLevel, TypeId, CreatorId,
                        RequiredEquipment, SafetyNotes, BestConditions, ValidationStatus, MaxDepth,
                        CurrentStrength, HasMooring, BottomType, CreatedAt
                    ) VALUES (
                        '[TEST VALIDATION] Spot en attente de validation',
                        'Ce spot de test permet de vérifier le workflow de validation des administrateurs.',
                        43.2951, 5.3656, 2, @TypeId, @CreatorId,
                        'Équipement standard de plongée',
                        'Respecter les consignes de sécurité',
                        'Mer calme, visibilité > 10m',
                        1, 25, 2, 1, 'Sable et rochers', NOW()
                    )";

                using var createSpotCmd = new MySqlCommand(createSpotQuery, connection);
                createSpotCmd.Parameters.AddWithValue("@TypeId", spotTypeId.Value);
                createSpotCmd.Parameters.AddWithValue("@CreatorId", regularUserId.Value);
                await createSpotCmd.ExecuteNonQueryAsync();

                Debug.WriteLine("[AdminSpotMigrationHelper] ✅ Created test pending spot for validation workflow");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminSpotMigrationHelper] ⚠️ Error creating test pending spot: {ex.Message}");
                // Continue anyway - not critical
            }
        }
    }
}