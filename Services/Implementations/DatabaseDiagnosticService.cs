using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Services.Interfaces;

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
                var results = new System.Text.StringBuilder();
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
    }
}