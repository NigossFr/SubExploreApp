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

                // Vérifier d'abord si la base de données existe et est prête
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogWarning("Cannot connect to database, attempting to create...");
                }

                // 🔧 CORRECTION: Use EnsureCreated for fresh database instead of migrations
                _logger.LogInformation("Creating database schema and tables...");
                
                try
                {
                    // Try migrations first for existing databases
                    using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                    await _context.Database.MigrateAsync(cancellationTokenSource.Token);
                    _logger.LogInformation("Database migrations applied successfully");
                }
                catch (Exception migrationEx)
                {
                    _logger.LogWarning(migrationEx, "Migration failed, trying EnsureCreated for fresh database");
                    
                    // Fallback to EnsureCreated for completely fresh database
                    var created = await _context.Database.EnsureCreatedAsync();
                    _logger.LogInformation("Database EnsureCreated result: {Created}", created);
                }

                // Ensure critical tables exist (fallback for migration issues)
                await EnsureRevokedTokensTableAsync();
                await EnsureUserFavoriteSpotsTableAsync();

                // 🎯 SOLUTION: Use DatabaseService.SeedDatabaseAsync for reliable data seeding
                _logger.LogInformation("Seeding database with initial data including admin user...");
                var dbService = new DatabaseService(_context, Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseService>.Instance);
                var seedResult = await dbService.SeedDatabaseAsync();
                _logger.LogInformation("Database seeding result: {SeedResult}", seedResult);

                // Also ensure admin user exists with our method as backup
                await CreateDefaultAdminUserAsync();

                // Migration includes all seed data including admin user and tables

                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Database initialization timed out after 2 minutes");
                throw new TimeoutException("Database initialization timed out");
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
                if (!await _context.Database.CanConnectAsync())
                {
                    _logger.LogWarning("Cannot connect to database");
                    return false;
                }

                // PostgreSQL-compatible: Check if critical tables exist using direct SQL execution
                var checkTablesQuery = @"
                    SELECT COUNT(*)
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name IN ('revoked_tokens', 'user_favorite_spots', 'users', 'spots')";

                // Use proper scalar query execution
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = checkTablesQuery;
                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await _context.Database.OpenConnectionAsync();
                var result = await command.ExecuteScalarAsync();
                var tablesCount = Convert.ToInt32(result ?? 0);
                
                // We expect at least 4 critical tables
                var isInitialized = tablesCount >= 4;
                
                _logger.LogInformation("Database initialization check: {TablesFound}/4 critical tables found", tablesCount);
                return isInitialized;
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
                // PostgreSQL: Skip manual table creation - use EF migrations instead
                _logger.LogInformation("PostgreSQL: Relying on EF migrations for revoked_tokens table");
                
                // Just verify the table exists using direct SQL execution
                var tableExistsQuery = @"
                    SELECT COUNT(*)
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = 'revoked_tokens'";

                var tableExists = await _context.Database.ExecuteSqlRawAsync("SELECT 1") >= 0; // Test basic connection first
                
                // Use proper scalar query execution
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = tableExistsQuery;
                await _context.Database.OpenConnectionAsync();
                var result = await command.ExecuteScalarAsync();
                var tableCount = Convert.ToInt32(result ?? 0);
                tableExists = tableCount > 0;
                
                if (tableExists)
                {
                    _logger.LogInformation("✅ revoked_tokens table exists in PostgreSQL");
                }
                else
                {
                    _logger.LogWarning("⚠️ revoked_tokens table not found - should be created by EF migrations");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking revoked_tokens table");
                // Don't throw - not critical for startup
            }
        }

        public async Task EnsureUserFavoriteSpotsTableAsync()
        {
            try
            {
                // PostgreSQL: Skip manual table creation - use EF migrations instead
                _logger.LogInformation("PostgreSQL: Relying on EF migrations for user_favorite_spots table");
                
                // Just verify the table exists using direct SQL execution
                var tableExistsQuery = @"
                    SELECT COUNT(*)
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = 'user_favorite_spots'";

                // Use proper scalar query execution
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = tableExistsQuery;
                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await _context.Database.OpenConnectionAsync();
                var result = await command.ExecuteScalarAsync();
                var tableCount = Convert.ToInt32(result ?? 0);
                var tableExists = tableCount > 0;
                
                if (tableExists)
                {
                    _logger.LogInformation("✅ user_favorite_spots table exists in PostgreSQL");
                }
                else
                {
                    _logger.LogWarning("⚠️ user_favorite_spots table not found - should be created by EF migrations");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user_favorite_spots table");
                // Don't throw - not critical for startup
            }
        }

        private async Task CreateDefaultAdminUserAsync()
        {
            try
            {
                // PostgreSQL-compatible: Check if users table exists using direct SQL execution  
                var userExistsQuery = @"
                    SELECT COUNT(*)
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = 'users'";

                // Use proper scalar query execution
                using var command1 = _context.Database.GetDbConnection().CreateCommand();
                command1.CommandText = userExistsQuery;
                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await _context.Database.OpenConnectionAsync();
                var result1 = await command1.ExecuteScalarAsync();
                var usersTableExists = Convert.ToInt32(result1 ?? 0);
                
                if (usersTableExists == 0)
                {
                    _logger.LogWarning("users table does not exist, cannot create default admin user");
                    return;
                }

                // PostgreSQL-compatible: Check if admin user already exists using direct SQL execution
                var adminExistsQuery = @"
                    SELECT COUNT(*)
                    FROM users 
                    WHERE email = 'admin@subexplore.com'";

                // Use proper scalar query execution
                using var command2 = _context.Database.GetDbConnection().CreateCommand();
                command2.CommandText = adminExistsQuery;
                var result2 = await command2.ExecuteScalarAsync();
                var adminExists = Convert.ToInt32(result2 ?? 0);

                if (adminExists == 0)
                {
                    _logger.LogInformation("Creating default admin user for PostgreSQL");

                    // PostgreSQL-compatible: Create admin user with proper snake_case columns and STRING enum values
                    var createAdminSql = @"
                        INSERT INTO users (
                            email, username, first_name, last_name, 
                            password_hash, created_at, updated_at, 
                            account_type, subscription_status, expertise_level,
                            is_email_confirmed
                        ) VALUES (
                            'admin@subexplore.com', 
                            'admin', 
                            'Admin', 
                            'System',
                            $1,
                            $2,
                            $2,
                            'administrator'::account_type,
                            'premium'::subscription_status,
                            'professional'::expertise_level,
                            true
                        )";

                    // 🔧 ENUM VALUES: account_type='administrator', subscription_status='premium', expertise_level='professional'
                    // Hash the password: Admin123!
                    var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
                    var createdAt = DateTime.UtcNow;

                    await _context.Database.ExecuteSqlRawAsync(createAdminSql, 
                        new Npgsql.NpgsqlParameter { Value = passwordHash },
                        new Npgsql.NpgsqlParameter { Value = createdAt });

                    _logger.LogInformation("✅ Default admin user created successfully: admin@subexplore.com / Admin123!");
                }
                else
                {
                    _logger.LogInformation("✅ Default admin user already exists");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating default admin user: {ErrorMessage}", ex.Message);
                // Don't throw - this is not critical for app functionality
            }
        }
    }
}