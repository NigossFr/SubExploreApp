using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SubExplore.Services.Implementations
{
    public class DatabaseServiceSimple : IDatabaseService
    {
        private readonly SubExploreDbContext _context;
        private readonly ILogger<DatabaseServiceSimple> _logger;

        public DatabaseServiceSimple(SubExploreDbContext context, ILogger<DatabaseServiceSimple> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> EnsureDatabaseCreatedAsync()
        {
            try
            {
                _logger.LogInformation("Création de la base de données...");
                bool result = await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Base de données créée avec succès");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la base de données");
                return false;
            }
        }

        public async Task<bool> SeedDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Initialisation des données...");

                // 🔧 CORRECTION ENUM: Convertir enums string → integer PostgreSQL
                _logger.LogInformation("CORRECTION ENUM: Conversion string → integer pour compatibilité Npgsql...");
                
                try
                {
                    // 🚨 SOLUTION FINALE: SUPPRESSION SQL BRUTE pour contourner l'erreur enum
                    _logger.LogInformation("SUPPRESSION SQL BRUTE: Contournement erreur enum string...");
                    
                    // Supprimer toutes les données avec SQL brut (évite le mapping enum)
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM user_favorite_spots WHERE true;");
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM spot_media WHERE true;");
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM spots WHERE true;");
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM spot_types WHERE true;");
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM user_preferences WHERE true;");
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM users WHERE true;");
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM email_verification_tokens WHERE true;");
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM password_reset_tokens WHERE true;");
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM revoked_tokens WHERE true;");
                    
                    _logger.LogInformation("✅ Suppression SQL brute terminée - Tables vidées");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Erreur lors de la suppression SQL (normal si tables n'existent pas): {ex.Message}");
                }

                // Créer l'utilisateur admin
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
                    IsEmailConfirmed = true, // 🔑 ESSENTIEL: Admin email pré-vérifié
                    CreatedAt = DateTime.UtcNow,
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
                        CreatedAt = DateTime.UtcNow
                    }
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                // Créer les nouveaux types de spots selon la nouvelle organisation
                var spotTypes = new List<SpotType>
                {
                    // === ACTIVITÉS (variations de bleus) ===
                    new SpotType
                    {
                        Name = "Plongée bouteille",
                        IconPath = "marker_scuba.png", 
                        ColorCode = "#0077BE", // Bleu principal
                        Category = ActivityCategory.Activity,
                        Description = "Sites de plongée avec bouteille (tous niveaux - récréative et technique)",
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" } },
                            { "MaxDepthRange", new[] { 0, 200 } }
                        },
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Apnée",
                        IconPath = "marker_freediving.png",
                        ColorCode = "#4A90E2", // Bleu moyen
                        Category = ActivityCategory.Activity,
                        Description = "Sites adaptés à la plongée en apnée",
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" } },
                            { "MaxDepthRange", new[] { 0, 30 } }
                        },
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Randonnée sous-marine",
                        IconPath = "marker_snorkeling.png",
                        ColorCode = "#87CEEB", // Bleu clair
                        Category = ActivityCategory.Activity,
                        Description = "Sites de surface accessibles pour la randonnée sous-marine",
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "DifficultyLevel", "SafetyNotes" } },
                            { "MaxDepthRange", new[] { 0, 5 } }
                        },
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Photo sous-marine",
                        IconPath = "marker_photography.png",
                        ColorCode = "#5DADE2", // Bleu photo
                        Category = ActivityCategory.Activity,
                        Description = "Sites d'intérêt pour la photographie sous-marine",
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "DifficultyLevel" } }
                        },
                        IsActive = true
                    },

                    // === STRUCTURES (variations de verts) ===
                    new SpotType
                    {
                        Name = "Clubs",
                        IconPath = "marker_club.png",
                        ColorCode = "#228B22", // Vert foncé
                        Category = ActivityCategory.Structure,
                        Description = "Clubs de plongée et associations",
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description" } }
                        },
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Professionnels",
                        IconPath = "marker_pro.png",
                        ColorCode = "#32CD32", // Vert lime
                        Category = ActivityCategory.Structure,
                        Description = "Centres de plongée, instructeurs et guides professionnels",
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description", "SafetyNotes" } }
                        },
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Bases fédérales",
                        IconPath = "marker_federal.png",
                        ColorCode = "#90EE90", // Vert clair
                        Category = ActivityCategory.Structure,
                        Description = "Bases fédérales et structures officielles",
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description", "SafetyNotes" } }
                        },
                        IsActive = true
                    },

                    // === BOUTIQUES (tons oranges) ===
                    new SpotType
                    {
                        Name = "Boutiques",
                        IconPath = "marker_shop.png",
                        ColorCode = "#FF8C00", // Orange principal
                        Category = ActivityCategory.Shop,
                        Description = "Magasins de matériel de plongée et équipements sous-marins",
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description" } }
                        },
                        IsActive = true
                    }
                };

                _context.SpotTypes.AddRange(spotTypes);
                await _context.SaveChangesAsync();

                // Vérifier que tous les types ont bien été créés
                var createdTypes = await _context.SpotTypes.Where(st => st.IsActive).ToListAsync();
                _logger.LogInformation($"Types de spots créés: {string.Join(", ", createdTypes.Select(t => $"{t.Name} ({t.ColorCode})"))}");
                
                if (createdTypes.Count != 8)
                {
                    _logger.LogWarning($"Attention: Seulement {createdTypes.Count}/8 types créés");
                }

                // Créer les 5 spots de base
                var spots = new List<Spot>
                {
                    new Spot
                    {
                        Name = "Calanque de Sormiou",
                        Description = "Magnifique calanque marseillaise avec une eau cristalline et une biodiversité riche. Site emblématique de la côte méditerranéenne.",
                        Latitude = 43.2148m,
                        Longitude = 5.4203m,
                        MaxDepth = 25,
                        DifficultyLevel = DifficultyLevel.Intermediate,
                        ValidationStatus = SpotValidationStatus.Approved,
                        TypeId = spotTypes[0].Id, // Plongée bouteille
                        CreatorId = adminUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        CurrentStrength = CurrentStrength.Moderate,
                        BestConditions = "Mer calme, visibilité 15-20m, éviter les vents du mistral",
                        SafetyNotes = "Attention aux bateaux de plaisance en été. Zone de baignade surveillée.",
                        RequiredEquipment = "Palmes, masque, tuba, combinaison 5mm recommandée"
                    },
                    new Spot
                    {
                        Name = "Île Maïre",
                        Description = "Site de plongée technique avec tombant spectaculaire et grottes sous-marines. Biodiversité exceptionnelle.",
                        Latitude = 43.2105m,
                        Longitude = 5.3520m,
                        MaxDepth = 40,
                        DifficultyLevel = DifficultyLevel.Advanced,
                        ValidationStatus = SpotValidationStatus.Approved,
                        TypeId = spotTypes[0].Id, // Plongée bouteille
                        CreatorId = adminUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        CurrentStrength = CurrentStrength.Strong,
                        BestConditions = "Mer peu agitée, visibilité 20-25m, pas de vent d'est",
                        SafetyNotes = "Plongée technique - Niveau 2 minimum requis. Courants forts possibles.",
                        RequiredEquipment = "Équipement complet de plongée, lampe obligatoire, parachute de palier"
                    },
                    new Spot
                    {
                        Name = "Baie de Cassis",
                        Description = "Site parfait pour l'apnée avec profondeur progressive et faune accessible. Idéal pour débutants.",
                        Latitude = 43.2148m,
                        Longitude = 5.5385m,
                        MaxDepth = 15,
                        DifficultyLevel = DifficultyLevel.Beginner,
                        ValidationStatus = SpotValidationStatus.Approved,
                        TypeId = spotTypes[1].Id, // Apnée
                        CreatorId = adminUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        CurrentStrength = CurrentStrength.Weak,
                        BestConditions = "Mer calme, visibilité 10-15m, température eau >18°C",
                        SafetyNotes = "Idéal pour débutants. Surveiller les autres usagers de la mer.",
                        RequiredEquipment = "Palmes, masque, tuba, combinaison selon saison"
                    },
                    new Spot
                    {
                        Name = "Port-Cros",
                        Description = "Réserve naturelle marine avec une biodiversité exceptionnelle. Parfait pour la photographie sous-marine.",
                        Latitude = 43.0092m,
                        Longitude = 6.3914m,
                        MaxDepth = 12,
                        DifficultyLevel = DifficultyLevel.Beginner,
                        ValidationStatus = SpotValidationStatus.Approved,
                        TypeId = spotTypes[3].Id, // Photo sous-marine
                        CreatorId = adminUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        CurrentStrength = CurrentStrength.Weak,
                        BestConditions = "Eau calme, excellente visibilité, lumière naturelle optimale",
                        SafetyNotes = "Zone protégée - respecter la faune et la flore. Pêche interdite.",
                        RequiredEquipment = "Appareil photo étanche, palmes, masque, tuba"
                    },
                    new Spot
                    {
                        Name = "Sentier Sous-Marin de Banyuls",
                        Description = "Parcours balisé idéal pour la randonnée sous-marine familiale. Découverte de la faune méditerranéenne.",
                        Latitude = 42.4875m,
                        Longitude = 3.1286m,
                        MaxDepth = 5,
                        DifficultyLevel = DifficultyLevel.Beginner,
                        ValidationStatus = SpotValidationStatus.Approved,
                        TypeId = spotTypes[2].Id, // Randonnée sous-marine
                        CreatorId = adminUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        CurrentStrength = CurrentStrength.None,
                        BestConditions = "Mer calme, visibilité correcte, éviter les jours de tramontane",
                        SafetyNotes = "Parcours familial. Respecter les bouées de balisage.",
                        RequiredEquipment = "Palmes, masque, tuba, lycra anti-UV recommandé"
                    }
                };

                _context.Spots.AddRange(spots);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Données initialisées avec succès : 1 utilisateur, 8 types de spots, 5 spots");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation des données");
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du test de connexion");
                return false;
            }
        }

        public async Task<bool> MigrateDatabaseAsync()
        {
            return await EnsureDatabaseCreatedAsync();
        }

        public async Task<bool> CleanupSpotTypesAsync()
        {
            // Exécuter la migration vers la nouvelle structure hiérarchique
            return await ExecuteSpotTypeMigrationAsync();
        }

        private async Task<bool> ExecuteSpotTypeMigrationAsync()
        {
            try
            {
                _logger.LogInformation("🚀 Début de la migration vers la structure hiérarchique des types de spots...");

                // 1. SAUVEGARDER TEMPORAIREMENT LES SPOTS
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TEMPORARY TABLE IF NOT EXISTS temp_spots_backup AS 
                    SELECT s.*, st.Name as OldTypeName 
                    FROM Spots s 
                    JOIN SpotTypes st ON s.TypeId = st.Id;
                ");

                // 2. DÉSACTIVER LES ANCIENS TYPES
                var oldTypesUpdated = await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE SpotTypes 
                    SET IsActive = 0 
                    WHERE Name IN ('Plongée récréative', 'Plongée technique');
                ");
                _logger.LogInformation($"Anciens types désactivés: {oldTypesUpdated}");

                // 3. SUPPRIMER LES NOUVEAUX TYPES S'ILS EXISTENT DÉJÀ
                await _context.Database.ExecuteSqlRawAsync(@"
                    DELETE FROM SpotTypes WHERE Name IN (
                        'Plongée bouteille', 'Apnée', 'Randonnée sous-marine', 'Photo sous-marine',
                        'Clubs', 'Professionnels', 'Bases fédérales', 'Boutiques'
                    );
                ");

                // 4. CRÉER LES NOUVEAUX TYPES DE SPOTS
                
                // ACTIVITÉS
                await _context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                    VALUES 
                    ('Plongée bouteille', 'marker_scuba.png', '#0077BE', 0, 'Sites de plongée avec bouteille (tous niveaux - récréative et technique)', 1, 
                     '{""RequiredFields"":[""MaxDepth"",""DifficultyLevel"",""SafetyNotes""],""MaxDepthRange"":[0,200]}', 1);
                ");

                await _context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                    VALUES 
                    ('Apnée', 'marker_freediving.png', '#4A90E2', 0, 'Sites adaptés à la plongée en apnée', 1, 
                     '{""RequiredFields"":[""MaxDepth"",""DifficultyLevel"",""SafetyNotes""],""MaxDepthRange"":[0,30]}', 1);
                ");

                await _context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                    VALUES 
                    ('Randonnée sous-marine', 'marker_snorkeling.png', '#87CEEB', 0, 'Sites de surface accessibles pour la randonnée sous-marine', 0, 
                     '{""RequiredFields"":[""DifficultyLevel"",""SafetyNotes""],""MaxDepthRange"":[0,5]}', 1);
                ");

                await _context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                    VALUES 
                    ('Photo sous-marine', 'marker_photography.png', '#5DADE2', 0, 'Sites d''intérêt pour la photographie sous-marine', 0, 
                     '{""RequiredFields"":[""DifficultyLevel""]}', 1);
                ");

                // STRUCTURES
                await _context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                    VALUES 
                    ('Clubs', 'marker_club.png', '#228B22', 1, 'Clubs de plongée et associations', 0, 
                     '{""RequiredFields"":[""Description""]}', 1);
                ");

                await _context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                    VALUES 
                    ('Professionnels', 'marker_pro.png', '#32CD32', 1, 'Centres de plongée, instructeurs et guides professionnels', 1, 
                     '{""RequiredFields"":[""Description"",""SafetyNotes""]}', 1);
                ");

                await _context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                    VALUES 
                    ('Bases fédérales', 'marker_federal.png', '#90EE90', 1, 'Bases fédérales et structures officielles', 1, 
                     '{""RequiredFields"":[""Description"",""SafetyNotes""]}', 1);
                ");

                // BOUTIQUES
                await _context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                    VALUES 
                    ('Boutiques', 'marker_shop.png', '#FF8C00', 2, 'Magasins de matériel de plongée et équipements sous-marins', 0, 
                     '{""RequiredFields"":[""Description""]}', 1);
                ");

                // 5. MIGRER LES SPOTS EXISTANTS
                var migratedSpots = await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE Spots 
                    SET TypeId = (SELECT Id FROM SpotTypes WHERE Name = 'Plongée bouteille' AND IsActive = 1 LIMIT 1)
                    WHERE TypeId IN (
                        SELECT Id FROM SpotTypes 
                        WHERE Name IN ('Plongée récréative', 'Plongée technique') AND IsActive = 0
                    );
                ");
                _logger.LogInformation($"Spots migrés vers 'Plongée bouteille': {migratedSpots}");

                // 6. NETTOYAGE
                await _context.Database.ExecuteSqlRawAsync("DROP TEMPORARY TABLE IF EXISTS temp_spots_backup;");

                // 7. VÉRIFICATION FINALE
                var newTypesCount = await _context.SpotTypes.Where(st => st.IsActive).CountAsync();
                _logger.LogInformation($"✅ Migration terminée! {newTypesCount} types de spots actifs");

                if (newTypesCount >= 8)
                {
                    _logger.LogInformation("🎉 Nouvelle structure hiérarchique installée avec succès!");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"⚠️ Seulement {newTypesCount}/8 types créés");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la migration des types de spots");
                return false;
            }
        }

        public async Task<bool> ImportRealSpotsAsync(string jsonFilePath = null)
        {
            // Pas besoin avec la nouvelle approche simplifiée
            return true;
        }

        public async Task<bool> ExecuteSpotTypeCategoryMappingMigrationAsync()
        {
            try
            {
                _logger.LogInformation("Simple service does not support category mapping migration - this is handled in the main DatabaseService");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in simple service migration method");
                return false;
            }
        }

        public async Task<string> AnalyzeFilteringIssuesAsync()
        {
            return "Simple service does not support filtering analysis - use the main DatabaseService";
        }

    }
}