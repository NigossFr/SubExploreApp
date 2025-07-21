using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service for initializing and migrating the database programmatically
    /// </summary>
    public class DatabaseInitializationService : IDatabaseInitializationService
    {
        private readonly SubExploreDbContext _context;
        private readonly ILogger<DatabaseInitializationService> _logger;

        public DatabaseInitializationService(SubExploreDbContext context, ILogger<DatabaseInitializationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Starting database initialization");

                // Apply migrations (this will create the database if it doesn't exist)
                await _context.Database.MigrateAsync();
                _logger.LogInformation("Database migrations applied successfully");

                // Migration includes all seed data including admin user and tables

                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database initialization");
                throw;
            }
        }

        public async Task<bool> IsDatabaseInitializedAsync()
        {
            try
            {
                // Check if we can connect to the database
                await _context.Database.CanConnectAsync();

                // Check if RevokedTokens table exists
                var tableExists = await _context.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) as Value FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'RevokedTokens'"
                ).FirstOrDefaultAsync();

                return tableExists > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database initialization status");
                return false;
            }
        }

        public async Task ApplyMigrationsAsync()
        {
            try
            {
                _logger.LogInformation("Applying database migrations");
                await _context.Database.MigrateAsync();
                _logger.LogInformation("Database migrations applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying database migrations");
                throw;
            }
        }

        public async Task EnsureRevokedTokensTableAsync()
        {
            try
            {
                // Check if RevokedTokens table exists
                var tableExistsQuery = @"
                    SELECT COUNT(*) as Value
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'RevokedTokens'";

                var tableExists = await _context.Database.SqlQueryRaw<int>(tableExistsQuery).FirstOrDefaultAsync();

                if (tableExists == 0)
                {
                    _logger.LogInformation("RevokedTokens table not found, creating it");

                    // Create the table manually
                    var createTableSql = @"
                        CREATE TABLE `RevokedTokens` (
                            `Id` int NOT NULL AUTO_INCREMENT,
                            `TokenHash` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
                            `TokenType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
                            `UserId` int NULL,
                            `RevokedAt` datetime(6) NOT NULL,
                            `ExpiresAt` datetime(6) NULL,
                            `RevocationReason` varchar(200) CHARACTER SET utf8mb4 NULL,
                            `RevocationIpAddress` varchar(45) CHARACTER SET utf8mb4 NULL,
                            CONSTRAINT `PK_RevokedTokens` PRIMARY KEY (`Id`),
                            CONSTRAINT `FK_RevokedTokens_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
                        ) CHARACTER SET=utf8mb4;";

                    await _context.Database.ExecuteSqlRawAsync(createTableSql);

                    // Create indexes
                    var createIndexesSql = @"
                        CREATE UNIQUE INDEX `IX_RevokedTokens_TokenHash` ON `RevokedTokens` (`TokenHash`);
                        CREATE INDEX `IX_RevokedTokens_UserId` ON `RevokedTokens` (`UserId`);
                        CREATE INDEX `IX_RevokedTokens_RevokedAt` ON `RevokedTokens` (`RevokedAt`);
                        CREATE INDEX `IX_RevokedTokens_ExpiresAt` ON `RevokedTokens` (`ExpiresAt`);";

                    // Execute each index creation separately to avoid issues
                    var indexCommands = createIndexesSql.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var indexCommand in indexCommands)
                    {
                        if (!string.IsNullOrWhiteSpace(indexCommand))
                        {
                            await _context.Database.ExecuteSqlRawAsync(indexCommand.Trim());
                        }
                    }

                    // Create migration history table if it doesn't exist and add record
                    var createMigrationHistoryTableSql = @"
                        CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
                            `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
                            `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
                            CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
                        ) CHARACTER SET=utf8mb4;";

                    await _context.Database.ExecuteSqlRawAsync(createMigrationHistoryTableSql);

                    var migrationHistorySql = @"
                        INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
                        VALUES ('20250719120200_AddRevokedTokensTable', '8.0.0');";

                    await _context.Database.ExecuteSqlRawAsync(migrationHistorySql);

                    _logger.LogInformation("RevokedTokens table created successfully");
                }
                else
                {
                    _logger.LogInformation("RevokedTokens table already exists");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring RevokedTokens table exists");
                throw;
            }
        }

        private async Task CreateDefaultAdminUserAsync()
        {
            try
            {
                // Check if any users exist
                var userExistsQuery = @"
                    SELECT COUNT(*) as Value
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'Users'";

                var usersTableExists = await _context.Database.SqlQueryRaw<int>(userExistsQuery).FirstOrDefaultAsync();
                
                if (usersTableExists == 0)
                {
                    _logger.LogWarning("Users table does not exist, cannot create default admin user");
                    return;
                }

                // Check if admin user already exists
                var adminExistsQuery = @"
                    SELECT COUNT(*) as Value
                    FROM Users 
                    WHERE Email = 'admin@subexplore.com'";

                var adminExists = await _context.Database.SqlQueryRaw<int>(adminExistsQuery).FirstOrDefaultAsync();

                if (adminExists == 0)
                {
                    _logger.LogInformation("Creating default admin user");

                    // Create admin user directly with SQL to avoid EF issues
                    var createAdminSql = @"
                        INSERT INTO `Users` (
                            `Email`, `Username`, `FirstName`, `LastName`, 
                            `PasswordHash`, `CreatedAt`, `UpdatedAt`, 
                            `AccountType`, `SubscriptionStatus`
                        ) VALUES (
                            'admin@subexplore.com', 
                            'admin', 
                            'Admin', 
                            'User',
                            @passwordHash,
                            @createdAt,
                            @createdAt,
                            0,
                            0
                        )";

                    // Hash the password: Admin123!
                    var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
                    var createdAt = DateTime.UtcNow;

                    await _context.Database.ExecuteSqlRawAsync(createAdminSql, 
                        new MySqlConnector.MySqlParameter("@passwordHash", passwordHash),
                        new MySqlConnector.MySqlParameter("@createdAt", createdAt));

                    _logger.LogInformation("Default admin user created successfully: admin@subexplore.com / Admin123!");
                }
                else
                {
                    _logger.LogInformation("Default admin user already exists");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating default admin user");
                // Don't throw - this is not critical for app functionality
            }
        }
    }
}