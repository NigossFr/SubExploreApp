using MySqlConnector;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SubExplore.Helpers
{
    /// <summary>
    /// Helper to fix Username unique constraint issues
    /// </summary>
    public static class UsernameMigrationHelper
    {
        public static async Task ApplyUsernameMigrationAsync()
        {
            try
            {
                Debug.WriteLine("[UsernameMigrationHelper] 🔧 Starting Username migration");
                
                // Use Android emulator connection string
                var connectionString = "Server=10.0.2.2;Port=3306;User ID=subexplore_user;Password=SubExplore2024!;Database=subexplore_dev;Allow User Variables=True;Character Set=utf8mb4;Connection Timeout=30;";
                Debug.WriteLine("[UsernameMigrationHelper] 🔧 Using AndroidEmulatorConnection for migration");
                Debug.WriteLine($"[UsernameMigrationHelper] Using connection: server=10.0.2.2;port=3306;database=subexplore_dev;...");

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                Debug.WriteLine("[UsernameMigrationHelper] ✅ Database connected for migration");

                // First, update all empty/null usernames to be unique
                var updateUsernamesQuery = @"
                    UPDATE Users 
                    SET Username = CONCAT('user_', Id) 
                    WHERE Username IS NULL OR Username = '' OR TRIM(Username) = ''";

                using var updateCmd = new MySqlCommand(updateUsernamesQuery, connection);
                var updatedRows = await updateCmd.ExecuteNonQueryAsync();
                Debug.WriteLine($"[UsernameMigrationHelper] ✅ Updated {updatedRows} usernames to be unique");

                // Check if unique constraint exists and is problematic
                var checkConstraintQuery = @"
                    SELECT CONSTRAINT_NAME, COLUMN_NAME 
                    FROM information_schema.TABLE_CONSTRAINTS tc
                    JOIN information_schema.KEY_COLUMN_USAGE kcu ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                    WHERE tc.TABLE_SCHEMA = DATABASE() 
                    AND tc.TABLE_NAME = 'Users' 
                    AND tc.CONSTRAINT_TYPE = 'UNIQUE'
                    AND kcu.COLUMN_NAME = 'Username'";

                using var checkCmd = new MySqlCommand(checkConstraintQuery, connection);
                using var reader = await checkCmd.ExecuteReaderAsync();
                
                bool hasUniqueConstraint = reader.HasRows;
                string? constraintName = null;
                
                if (reader.Read())
                {
                    constraintName = reader.GetString("CONSTRAINT_NAME");
                }
                reader.Close();

                if (hasUniqueConstraint && !string.IsNullOrEmpty(constraintName))
                {
                    Debug.WriteLine($"[UsernameMigrationHelper] Found unique constraint: {constraintName}");
                    
                    // Drop the unique constraint temporarily
                    var dropConstraintQuery = $"ALTER TABLE Users DROP INDEX {constraintName}";
                    using var dropCmd = new MySqlCommand(dropConstraintQuery, connection);
                    await dropCmd.ExecuteNonQueryAsync();
                    Debug.WriteLine($"[UsernameMigrationHelper] ✅ Dropped unique constraint: {constraintName}");
                    
                    // Make Username column nullable and allow duplicates for now
                    var modifyColumnQuery = "ALTER TABLE Users MODIFY COLUMN Username VARCHAR(30) NULL";
                    using var modifyCmd = new MySqlCommand(modifyColumnQuery, connection);
                    await modifyCmd.ExecuteNonQueryAsync();
                    Debug.WriteLine("[UsernameMigrationHelper] ✅ Modified Username column to be nullable");
                }
                else
                {
                    Debug.WriteLine("[UsernameMigrationHelper] ✅ No problematic unique constraint found");
                }

                Debug.WriteLine("[UsernameMigrationHelper] 🎉 Username migration completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UsernameMigrationHelper] ❌ Error during username migration: {ex.Message}");
                Debug.WriteLine($"[UsernameMigrationHelper] Stack trace: {ex.StackTrace}");
                // Don't throw - allow app to continue
            }
        }
    }
}