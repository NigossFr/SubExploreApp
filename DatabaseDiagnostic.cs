using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Services.Interfaces;
using SubExplore.Repositories.Interfaces;
using SubExplore.Models.Domain;
using System.Diagnostics;
using System.Linq;

namespace SubExplore
{
    /// <summary>
    /// Ultra-deep database diagnostic to identify database connectivity and data issues
    /// </summary>
    public static class DatabaseDiagnostic
    {
        public static async Task RunUltraDeepDatabaseTestAsync(IServiceProvider serviceProvider)
        {
            Debug.WriteLine("=== 🚨 ULTRA-DEEP DATABASE DIAGNOSTIC STARTING ===");
            
            try
            {
                // Test 1: Direct Database Context Test
                await TestDatabaseContextDirectly(serviceProvider);
                
                // Test 2: Connection String and Configuration Test
                await TestConnectionConfiguration(serviceProvider);
                
                // Test 3: Table Existence Test
                await TestTableExistence(serviceProvider);
                
                // Test 4: Data Existence Test
                await TestDataExistence(serviceProvider);
                
                // Test 5: Raw SQL Operations Test
                await TestRawSqlOperations(serviceProvider);
                
                // Test 6: Repository Layer Test
                await TestRepositoryOperations(serviceProvider);
                
                // Test 7: Service Layer Integration Test
                await TestServiceLayerIntegration(serviceProvider);
                
                Debug.WriteLine("=== ✅ ULTRA-DEEP DATABASE DIAGNOSTIC COMPLETED ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRITICAL DATABASE ERROR] Diagnostic failed: {ex.Message}");
                Debug.WriteLine($"[CRITICAL DATABASE ERROR] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[CRITICAL DATABASE ERROR] Inner exception: {ex.InnerException.Message}");
                }
            }
        }
        
