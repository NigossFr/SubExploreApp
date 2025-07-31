using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Reflection;
using System.Diagnostics;

namespace SubExplore.Helpers
{
    /// <summary>
    /// Helper class to manually apply IsEmailConfirmed column migration
    /// This resolves the "Unknown column 'u.IsEmailConfirmed' in 'field list'" error
    /// </summary>
    public static class IsEmailConfirmedMigrationHelper
    {
        /// <summary>
        /// Apply the IsEmailConfirmed column migration manually
        /// This adds the missing column that prevents admin login
        /// </summary>
        public static async Task ApplyIsEmailConfirmedMigrationAsync()
        {
            try
            {
                Debug.WriteLine("[IsEmailConfirmedMigrationHelper] 🔧 Starting IsEmailConfirmed migration");
                
                // Load configuration to get connection string (same as MigrationHelper)
                var assembly = Assembly.GetExecutingAssembly();
                var appSettingsResourceName = "SubExplore.appsettings.json";
                using var stream = assembly.GetManifestResourceStream(appSettingsResourceName);
                
                IConfiguration configuration;
                if (stream != null)
                {
                    configuration = new ConfigurationBuilder()
                        .AddJsonStream(stream)
                        .Build();
                }
                else
                {
                    throw new InvalidOperationException("Could not load appsettings.json");
                }

                // Get connection string based on platform (same logic as MigrationHelper)
                string connectionString = null;
                
#if ANDROID
                if (Microsoft.Maui.Devices.DeviceInfo.Current.DeviceType == Microsoft.Maui.Devices.DeviceType.Virtual)
                {
                    connectionString = configuration.GetConnectionString("AndroidEmulatorConnection");
                    Debug.WriteLine("[IsEmailConfirmedMigrationHelper] 🔧 Using AndroidEmulatorConnection for migration");
                }
                else
                {
                    connectionString = configuration.GetConnectionString("AndroidDeviceConnection");
                    Debug.WriteLine("[IsEmailConfirmedMigrationHelper] 🔧 Using AndroidDeviceConnection for migration");
                }
#elif IOS
                connectionString = configuration.GetConnectionString("iOSConnection");
                Debug.WriteLine("[IsEmailConfirmedMigrationHelper] 🔧 Using iOSConnection for migration");
#elif WINDOWS
                connectionString = configuration.GetConnectionString("WindowsConnection");
                Debug.WriteLine("[IsEmailConfirmedMigrationHelper] 🔧 Using WindowsConnection for migration");
#else
                connectionString = configuration.GetConnectionString("DefaultConnection");
                Debug.WriteLine("[IsEmailConfirmedMigrationHelper] 🔧 Using DefaultConnection for migration");
#endif

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Connection string not found for current platform");
                }

                Debug.WriteLine($"[IsEmailConfirmedMigrationHelper] Using connection: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");

                // Check if column already exists
                Debug.WriteLine("[IsEmailConfirmedMigrationHelper] Checking if IsEmailConfirmed column exists...");
                
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                Debug.WriteLine("[IsEmailConfirmedMigrationHelper] ✅ Database connected for migration");

                // Check if IsEmailConfirmed column exists
                var checkColumnQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.columns 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'Users' 
                    AND column_name = 'IsEmailConfirmed'";

                using var checkCommand = new MySqlCommand(checkColumnQuery, connection);
                var columnCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                if (columnCount == 0)
                {
                    Debug.WriteLine("[IsEmailConfirmedMigrationHelper] ➕ Adding IsEmailConfirmed column...");
                    
                    // Add the column
                    var addColumnQuery = @"
                        ALTER TABLE Users 
                        ADD COLUMN IsEmailConfirmed TINYINT(1) NOT NULL DEFAULT 0";
                    
                    using var addColumnCommand = new MySqlCommand(addColumnQuery, connection);
                    await addColumnCommand.ExecuteNonQueryAsync();
                    
                    // Set admin user as email confirmed
                    Debug.WriteLine("[IsEmailConfirmedMigrationHelper] ✅ Setting admin user as email confirmed...");
                    
                    var updateAdminQuery = @"
                        UPDATE Users 
                        SET IsEmailConfirmed = 1 
                        WHERE Email = 'admin@subexplore.com'";
                    
                    using var updateCommand = new MySqlCommand(updateAdminQuery, connection);
                    var affectedRows = await updateCommand.ExecuteNonQueryAsync();
                    
                    Debug.WriteLine($"[IsEmailConfirmedMigrationHelper] ✅ Updated {affectedRows} admin user(s)");
                    Debug.WriteLine("[IsEmailConfirmedMigrationHelper] ✅ IsEmailConfirmed migration completed successfully");
                }
                else
                {
                    Debug.WriteLine("[IsEmailConfirmedMigrationHelper] ✅ IsEmailConfirmed column already exists");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IsEmailConfirmedMigrationHelper] ❌ Migration failed: {ex.Message}");
                Debug.WriteLine($"[IsEmailConfirmedMigrationHelper] Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to apply IsEmailConfirmed migration: {ex.Message}", ex);
            }
        }
    }
}