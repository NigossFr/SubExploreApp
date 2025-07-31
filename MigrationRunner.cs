using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Reflection;

namespace SubExplore
{
    /// <summary>
    /// Simple migration runner to execute SQL directly
    /// </summary>
    public class MigrationRunner
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("🔧 SubExplore Migration Runner");
                Console.WriteLine("==============================");
                
                // Load configuration
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

                // Get connection string
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("No connection string found");
                }

                Console.WriteLine("🔍 Testing database connection...");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                Console.WriteLine("✅ Database connection successful");

                // Check if migration is needed
                Console.WriteLine("🔍 Checking for existing columns...");
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
                    Console.WriteLine("✅ Role hierarchy columns already exist - migration already applied");
                }
                else
                {
                    Console.WriteLine("⚠️  ModeratorSince column not found - applying migration...");
                    
                    // Apply migration SQL directly
                    var migrationSql = @"
                        -- Add role hierarchy columns
                        ALTER TABLE Users 
                        ADD COLUMN ModeratorSpecialization INT NOT NULL DEFAULT 0,
                        ADD COLUMN ModeratorStatus INT NOT NULL DEFAULT 0,
                        ADD COLUMN Permissions INT NOT NULL DEFAULT 1,
                        ADD COLUMN ModeratorSince DATETIME(6) NULL,
                        ADD COLUMN OrganizationId INT NULL;

                        -- Update existing users to have CreateSpots permission
                        UPDATE Users SET Permissions = 1 WHERE Permissions = 0;

                        -- Create indexes
                        CREATE INDEX IX_Users_OrganizationId ON Users (OrganizationId);
                        CREATE INDEX IX_Users_ModeratorSpecialization_ModeratorStatus ON Users (ModeratorSpecialization, ModeratorStatus);
                        CREATE INDEX IX_Users_Permissions ON Users (Permissions);
                    ";

                    using var migrationCommand = new MySqlCommand(migrationSql, connection);
                    await migrationCommand.ExecuteNonQueryAsync();
                    Console.WriteLine("✅ Migration applied successfully");
                }

                // Verify admin user
                Console.WriteLine("🔍 Checking admin user...");
                var adminSql = "SELECT Id, Email, AccountType, Permissions FROM Users WHERE Email = 'admin@subexplore.com'";
                using var adminCommand = new MySqlCommand(adminSql, connection);
                using var reader = await adminCommand.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var id = reader.GetInt32("Id");
                    var email = reader.GetString("Email");
                    var accountType = reader.GetInt32("AccountType");
                    var permissions = reader.GetInt32("Permissions");
                    
                    Console.WriteLine($"✅ Admin user found:");
                    Console.WriteLine($"   - ID: {id}");
                    Console.WriteLine($"   - Email: {email}");
                    Console.WriteLine($"   - Account Type: {accountType}");
                    Console.WriteLine($"   - Permissions: {permissions}");
                }
                else
                {
                    Console.WriteLine("⚠️  Admin user not found in database");
                }

                Console.WriteLine("\n🎉 Migration process completed successfully!");
                Console.WriteLine("The admin login issue should now be resolved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
                Environment.Exit(1);
            }
        }
    }
}