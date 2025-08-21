using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Services.Interfaces;
using SubExplore.Repositories.Interfaces;
using System.Text;

namespace SubExplore.Services.Implementations
{
    public class DatabaseDiagnosticService
    {
        private readonly SubExploreDbContext _context;
        private readonly ILogger<DatabaseDiagnosticService> _logger;
        private readonly IDatabaseService _databaseService;

        public DatabaseDiagnosticService(SubExploreDbContext context, ILogger<DatabaseDiagnosticService> logger, IDatabaseService databaseService)
        {
            _context = context;
            _logger = logger;
            _databaseService = databaseService;
        }

        public async Task<string> GetDetailedDatabaseStatusAsync()
        {
            try
            {
                var results = new StringBuilder();
                results.AppendLine("=== DIAGNOSTIC DÉTAILLÉ DE LA BASE DE DONNÉES ===");
                results.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                results.AppendLine();

                // Test de connexion
                var canConnect = await _context.Database.CanConnectAsync();
                results.AppendLine($"🔗 CONNEXION: {(canConnect ? "✅ OK" : "❌ ÉCHEC")}");
                results.AppendLine();

                if (canConnect)
                {
                    // Compter tous les types (actifs et inactifs)
                    var allSpotTypes = await _context.SpotTypes.ToListAsync();
                    var activeSpotTypes = allSpotTypes.Where(st => st.IsActive).ToList();
                    var inactiveSpotTypes = allSpotTypes.Where(st => !st.IsActive).ToList();

                    results.AppendLine($"📊 TYPES DE SPOTS:");
                    results.AppendLine($"   Total: {allSpotTypes.Count}");
                    results.AppendLine($"   Actifs: {activeSpotTypes.Count}");
                    results.AppendLine($"   Inactifs: {inactiveSpotTypes.Count}");
                    results.AppendLine();

                    // Lister tous les types actifs avec détails
                    if (activeSpotTypes.Any())
                    {
                        results.AppendLine("🏷️ TYPES ACTIFS DÉTAILLÉS:");
                        foreach (var type in activeSpotTypes.OrderBy(t => t.Name))
                        {
                            results.AppendLine($"   • {type.Name}");
                            results.AppendLine($"     Couleur: {type.ColorCode}");
                            results.AppendLine($"     Catégorie: {type.Category}");
                            results.AppendLine($"     ID: {type.Id}");
                            results.AppendLine();
                        }
                    }

                    // Lister tous les types inactifs
                    if (inactiveSpotTypes.Any())
                    {
                        results.AppendLine("🗑️ TYPES INACTIFS:");
                        foreach (var type in inactiveSpotTypes.OrderBy(t => t.Name))
                        {
                            results.AppendLine($"   • {type.Name} (ID: {type.Id})");
                        }
                        results.AppendLine();
                    }

                    // Compter les spots
                    var totalSpots = await _context.Spots.CountAsync();
                    var approvedSpots = await _context.Spots.CountAsync(s => s.ValidationStatus == Models.Enums.SpotValidationStatus.Approved);
                    var totalUsers = await _context.Users.CountAsync();

                    results.AppendLine($"🏖️ SPOTS: Total: {totalSpots}, Approuvés: {approvedSpots}");
                    results.AppendLine($"👥 UTILISATEURS: {totalUsers}");
                    results.AppendLine();

                    // Test des extensions de catégorie
                    results.AppendLine("🔍 TEST DES EXTENSIONS DE CATÉGORIE:");
                    
                    foreach (var category in new[] { "Activités", "Structures", "Boutiques" })
                    {
                        var categoryTypes = activeSpotTypes.Where(st => 
                            Helpers.Extensions.SpotTypeExtensions.SpotCategories.ContainsKey(category) &&
                            Helpers.Extensions.SpotTypeExtensions.SpotCategories[category].Contains(st.Name)
                        ).ToList();
                        
                        results.AppendLine($"   {category}: {categoryTypes.Count} types");
                        foreach (var type in categoryTypes)
                        {
                            results.AppendLine($"     - {type.Name}");
                        }
                    }
                }

                results.AppendLine("=== FIN DU DIAGNOSTIC ===");
                return results.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du diagnostic de base de données");
                return $"❌ Erreur lors du diagnostic: {ex.Message}";
            }
        }

        public async Task<bool> ForceDataRecreationAsync()
        {
            try
            {
                _logger.LogInformation("🔄 Démarrage de la recréation forcée des données...");
                
                // Supprimer et recréer la base de données
                await _context.Database.EnsureDeletedAsync();
                await _context.Database.EnsureCreatedAsync();
                
                // Reseeder les données
                var seedResult = await _databaseService.SeedDatabaseAsync();
                
                _logger.LogInformation($"✅ Recréation terminée. Résultat: {seedResult}");
                return seedResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la recréation forcée");
                return false;
            }
        }

