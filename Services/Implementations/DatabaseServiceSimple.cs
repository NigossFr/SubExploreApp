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

                // Vérifier si des données existent déjà
                if (await _context.SpotTypes.AnyAsync() || await _context.Spots.AnyAsync())
                {
                    _logger.LogInformation("Données déjà présentes, suppression pour réinitialisation...");
                    
                    // Supprimer les données existantes
                    _context.Spots.RemoveRange(_context.Spots);
                    _context.SpotTypes.RemoveRange(_context.SpotTypes);
                    _context.Users.RemoveRange(_context.Users);
                    await _context.SaveChangesAsync();
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
                    CreatedAt = DateTime.UtcNow,
                    Preferences = new UserPreferences
                    {
                        Theme = "dark",
                        DisplayNamePreference = "username",
                        NotificationSettings = JsonSerializer.Serialize(new
                        {
                            SpotValidations = true,
                            NewSpots = true,
                            Comments = true
                        }),
                        Language = "fr",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                // Créer les 5 types de spots requis
                var spotTypes = new List<SpotType>
                {
                    new SpotType
                    {
                        Name = "Apnée",
                        IconPath = "marker_freediving.png",
                        ColorCode = "#00B4D8",
                        Category = ActivityCategory.Freediving,
                        Description = "Sites adaptés à la plongée en apnée",
                        RequiresExpertValidation = true,
                        ValidationCriteria = JsonSerializer.Serialize(new { RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" } }),
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Photo sous-marine",
                        IconPath = "marker_photography.png",
                        ColorCode = "#2EC4B6",
                        Category = ActivityCategory.UnderwaterPhotography,
                        Description = "Sites d'intérêt pour la photographie sous-marine",
                        RequiresExpertValidation = false,
                        ValidationCriteria = JsonSerializer.Serialize(new { RequiredFields = new[] { "DifficultyLevel" } }),
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Plongée récréative",
                        IconPath = "marker_diving.png",
                        ColorCode = "#006994",
                        Category = ActivityCategory.Diving,
                        Description = "Sites adaptés à la plongée avec bouteille",
                        RequiresExpertValidation = true,
                        ValidationCriteria = JsonSerializer.Serialize(new { RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" } }),
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Plongée technique",
                        IconPath = "marker_technical.png",
                        ColorCode = "#FF9F1C",
                        Category = ActivityCategory.Diving,
                        Description = "Sites pour plongée technique (profondeur, épaves...)",
                        RequiresExpertValidation = true,
                        ValidationCriteria = JsonSerializer.Serialize(new { RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes", "RequiredEquipment" } }),
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Randonnée sous marine",
                        IconPath = "marker_snorkeling.png",
                        ColorCode = "#48CAE4",
                        Category = ActivityCategory.Snorkeling,
                        Description = "Sites de surface accessibles pour la randonnée sous-marine",
                        RequiresExpertValidation = false,
                        ValidationCriteria = JsonSerializer.Serialize(new { RequiredFields = new[] { "DifficultyLevel", "SafetyNotes" } }),
                        IsActive = true
                    }
                };

                _context.SpotTypes.AddRange(spotTypes);
                await _context.SaveChangesAsync();

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
                        TypeId = spotTypes[2].Id, // Plongée récréative
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
                        TypeId = spotTypes[3].Id, // Plongée technique
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
                        TypeId = spotTypes[0].Id, // Apnée
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
                        TypeId = spotTypes[1].Id, // Photo sous-marine
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
                        TypeId = spotTypes[4].Id, // Randonnée sous marine
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

                _logger.LogInformation("Données initialisées avec succès : 1 utilisateur, 5 types de spots, 5 spots");
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
            // Pas besoin avec la nouvelle approche simplifiée
            return true;
        }

        public async Task<bool> ImportRealSpotsAsync(string jsonFilePath = null)
        {
            // Pas besoin avec la nouvelle approche simplifiée
            return true;
        }

        public async Task<string> GetDatabaseDiagnosticsAsync()
        {
            try
            {
                var totalSpots = await _context.Spots.CountAsync();
                var approvedSpots = await _context.Spots.CountAsync(s => s.ValidationStatus == SpotValidationStatus.Approved);
                var totalSpotTypes = await _context.SpotTypes.CountAsync();
                var activeSpotTypes = await _context.SpotTypes.CountAsync(st => st.IsActive);
                var totalUsers = await _context.Users.CountAsync();

                return $@"=== DIAGNOSTICS BASE DE DONNÉES ===
📊 SPOTS: Total: {totalSpots}, Approuvés: {approvedSpots}
🏷️ TYPES DE SPOTS: Total: {totalSpotTypes}, Actifs: {activeSpotTypes}
👤 UTILISATEURS: {totalUsers}
=== FIN DIAGNOSTICS ===";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du diagnostic");
                return $"❌ Erreur lors du diagnostic: {ex.Message}";
            }
        }
    }
}