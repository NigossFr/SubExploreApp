using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Helpers
{
    /// <summary>
    /// Outil de diagnostic et de réparation pour le système de spots
    /// </summary>
    public static class SpotSystemDiagnosticTool
    {
        public static async Task<SpotSystemDiagnostic> RunFullDiagnosticAsync(IServiceProvider serviceProvider)
        {
            var diagnostic = new SpotSystemDiagnostic();
            
            try
            {
                var dbContext = serviceProvider.GetService<SubExploreDbContext>();
                if (dbContext == null)
                {
                    diagnostic.AddError("CRITICAL: DbContext not available");
                    return diagnostic;
                }

                System.Diagnostics.Debug.WriteLine("🔍 [DIAGNOSTIC] Starting comprehensive spot system analysis...");

                // 1. Analyser les types de spots actuels
                await AnalyzeSpotTypes(dbContext, diagnostic);
                
                // 2. Analyser la répartition des spots
                await AnalyzeSpotDistribution(dbContext, diagnostic);
                
                // 3. Vérifier les données des spots
                await AnalyzeSpotData(dbContext, diagnostic);
                
                // 4. Tester les requêtes de filtrage
                await TestFilteringQueries(dbContext, diagnostic);

                diagnostic.GenerateReport();
                System.Diagnostics.Debug.WriteLine("✅ [DIAGNOSTIC] Analysis complete");
                
                return diagnostic;
            }
            catch (Exception ex)
            {
                diagnostic.AddError($"CRITICAL: Diagnostic failed with exception: {ex.Message}");
                return diagnostic;
            }
        }

        private static async Task AnalyzeSpotTypes(SubExploreDbContext dbContext, SpotSystemDiagnostic diagnostic)
        {
            System.Diagnostics.Debug.WriteLine("📋 [DIAGNOSTIC] Analyzing spot types...");
            
            var spotTypes = await dbContext.SpotTypes.ToListAsync();
            diagnostic.TotalSpotTypes = spotTypes.Count;
            diagnostic.ActiveSpotTypes = spotTypes.Where(st => st.IsActive).Count();
            
            System.Diagnostics.Debug.WriteLine($"📋 Total spot types: {diagnostic.TotalSpotTypes}");
            System.Diagnostics.Debug.WriteLine($"📋 Active spot types: {diagnostic.ActiveSpotTypes}");
            
            // Analyser la répartition par catégories
            var categoryDistribution = spotTypes
                .Where(st => st.IsActive)
                .GroupBy(st => st.Category)
                .ToDictionary(g => g.Key, g => g.Count());
                
            diagnostic.SpotTypesByCategory = categoryDistribution;
            
            System.Diagnostics.Debug.WriteLine("📊 Répartition des types par catégorie:");
            foreach (var category in categoryDistribution)
            {
                System.Diagnostics.Debug.WriteLine($"   - {category.Key}: {category.Value} types");
            }
            
            // Lister tous les types de spots avec détails
            System.Diagnostics.Debug.WriteLine("📝 Détail des types de spots:");
            foreach (var spotType in spotTypes.Where(st => st.IsActive))
            {
                System.Diagnostics.Debug.WriteLine($"   - ID:{spotType.Id} | {spotType.Name} | Catégorie: {spotType.Category} | Couleur: {spotType.ColorCode}");
            }
        }

        private static async Task AnalyzeSpotDistribution(SubExploreDbContext dbContext, SpotSystemDiagnostic diagnostic)
        {
            System.Diagnostics.Debug.WriteLine("🗺️ [DIAGNOSTIC] Analyzing spot distribution...");
            
            var spots = await dbContext.Spots
                .Include(s => s.Type)
                .ToListAsync();
                
            diagnostic.TotalSpots = spots.Count;
            diagnostic.ApprovedSpots = spots.Where(s => s.ValidationStatus == SpotValidationStatus.Approved).Count();
            
            System.Diagnostics.Debug.WriteLine($"🗺️ Total spots: {diagnostic.TotalSpots}");
            System.Diagnostics.Debug.WriteLine($"🗺️ Approved spots: {diagnostic.ApprovedSpots}");
            
            // Analyser la répartition par type de spot
            var spotsByType = spots
                .Where(s => s.Type != null)
                .GroupBy(s => new { s.Type.Id, s.Type.Name, s.Type.Category })
                .Select(g => new { 
                    TypeInfo = g.Key, 
                    Count = g.Count(),
                    ApprovedCount = g.Count(s => s.ValidationStatus == SpotValidationStatus.Approved)
                })
                .OrderByDescending(x => x.Count)
                .ToList();
                
            System.Diagnostics.Debug.WriteLine("📊 Répartition des spots par type:");
            foreach (var group in spotsByType)
            {
                System.Diagnostics.Debug.WriteLine($"   - {group.TypeInfo.Name} (ID:{group.TypeInfo.Id}, {group.TypeInfo.Category}): {group.Count} total ({group.ApprovedCount} approuvés)");
            }
            
            // Analyser la répartition par catégorie d'activité
            var spotsByCategory = spots
                .Where(s => s.Type != null)
                .GroupBy(s => s.Type.Category)
                .Select(g => new { 
                    Category = g.Key, 
                    Count = g.Count(),
                    ApprovedCount = g.Count(s => s.ValidationStatus == SpotValidationStatus.Approved),
                    Types = g.Select(s => s.Type.Name).Distinct().ToList()
                })
                .ToList();
                
            diagnostic.SpotsByCategory = spotsByCategory.ToDictionary(x => x.Category, x => x.ApprovedCount);
            
            System.Diagnostics.Debug.WriteLine("🎯 Répartition des spots par catégorie d'activité:");
            foreach (var group in spotsByCategory)
            {
                System.Diagnostics.Debug.WriteLine($"   - {group.Category}: {group.Count} total ({group.ApprovedCount} approuvés)");
                System.Diagnostics.Debug.WriteLine($"     Types: {string.Join(", ", group.Types)}");
            }
            
            // Identifier les problèmes
            if (spotsByCategory.Count == 1)
            {
                diagnostic.AddError($"MAJOR ISSUE: Tous les spots sont dans une seule catégorie ({spotsByCategory.First().Category})");
            }
            
            if (spotsByCategory.Any(g => g.ApprovedCount == 0))
            {
                var emptyCategories = spotsByCategory.Where(g => g.ApprovedCount == 0).Select(g => g.Category);
                diagnostic.AddWarning($"Des catégories n'ont aucun spot approuvé: {string.Join(", ", emptyCategories)}");
            }
        }

        private static async Task AnalyzeSpotData(SubExploreDbContext dbContext, SpotSystemDiagnostic diagnostic)
        {
            System.Diagnostics.Debug.WriteLine("📍 [DIAGNOSTIC] Analyzing individual spot data...");
            
            var spots = await dbContext.Spots
                .Include(s => s.Type)
                .Include(s => s.Creator)
                .Take(10) // Analyser les 10 premiers spots pour éviter trop de logs
                .ToListAsync();
                
            System.Diagnostics.Debug.WriteLine("🔍 Détail des premiers spots:");
            foreach (var spot in spots)
            {
                var typeInfo = spot.Type != null ? $"{spot.Type.Name} ({spot.Type.Category})" : "NO TYPE";
                var creatorInfo = spot.Creator != null ? spot.Creator.Username : "NO CREATOR";
                
                System.Diagnostics.Debug.WriteLine($"   - ID:{spot.Id} | {spot.Name} | Type: {typeInfo} | Créateur: {creatorInfo} | Status: {spot.ValidationStatus}");
            }
            
            // Vérifier les spots orphelins
            var spotsWithoutType = await dbContext.Spots.Where(s => s.Type == null).CountAsync();
            var spotsWithoutCreator = await dbContext.Spots.Where(s => s.Creator == null).CountAsync();
            
            if (spotsWithoutType > 0)
            {
                diagnostic.AddError($"CRITICAL: {spotsWithoutType} spots sans type défini");
            }
            
            if (spotsWithoutCreator > 0)
            {
                diagnostic.AddWarning($"{spotsWithoutCreator} spots sans créateur défini");
            }
        }

        private static async Task TestFilteringQueries(SubExploreDbContext dbContext, SpotSystemDiagnostic diagnostic)
        {
            System.Diagnostics.Debug.WriteLine("🔎 [DIAGNOSTIC] Testing filtering queries...");
            
            // Tester chaque catégorie d'activité (éviter les doublons des enum obsolètes)
            var uniqueCategories = new HashSet<ActivityCategory>
            {
                ActivityCategory.Activity,
                ActivityCategory.Structure, 
                ActivityCategory.Shop,
                ActivityCategory.Other
            };
            
            foreach (var category in uniqueCategories)
            {
                try
                {
                    var spotsInCategory = await dbContext.Spots
                        .Include(s => s.Type)
                        .Where(s => s.Type.Category == category && 
                                   s.Type.IsActive && 
                                   s.ValidationStatus == SpotValidationStatus.Approved)
                        .CountAsync();
                        
                    System.Diagnostics.Debug.WriteLine($"   - {category}: {spotsInCategory} spots filtrés");
                    
                    if (spotsInCategory == 0)
                    {
                        diagnostic.AddWarning($"Aucun spot trouvé pour la catégorie {category}");
                    }
                }
                catch (Exception ex)
                {
                    diagnostic.AddError($"Erreur lors du filtrage pour {category}: {ex.Message}");
                }
            }
            
            // Tester les requêtes de base
            try
            {
                var allApprovedSpots = await dbContext.Spots
                    .Where(s => s.ValidationStatus == SpotValidationStatus.Approved)
                    .CountAsync();
                    
                System.Diagnostics.Debug.WriteLine($"🔍 Total spots approuvés trouvés par requête: {allApprovedSpots}");
                diagnostic.FilteringWorksCorrectly = allApprovedSpots > 0;
            }
            catch (Exception ex)
            {
                diagnostic.AddError($"Erreur lors de la requête des spots approuvés: {ex.Message}");
                diagnostic.FilteringWorksCorrectly = false;
            }
        }

        /// <summary>
        /// Répare la répartition des spots en recréant une distribution équilibrée
        /// </summary>
        public static async Task<bool> RepairSpotDistributionAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var dbContext = serviceProvider.GetService<SubExploreDbContext>();
                if (dbContext == null) return false;

                System.Diagnostics.Debug.WriteLine("🔧 [REPAIR] Starting spot distribution repair...");

                // 1. S'assurer qu'on a des types de spots variés
                await EnsureVariedSpotTypesAsync(dbContext);
                
                // 2. Redistribuer les spots existants
                await RedistributeExistingSpotsAsync(dbContext);
                
                // 3. Créer des spots d'exemple si nécessaire
                await CreateExampleSpotsIfNeededAsync(dbContext);
                
                await dbContext.SaveChangesAsync();
                
                System.Diagnostics.Debug.WriteLine("✅ [REPAIR] Spot distribution repair completed");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [REPAIR] Repair failed: {ex.Message}");
                return false;
            }
        }

        private static async Task EnsureVariedSpotTypesAsync(SubExploreDbContext dbContext)
        {
            var spotTypes = await dbContext.SpotTypes.ToListAsync();
            
            // Vérifier qu'on a au moins un type par catégorie principale
            var requiredTypes = new[]
            {
                new { Category = ActivityCategory.Activity, Name = "Plongée bouteille", Color = "#0077BE", Icon = "marker_diving.png" },
                new { Category = ActivityCategory.Activity, Name = "Apnée", Color = "#4A90E2", Icon = "marker_freediving.png" },
                new { Category = ActivityCategory.Activity, Name = "Randonnée palmée", Color = "#87CEEB", Icon = "marker_snorkeling.png" },
                new { Category = ActivityCategory.Activity, Name = "Photo sous-marine", Color = "#5DADE2", Icon = "marker_photography.png" },
                new { Category = ActivityCategory.Other, Name = "Autres activités", Color = "#228B22", Icon = "marker_other.png" }
            };
            
            foreach (var required in requiredTypes)
            {
                if (!spotTypes.Any(st => st.Category == required.Category && st.IsActive))
                {
                    var newType = new SpotType
                    {
                        Name = required.Name,
                        Category = required.Category,
                        ColorCode = required.Color,
                        IconPath = required.Icon,
                        IsActive = true,
                        RequiresExpertValidation = required.Category == ActivityCategory.Activity || required.Category == ActivityCategory.Activity,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    dbContext.SpotTypes.Add(newType);
                    System.Diagnostics.Debug.WriteLine($"🔧 Created spot type: {required.Name} ({required.Category})");
                }
            }
        }

        private static async Task RedistributeExistingSpotsAsync(SubExploreDbContext dbContext)
        {
            var spots = await dbContext.Spots.Include(s => s.Type).ToListAsync();
            var spotTypes = await dbContext.SpotTypes.Where(st => st.IsActive).ToListAsync();
            
            if (spots.Count == 0 || spotTypes.Count == 0) return;
            
            // Si tous les spots sont du même type, les redistribuer
            var uniqueTypeIds = spots.Select(s => s.TypeId).Distinct().Count();
            
            if (uniqueTypeIds == 1)
            {
                System.Diagnostics.Debug.WriteLine("🔧 All spots are same type - redistributing...");
                
                var typesByCategory = spotTypes.GroupBy(st => st.Category).ToList();
                var random = new Random(42); // Seed fixe pour reproductibilité
                
                for (int i = 0; i < spots.Count; i++)
                {
                    var targetCategory = typesByCategory[i % typesByCategory.Count];
                    var randomType = targetCategory.ElementAt(random.Next(targetCategory.Count()));
                    
                    spots[i].TypeId = randomType.Id;
                    System.Diagnostics.Debug.WriteLine($"🔧 Reassigned spot '{spots[i].Name}' to type '{randomType.Name}' ({randomType.Category})");
                }
            }
        }

        private static async Task CreateExampleSpotsIfNeededAsync(SubExploreDbContext dbContext)
        {
            var spotCount = await dbContext.Spots.CountAsync();
            
            if (spotCount < 5)
            {
                System.Diagnostics.Debug.WriteLine("🔧 Creating example spots for better testing...");
                
                var spotTypes = await dbContext.SpotTypes.Where(st => st.IsActive).ToListAsync();
                var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                
                if (adminUser == null || spotTypes.Count == 0) return;
                
                var exampleSpots = new[]
                {
                    new { Name = "Calanque de Cassis", Category = ActivityCategory.Activity, Lat = 43.2148m, Lon = 5.5385m },
                    new { Name = "Piscine Y-40", Category = ActivityCategory.Activity, Lat = 45.1953m, Lon = 11.8453m },
                    new { Name = "Lagon de Moorea", Category = ActivityCategory.Activity, Lat = -17.5000m, Lon = -149.8333m },
                    new { Name = "Épave du Donator", Category = ActivityCategory.Activity, Lat = 43.1938m, Lon = 5.2661m },
                    new { Name = "Centre Marseille Plongée", Category = ActivityCategory.Other, Lat = 43.2965m, Lon = 5.3698m }
                };
                
                foreach (var example in exampleSpots)
                {
                    var spotType = spotTypes.FirstOrDefault(st => st.Category == example.Category);
                    if (spotType == null) continue;
                    
                    var existingSpot = await dbContext.Spots.FirstOrDefaultAsync(s => s.Name == example.Name);
                    if (existingSpot == null)
                    {
                        var newSpot = new Spot
                        {
                            Name = example.Name,
                            Description = $"Site d'exemple pour {example.Category}",
                            Latitude = example.Lat,
                            Longitude = example.Lon,
                            TypeId = spotType.Id,
                            CreatorId = adminUser.Id,
                            ValidationStatus = SpotValidationStatus.Approved,
                            DifficultyLevel = DifficultyLevel.Intermediate,
                            MaxDepth = 20,
                            CreatedAt = DateTime.UtcNow,
                            RequiredEquipment = "Équipement de base requis",
                            SafetyNotes = "Site sécurisé pour tous niveaux",
                            BestConditions = "Mer calme recommandée"
                        };
                        
                        dbContext.Spots.Add(newSpot);
                        System.Diagnostics.Debug.WriteLine($"🔧 Created example spot: {example.Name} ({example.Category})");
                    }
                }
            }
        }
    }

    public class SpotSystemDiagnostic
    {
        public int TotalSpotTypes { get; set; }
        public int ActiveSpotTypes { get; set; }
        public int TotalSpots { get; set; }
        public int ApprovedSpots { get; set; }
        public bool FilteringWorksCorrectly { get; set; }
        
        public Dictionary<ActivityCategory, int> SpotTypesByCategory { get; set; } = new();
        public Dictionary<ActivityCategory, int> SpotsByCategory { get; set; } = new();
        
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Info { get; set; } = new();
        
        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);
        public void AddInfo(string info) => Info.Add(info);
        
        public void GenerateReport()
        {
            System.Diagnostics.Debug.WriteLine("📋 === DIAGNOSTIC REPORT ===");
            System.Diagnostics.Debug.WriteLine($"Spot Types: {TotalSpotTypes} total, {ActiveSpotTypes} active");
            System.Diagnostics.Debug.WriteLine($"Spots: {TotalSpots} total, {ApprovedSpots} approved");
            System.Diagnostics.Debug.WriteLine($"Filtering: {(FilteringWorksCorrectly ? "✅ Working" : "❌ Broken")}");
            
            if (Errors.Any())
            {
                System.Diagnostics.Debug.WriteLine("🚨 ERRORS:");
                foreach (var error in Errors)
                    System.Diagnostics.Debug.WriteLine($"   - {error}");
            }
            
            if (Warnings.Any())
            {
                System.Diagnostics.Debug.WriteLine("⚠️ WARNINGS:");
                foreach (var warning in Warnings)
                    System.Diagnostics.Debug.WriteLine($"   - {warning}");
            }
            
            System.Diagnostics.Debug.WriteLine("📋 === END REPORT ===");
        }
        
        public bool IsHealthy => !Errors.Any() && FilteringWorksCorrectly && ApprovedSpots > 0;
    }
}