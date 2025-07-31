using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Reflection;

namespace SubExplore
{
    /// <summary>
    /// Helper to apply role hierarchy migration directly via SQL
    /// </summary>
    public class MigrationHelper
    {
        public static async Task ApplyMigrationsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔧 Starting role hierarchy migration");
                
                // Load configuration to get connection string
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

                // Get connection string based on platform
                string connectionString = null;
                
#if ANDROID
                if (Microsoft.Maui.Devices.DeviceInfo.Current.DeviceType == Microsoft.Maui.Devices.DeviceType.Virtual)
                {
                    connectionString = configuration.GetConnectionString("AndroidEmulatorConnection");
                    System.Diagnostics.Debug.WriteLine("🔧 Using AndroidEmulatorConnection for migration");
                }
                else
                {
                    connectionString = configuration.GetConnectionString("AndroidDeviceConnection");
                    System.Diagnostics.Debug.WriteLine("🔧 Using AndroidDeviceConnection for migration");
                }
#else
                connectionString = configuration.GetConnectionString("DefaultConnection");
                System.Diagnostics.Debug.WriteLine("🔧 Using DefaultConnection for migration");
#endif

                // Fallback to default if platform-specific not found
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = configuration.GetConnectionString("DefaultConnection");
                    System.Diagnostics.Debug.WriteLine("🔧 Fallback to DefaultConnection for migration");
                }
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("No connection string found");
                }

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                System.Diagnostics.Debug.WriteLine("✅ Database connected for migration");

                // Check if migration is needed
                var checkSql = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = 'subexplore_dev' 
                    AND TABLE_NAME = 'Users' 
                    AND COLUMN_NAME = 'ModeratorSince'";

                using var checkCommand = new MySqlCommand(checkSql, connection);
                var columnExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                if (columnExists)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Role hierarchy columns already exist");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Applying role hierarchy migration...");
                    
                    // Apply migration SQL directly
                    var migrationSql = @"
                        ALTER TABLE Users 
                        ADD COLUMN ModeratorSpecialization INT NOT NULL DEFAULT 0,
                        ADD COLUMN ModeratorStatus INT NOT NULL DEFAULT 0,
                        ADD COLUMN Permissions INT NOT NULL DEFAULT 1,
                        ADD COLUMN ModeratorSince DATETIME(6) NULL,
                        ADD COLUMN OrganizationId INT NULL;

                        UPDATE Users SET Permissions = 1 WHERE Permissions = 0;

                        CREATE INDEX IX_Users_OrganizationId ON Users (OrganizationId);
                        CREATE INDEX IX_Users_ModeratorSpecialization_ModeratorStatus ON Users (ModeratorSpecialization, ModeratorStatus);
                        CREATE INDEX IX_Users_Permissions ON Users (Permissions);";

                    using var migrationCommand = new MySqlCommand(migrationSql, connection);
                    await migrationCommand.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine("✅ Role hierarchy migration completed");
                }

                // Verify admin user exists and can be queried
                var adminSql = "SELECT Id, Email, AccountType, Permissions FROM Users WHERE Email = 'admin@subexplore.com'";
                using var adminCommand = new MySqlCommand(adminSql, connection);
                using var reader = await adminCommand.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var adminId = reader.GetInt32("Id");
                    var adminEmail = reader.GetString("Email");
                    var accountType = reader.GetInt32("AccountType");
                    var permissions = reader.GetInt32("Permissions");
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Admin user verified: ID={adminId}, Email={adminEmail}, Type={accountType}, Permissions={permissions}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Admin user not found in database");
                }

                System.Diagnostics.Debug.WriteLine("🎉 Migration verification completed - admin login should work now");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Migration error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
                // Don't throw - continue with app startup anyway
            }
        }
    }
}