        private static async Task TestDatabaseContextDirectly(IServiceProvider serviceProvider)
        {
            Debug.WriteLine("--- 🔍 TEST 1: DIRECT DATABASE CONTEXT TEST ---");
            
            try
            {
                var dbContext = serviceProvider.GetService<SubExploreDbContext>();
                Debug.WriteLine($"DbContext Service: {(dbContext != null ? "✓ Available" : "✗ NULL")}");
                
                if (dbContext == null)
                {
                    Debug.WriteLine("❌ CRITICAL: DbContext is NULL - DI container issue!");
                    return;
                }
                
                // Test database connection
                Debug.WriteLine("Testing database connection...");
                var canConnect = await dbContext.Database.CanConnectAsync();
                Debug.WriteLine($"Database Connection: {(canConnect ? "✓ Connected" : "✗ FAILED")}");
                
                if (!canConnect)
                {
                    Debug.WriteLine("❌ CRITICAL: Cannot connect to database!");
                    return;
                }
                
                // Test database name
                var databaseName = dbContext.Database.GetDbConnection().Database;
                Debug.WriteLine($"Database Name: {databaseName}");
                
                // Test connection string (masked)
                var connectionString = dbContext.Database.GetDbConnection().ConnectionString;
                var maskedConnectionString = connectionString?.Replace("password=seb09081980", "password=***");
                Debug.WriteLine($"Connection String: {maskedConnectionString}");
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DATABASE CONTEXT ERROR] {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[DATABASE CONTEXT ERROR] Inner: {ex.InnerException.Message}");
                }
            }
        }
        
        private static async Task TestConnectionConfiguration(IServiceProvider serviceProvider)
        {
            Debug.WriteLine("--- 🔍 TEST 2: CONNECTION CONFIGURATION TEST ---");
            
            try
            {
                var configuration = serviceProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                Debug.WriteLine($"Configuration Service: {(configuration != null ? "✓ Available" : "✗ NULL")}");
                
                if (configuration != null)
                {
                    var defaultConn = configuration["ConnectionStrings:DefaultConnection"];
                    var maskedConn = defaultConn?.Replace("password=seb09081980", "password=***");
                    Debug.WriteLine($"DefaultConnection: {(string.IsNullOrEmpty(defaultConn) ? "✗ NOT FOUND" : "✓ Found")}");
                    Debug.WriteLine($"Connection Details: {maskedConn}");
                    
                    var androidEmulatorConn = configuration["ConnectionStrings:AndroidEmulatorConnection"];
                    Debug.WriteLine($"AndroidEmulatorConnection: {(string.IsNullOrEmpty(androidEmulatorConn) ? "✗ NOT FOUND" : "✓ Found")}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CONNECTION CONFIG ERROR] {ex.Message}");
            }
        }
        
        private static async Task TestTableExistence(IServiceProvider serviceProvider)
        {
            Debug.WriteLine("--- 🔍 TEST 3: TABLE EXISTENCE TEST ---");
            
            try
            {
                var dbContext = serviceProvider.GetService<SubExploreDbContext>();
                if (dbContext == null) return;
                
                // Check critical tables
                var tables = new[] { "Users", "Spots", "SpotTypes", "UserFavoriteSpots", "RevokedTokens", "SpotMedia" };
                
                foreach (var tableName in tables)
                {
                    try
                    {
                        var query = $@"
                            SELECT COUNT(*) as Value
                            FROM information_schema.tables 
                            WHERE table_schema = DATABASE() 
                            AND table_name = '{tableName}'";
                        
                        var exists = await dbContext.Database.SqlQueryRaw<int>(query).FirstOrDefaultAsync();
                        Debug.WriteLine($"Table '{tableName}': {(exists > 0 ? "✓ EXISTS" : "✗ MISSING")}");
                    }
                    catch (Exception tableEx)
                    {
                        Debug.WriteLine($"Table '{tableName}': ✗ ERROR - {tableEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TABLE EXISTENCE ERROR] {ex.Message}");
            }
        }
        
        private static async Task TestDataExistence(IServiceProvider serviceProvider)
        {
            Debug.WriteLine("--- 🔍 TEST 4: DATA EXISTENCE TEST ---");
            
            try
            {
                var dbContext = serviceProvider.GetService<SubExploreDbContext>();
                if (dbContext == null) return;
                
                // Check data in critical tables
                try
                {
                    var userCount = await dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM Users").FirstOrDefaultAsync();
                    Debug.WriteLine($"Users table: {userCount} records");
                    
                    if (userCount > 0)
                    {
                        var adminUser = await dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM Users WHERE Email = 'admin@subexplore.com'").FirstOrDefaultAsync();
                        Debug.WriteLine($"Admin user exists: {(adminUser > 0 ? "✓ YES" : "✗ NO")}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Users data check: ✗ ERROR - {ex.Message}");
                }
                
                try
                {
                    var spotCount = await dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM Spots").FirstOrDefaultAsync();
                    Debug.WriteLine($"Spots table: {spotCount} records");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Spots data check: ✗ ERROR - {ex.Message}");
                }
                
                try
                {
                    var spotTypeCount = await dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM SpotTypes").FirstOrDefaultAsync();
                    Debug.WriteLine($"SpotTypes table: {spotTypeCount} records");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SpotTypes data check: ✗ ERROR - {ex.Message}");
                }
                
                try
                {
                    var favoritesCount = await dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM UserFavoriteSpots").FirstOrDefaultAsync();
                    Debug.WriteLine($"UserFavoriteSpots table: {favoritesCount} records");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"UserFavoriteSpots data check: ✗ ERROR - {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DATA EXISTENCE ERROR] {ex.Message}");
            }
        }
        
        private static async Task TestRawSqlOperations(IServiceProvider serviceProvider)
        {
            Debug.WriteLine("--- 🔍 TEST 5: RAW SQL OPERATIONS TEST ---");
            
            try
            {
                var dbContext = serviceProvider.GetService<SubExploreDbContext>();
                if (dbContext == null) return;
                
                // Test basic SQL operations
                try
                {
                    var currentTime = await dbContext.Database.SqlQueryRaw<DateTime>("SELECT NOW() as Value").FirstOrDefaultAsync();
                    Debug.WriteLine($"SQL NOW() query: ✓ SUCCESS - {currentTime}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SQL NOW() query: ✗ ERROR - {ex.Message}");
                }
                
                try
                {
                    var databaseName = await dbContext.Database.SqlQueryRaw<string>("SELECT DATABASE() as Value").FirstOrDefaultAsync();
                    Debug.WriteLine($"SQL DATABASE() query: ✓ SUCCESS - {databaseName}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SQL DATABASE() query: ✗ ERROR - {ex.Message}");
                }
                
                try
                {
                    var version = await dbContext.Database.SqlQueryRaw<string>("SELECT VERSION() as Value").FirstOrDefaultAsync();
                    Debug.WriteLine($"SQL VERSION() query: ✓ SUCCESS - {version}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SQL VERSION() query: ✗ ERROR - {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RAW SQL ERROR] {ex.Message}");
            }
        }
        
        private static async Task TestRepositoryOperations(IServiceProvider serviceProvider)
        {
            Debug.WriteLine("--- 🔍 TEST 6: REPOSITORY OPERATIONS TEST ---");
            
            try
            {
                // Test User Repository
                var userRepo = serviceProvider.GetService<IUserRepository>();
                Debug.WriteLine($"UserRepository: {(userRepo != null ? "✓ Available" : "✗ NULL")}");
                
                if (userRepo != null)
                {
                    try
                    {
                        var users = await userRepo.GetAllAsync();
                        Debug.WriteLine($"UserRepository.GetAllAsync(): ✓ SUCCESS - {users.Count()} users");
                        
                        if (users.Any())
                        {
                            var firstUser = users.First();
                            Debug.WriteLine($"First user: ID={firstUser.Id}, Email={firstUser.Email}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"UserRepository.GetAllAsync(): ✗ ERROR - {ex.Message}");
                    }
                }
                
                // Test Spot Repository
                var spotRepo = serviceProvider.GetService<ISpotRepository>();
                Debug.WriteLine($"SpotRepository: {(spotRepo != null ? "✓ Available" : "✗ NULL")}");
                
                if (spotRepo != null)
                {
                    try
                    {
                        var spots = await spotRepo.GetAllAsync();
                        Debug.WriteLine($"SpotRepository.GetAllAsync(): ✓ SUCCESS - {spots.Count()} spots");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"SpotRepository.GetAllAsync(): ✗ ERROR - {ex.Message}");
                    }
                }
                
                // Test UserFavoriteSpot Repository
                var favRepo = serviceProvider.GetService<IUserFavoriteSpotRepository>();
                Debug.WriteLine($"UserFavoriteSpotRepository: {(favRepo != null ? "✓ Available" : "✗ NULL")}");
                
                if (favRepo != null)
                {
                    try
                    {
                        var favorites = await favRepo.GetUserFavoritesAsync(1);
                        Debug.WriteLine($"UserFavoriteSpotRepository.GetUserFavoritesAsync(1): ✓ SUCCESS - {favorites.Count()} favorites");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"UserFavoriteSpotRepository.GetUserFavoritesAsync(1): ✗ ERROR - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[REPOSITORY OPERATIONS ERROR] {ex.Message}");
            }
        }
        
        private static async Task TestServiceLayerIntegration(IServiceProvider serviceProvider)
        {
            Debug.WriteLine("--- 🔍 TEST 7: SERVICE LAYER INTEGRATION TEST ---");
            
            try
            {
                // Test FavoriteSpotService
                var favService = serviceProvider.GetService<IFavoriteSpotService>();
                Debug.WriteLine($"FavoriteSpotService: {(favService != null ? "✓ Available" : "✗ NULL")}");
                
                if (favService != null)
                {
                    try
                    {
                        var isFavorited = await favService.IsSpotFavoritedAsync(1, 1);
                        Debug.WriteLine($"FavoriteSpotService.IsSpotFavoritedAsync(1,1): ✓ SUCCESS - {isFavorited}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"FavoriteSpotService.IsSpotFavoritedAsync(1,1): ✗ ERROR - {ex.Message}");
                    }
                }
                
                // Test SpotService  
                var spotService = serviceProvider.GetService<ISpotService>();
                Debug.WriteLine($"SpotService: {(spotService != null ? "✓ Available" : "✗ NULL")}");
                
                if (spotService != null)
                {
                    try
                    {
                        var isFavorite = await spotService.IsSpotFavoriteAsync(1, 1);
                        Debug.WriteLine($"SpotService.IsSpotFavoriteAsync(1,1): ✓ SUCCESS - {isFavorite}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"SpotService.IsSpotFavoriteAsync(1,1): ✗ ERROR - {ex.Message}");
                    }
                }
                
                // Test WeatherService
                var weatherService = serviceProvider.GetService<IWeatherService>();
                Debug.WriteLine($"WeatherService: {(weatherService != null ? "✓ Available" : "✗ NULL")}");
                
                if (weatherService != null)
                {
                    try
                    {
                        var isAvailable = await weatherService.IsServiceAvailableAsync();
                        Debug.WriteLine($"WeatherService.IsServiceAvailableAsync(): ✓ SUCCESS - {isAvailable}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"WeatherService.IsServiceAvailableAsync(): ✗ ERROR - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SERVICE LAYER ERROR] {ex.Message}");
            }
        }
    }
}