        /// <summary>
        /// Ultra-deep database diagnostic combining all diagnostic functionality
        /// Replaces the static DatabaseDiagnostic.RunUltraDeepDatabaseTestAsync
        /// </summary>
        public async Task<string> RunUltraDeepDiagnosticAsync()
        {
            var results = new StringBuilder();
            results.AppendLine("=== 🚨 ULTRA-DEEP DATABASE DIAGNOSTIC ===");
            results.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            results.AppendLine();

            try
            {
                // Test 1: Database Context and Connection
                results.AppendLine("--- 🔍 TEST 1: DATABASE CONTEXT & CONNECTION ---");
                var canConnect = await _context.Database.CanConnectAsync();
                results.AppendLine($"Database Connection: {(canConnect ? "✅ Connected" : "❌ FAILED")}");
                
                if (canConnect)
                {
                    var databaseName = _context.Database.GetDbConnection().Database;
                    results.AppendLine($"Database Name: {databaseName}");
                    
                    var connectionString = _context.Database.GetDbConnection().ConnectionString;
                    var maskedConnectionString = MaskConnectionString(connectionString);
                    results.AppendLine($"Connection String: {maskedConnectionString}");
                }
                results.AppendLine();

                if (!canConnect)
                {
                    results.AppendLine("❌ CRITICAL: Cannot proceed with further tests - database connection failed!");
                    return results.ToString();
                }

                // Test 2: Table Existence
                results.AppendLine("--- 📊 TEST 2: TABLE EXISTENCE ---");
                try
                {
                    var spotTypesCount = await _context.SpotTypes.CountAsync();
                    var spotsCount = await _context.Spots.CountAsync();
                    var usersCount = await _context.Users.CountAsync();
                    
                    results.AppendLine($"SpotTypes table: ✅ ({spotTypesCount} records)");
                    results.AppendLine($"Spots table: ✅ ({spotsCount} records)");
                    results.AppendLine($"Users table: ✅ ({usersCount} records)");
                }
                catch (Exception ex)
                {
                    results.AppendLine($"❌ Table access error: {ex.Message}");
                }
                results.AppendLine();

                // Test 3: Data Integrity
                results.AppendLine("--- 🔍 TEST 3: DATA INTEGRITY ---");
                try
                {
                    var allSpotTypes = await _context.SpotTypes.ToListAsync();
                    var activeSpotTypes = allSpotTypes.Where(st => st.IsActive).ToList();
                    var orphanedSpots = await _context.Spots
                        .Where(s => s.Type == null || !s.Type.IsActive)
                        .CountAsync();

                    results.AppendLine($"Total SpotTypes: {allSpotTypes.Count}");
                    results.AppendLine($"Active SpotTypes: {activeSpotTypes.Count}");
                    results.AppendLine($"Orphaned Spots: {orphanedSpots}");
                    
                    if (orphanedSpots > 0)
                    {
                        results.AppendLine("⚠️ WARNING: Found spots with inactive or missing spot types!");
                    }
                }
                catch (Exception ex)
                {
                    results.AppendLine($"❌ Data integrity check error: {ex.Message}");
                }
                results.AppendLine();

                // Test 4: Raw SQL Operations
                results.AppendLine("--- ⚡ TEST 4: RAW SQL OPERATIONS ---");
                try
                {
                    var rawResult = await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                    results.AppendLine($"Raw SQL test: ✅ Success (result: {rawResult})");
                }
                catch (Exception ex)
                {
                    results.AppendLine($"❌ Raw SQL error: {ex.Message}");
                }
                results.AppendLine();

                // Test 5: Performance Test
                results.AppendLine("--- ⚡ TEST 5: PERFORMANCE TEST ---");
                try
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var testQuery = await _context.SpotTypes.Take(10).ToListAsync();
                    stopwatch.Stop();
                    
                    results.AppendLine($"Query performance: ✅ {stopwatch.ElapsedMilliseconds}ms for 10 records");
                    
                    if (stopwatch.ElapsedMilliseconds > 1000)
                    {
                        results.AppendLine("⚠️ WARNING: Slow query performance detected!");
                    }
                }
                catch (Exception ex)
                {
                    results.AppendLine($"❌ Performance test error: {ex.Message}");
                }
                results.AppendLine();

                // Include detailed status
                results.AppendLine("--- 📋 DETAILED STATUS ---");
                var detailedStatus = await GetDetailedDatabaseStatusAsync();
                results.AppendLine(detailedStatus);

                results.AppendLine("=== ✅ ULTRA-DEEP DIAGNOSTIC COMPLETED ===");
                
                _logger.LogInformation("Ultra-deep database diagnostic completed successfully");
            }
            catch (Exception ex)
            {
                results.AppendLine($"❌ CRITICAL ERROR during ultra-deep diagnostic: {ex.Message}");
                _logger.LogError(ex, "Ultra-deep database diagnostic failed");
            }

            return results.ToString();
        }

        /// <summary>
        /// Masks sensitive information in connection strings for secure logging
        /// </summary>
        private static string MaskConnectionString(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "❌ NOT SET";

            // Mask common password patterns
            var masked = connectionString;
            
            // Pattern: Password=value; or Password=value (end of string)
            masked = System.Text.RegularExpressions.Regex.Replace(
                masked, 
                @"(Password=)[^;]+", 
                "$1***", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // Pattern: pwd=value; or pwd=value (end of string) 
            masked = System.Text.RegularExpressions.Regex.Replace(
                masked, 
                @"(pwd=)[^;]+", 
                "$1***", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Pattern: User Id=value or User ID=value
            masked = System.Text.RegularExpressions.Regex.Replace(
                masked, 
                @"(User Id?=)[^;]+", 
                match => $"{match.Groups[1].Value}{MaskValue(match.Value.Substring(match.Groups[1].Value.Length), false)}", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return masked;
        }

        /// <summary>
        /// Masks a value for secure logging
        /// </summary>
        private static string MaskValue(string value, bool isSecret)
        {
            if (string.IsNullOrEmpty(value))
                return "❌ NOT SET";

            if (!isSecret)
                return value;

            if (value.Length <= 4)
                return "***";

            // Show first 2 and last 2 characters
            return $"{value.Substring(0, 2)}***{value.Substring(value.Length - 2)}";
        }
    }
}