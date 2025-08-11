using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service pour diagnostiquer et réparer les problèmes de types de spots
    /// </summary>
    public class SpotTypeDiagnosticService
    {
        private readonly SubExploreDbContext _context;
        private readonly ILogger<SpotTypeDiagnosticService> _logger;

        public SpotTypeDiagnosticService(SubExploreDbContext context, ILogger<SpotTypeDiagnosticService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Diagnostique complet de l'état des types de spots et spots
        /// </summary>
        public async Task<string> DiagnoseSpotTypesAsync()
        {
            try
            {
                var report = "=== DIAGNOSTIC COMPLET DES TYPES DE SPOTS ===\n\n";

                // 1. Types de spots actifs
                var activeSpotTypes = await _context.SpotTypes
                    .Where(st => st.IsActive)
                    .OrderBy(st => st.Category)
                    .ThenBy(st => st.Name)
                    .ToListAsync();

                report += "📋 TYPES DE SPOTS ACTIFS:\n";
                foreach (var type in activeSpotTypes)
                {
                    var categoryName = type.Category switch
                    {
                        ActivityCategory.Activity => "Activités",
                        ActivityCategory.Structure => "Structures", 
                        ActivityCategory.Shop => "Boutiques",
                        ActivityCategory.Other => "Autres",
                        _ => $"Cat{(int)type.Category}"
                    };
                    report += $"  • {type.Name} (ID:{type.Id}) → {categoryName} ({(int)type.Category})\n";
                }

                // 2. Spots existants et leurs types
                var spots = await _context.Spots
                    .Include(s => s.Type)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                report += "\n📍 SPOTS ET LEURS TYPES:\n";
                foreach (var spot in spots)
                {
                    var typeName = spot.Type?.Name ?? "TYPE MANQUANT";
                    var typeActive = spot.Type?.IsActive ?? false;
                    var status = typeActive ? "✓" : "❌ INACTIF";
                    report += $"  • {spot.Name} → {typeName} (ID:{spot.TypeId}) {status}\n";
                }

                // 3. Analyse par catégorie pour le filtrage
                report += "\n🔍 ANALYSE DU FILTRAGE:\n";

                var activitesSpots = spots.Where(s => s.Type != null && s.Type.IsActive && 
                    (s.Type.Category == ActivityCategory.Activity ||
                     s.Type.Name.Contains("Plongée") || s.Type.Name.Contains("Apnée") || 
                     s.Type.Name.Contains("Randonnée") || s.Type.Name.Contains("Photo"))).ToList();
                report += $"  Activités: {activitesSpots.Count} spots\n";

                var structuresSpots = spots.Where(s => s.Type != null && s.Type.IsActive && 
                    (s.Type.Category == ActivityCategory.Structure ||
                     s.Type.Name.Contains("Club") || s.Type.Name.Contains("Professionnel") || 
                     s.Type.Name.Contains("Base"))).ToList();
                report += $"  Structures: {structuresSpots.Count} spots\n";

                var boutiquesSpots = spots.Where(s => s.Type != null && s.Type.IsActive && 
                    (s.Type.Category == ActivityCategory.Shop ||
                     s.Type.Name.Contains("Boutique"))).ToList();
                report += $"  Boutiques: {boutiquesSpots.Count} spots\n";

                // 4. Problèmes détectés
                report += "\n🚨 PROBLÈMES DÉTECTÉS:\n";
                var problems = 0;

                // Spots avec types inactifs
                var spotsWithInactiveTypes = spots.Where(s => s.Type == null || !s.Type.IsActive).ToList();
                if (spotsWithInactiveTypes.Any())
                {
                    problems++;
                    report += $"  ❌ {spotsWithInactiveTypes.Count} spots ont des types inactifs:\n";
                    foreach (var spot in spotsWithInactiveTypes)
                    {
                        report += $"     • {spot.Name} (TypeId: {spot.TypeId})\n";
                    }
                }

                // Types avec mauvaise catégorie
                var typesWithBadCategory = activeSpotTypes.Where(t => 
                    (t.Name.Contains("Boutique") && t.Category != ActivityCategory.Shop) ||
                    (t.Name.Contains("Club") && t.Category != ActivityCategory.Structure) ||
                    (t.Name.Contains("Plongée") && t.Category != ActivityCategory.Activity)).ToList();
                
                if (typesWithBadCategory.Any())
                {
                    problems++;
                    report += $"  ❌ {typesWithBadCategory.Count} types ont une catégorie incorrecte:\n";
                    foreach (var type in typesWithBadCategory)
                    {
                        report += $"     • {type.Name} est en catégorie {(int)type.Category} au lieu de la bonne\n";
                    }
                }

                if (problems == 0)
                {
                    report += "  ✅ Aucun problème détecté\n";
                }

                report += "\n=== FIN DU DIAGNOSTIC ===";
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du diagnostic des types de spots");
                return $"❌ Erreur lors du diagnostic: {ex.Message}";
            }
        }

        /// <summary>
        /// Répare automatiquement les problèmes de types de spots
        /// </summary>
        public async Task<bool> RepairSpotTypesAsync()
        {
            try
            {
                _logger.LogInformation("🔧 Début de la réparation des types de spots...");

                // 1. Identifier et désactiver les doublons
                var allTypes = await _context.SpotTypes.ToListAsync();
                var duplicateNames = allTypes.GroupBy(t => t.Name)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                foreach (var duplicateName in duplicateNames)
                {
                    var duplicates = allTypes.Where(t => t.Name == duplicateName).OrderByDescending(t => t.Id).ToList();
                    var keepType = duplicates.First(); // Garder le plus récent
                    var removeTypes = duplicates.Skip(1).ToList();

                    _logger.LogInformation($"Doublon détecté pour '{duplicateName}' - Garder ID:{keepType.Id}, Supprimer: {string.Join(",", removeTypes.Select(t => t.Id))}");

                    // Mettre à jour les spots qui pointent vers les anciens types
                    foreach (var oldType in removeTypes)
                    {
                        var spotsToUpdate = await _context.Spots.Where(s => s.TypeId == oldType.Id).ToListAsync();
                        foreach (var spot in spotsToUpdate)
                        {
                            spot.TypeId = keepType.Id;
                            _logger.LogInformation($"Spot '{spot.Name}' mis à jour: Type {oldType.Id} → {keepType.Id}");
                        }
                        
                        // Désactiver l'ancien type
                        oldType.IsActive = false;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Réparation des doublons terminée");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la réparation des types de spots");
                return false;
            }
        }
    }
}