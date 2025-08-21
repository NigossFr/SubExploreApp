using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Dedicated service for creating admin user - for debugging login issues
    /// </summary>
    public class AdminUserCreationService
    {
        private readonly SubExploreDbContext _context;
        private readonly ILogger<AdminUserCreationService> _logger;

        public AdminUserCreationService(SubExploreDbContext context, ILogger<AdminUserCreationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> ForceCreateAdminUserAsync()
        {
            try
            {
                _logger.LogInformation("🚨 FORCE ADMIN CREATION: Starting diagnostic admin user creation");
                
                // Check database connection
                var canConnect = await _context.Database.CanConnectAsync();
                _logger.LogInformation("🔍 Database connection: {CanConnect}", canConnect);
                
                if (!canConnect)
                {
                    _logger.LogError("❌ Cannot connect to database");
                    return false;
                }

                // Check if users table exists
                var tablesQuery = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = 'users'";
                
                var usersTableExists = await _context.Database.SqlQueryRaw<string>(tablesQuery).ToListAsync();
                _logger.LogInformation("🔍 Users table exists: {Exists}", usersTableExists.Any());

                if (!usersTableExists.Any())
                {
                    _logger.LogWarning("⚠️ Users table does not exist, creating database schema...");
                    var created = await _context.Database.EnsureCreatedAsync();
                    _logger.LogInformation("🔧 Database EnsureCreated result: {Created}", created);
                }

                // Check for existing admin user
                var existingAdmin = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == "admin@subexplore.com");
                
                if (existingAdmin != null)
                {
                    _logger.LogInformation("✅ Admin user already exists: {AdminId}", existingAdmin.Id);
                    _logger.LogInformation("🔍 Admin email confirmed: {IsConfirmed}", existingAdmin.IsEmailConfirmed);
                    return true;
                }

                _logger.LogInformation("🔧 Creating admin user...");

                // Create admin user using Entity Framework (not raw SQL)
                var adminUser = new User
                {
                    Email = "admin@subexplore.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Username = "admin",
                    FirstName = "Admin",
                    LastName = "System",
                    AccountType = AccountType.Administrator,
                    SubscriptionStatus = SubscriptionStatus.Premium,
                    ExpertiseLevel = ExpertiseLevel.Professional,
                    IsEmailConfirmed = true, // 🔑 CRITICAL for login
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Preferences = new UserPreferences
                    {
                        Theme = "dark",
                        DisplayNamePreference = "username",
                        NotificationSettings = new Dictionary<string, object>
                        {
                            { "SpotValidations", true },
                            { "NewSpots", true },
                            { "Comments", true }
                        },
                        Language = "fr",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("🎉 Admin user created successfully!");
                _logger.LogInformation("📧 Email: admin@subexplore.com");
                _logger.LogInformation("🔑 Password: Admin123!");
                _logger.LogInformation("✅ Email Confirmed: {IsConfirmed}", adminUser.IsEmailConfirmed);
                _logger.LogInformation("👤 User ID: {UserId}", adminUser.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create admin user: {ErrorMessage}", ex.Message);
                return false;
            }
        }

        public async Task<string> DiagnoseAdminUserAsync()
        {
            try
            {
                var result = "=== ADMIN USER DIAGNOSTIC ===\n";
                
                // Check connection
                var canConnect = await _context.Database.CanConnectAsync();
                result += $"Database Connection: {(canConnect ? "✅ OK" : "❌ FAILED")}\n";
                
                if (!canConnect) return result;

                // Check users table
                var usersCount = await _context.Users.CountAsync();
                result += $"Total Users: {usersCount}\n";

                // Check admin user specifically
                var adminUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == "admin@subexplore.com");
                
                if (adminUser != null)
                {
                    result += $"Admin User: ✅ FOUND\n";
                    result += $"  - ID: {adminUser.Id}\n";
                    result += $"  - Email: {adminUser.Email}\n";
                    result += $"  - Username: {adminUser.Username}\n";
                    result += $"  - Email Confirmed: {(adminUser.IsEmailConfirmed ? "✅ YES" : "❌ NO")}\n";
                    result += $"  - Account Type: {adminUser.AccountType}\n";
                    result += $"  - Created: {adminUser.CreatedAt}\n";
                }
                else
                {
                    result += "Admin User: ❌ NOT FOUND\n";
                }

                result += "=== END DIAGNOSTIC ===";
                return result;
            }
            catch (Exception ex)
            {
                return $"❌ Diagnostic failed: {ex.Message}";
            }
        }
    }
}