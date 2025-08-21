using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.IO;
using System.Reflection;
// using SubExplore.Migrations; // Migrations supprimées pour PostgreSQL

namespace SubExplore.Services.Implementations
{
    public class DatabaseService : IDatabaseService
    {
        private readonly SubExploreDbContext _context;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(SubExploreDbContext context, ILogger<DatabaseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> EnsureDatabaseCreatedAsync()
        {
            try
            {
                _logger.LogInformation("Tentative de création de la base de données...");

                // Utilisation de EnsureCreated pour créer la base de données à partir des modèles
                // Cette approche est préférée pour les applications MAUI où les migrations ne sont pas bien supportées
                bool result = await _context.Database.EnsureCreatedAsync().ConfigureAwait(false);

                if (result)
                {
                    _logger.LogInformation("Base de données créée avec succès");
                }
                else
                {
                    _logger.LogInformation("Base de données existante détectée, aucune création nécessaire");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la base de données: {Message}", ex.Message);

                // Loggez des informations supplémentaires sur l'exception pour faciliter le débogage
                if (ex.InnerException != null)
                {
                    _logger.LogError("Exception interne: {Message}", ex.InnerException.Message);
                }

                return false;
            }
        }

        /// <summary>
        /// Migrates the database schema (deprecated for MAUI applications)
        /// </summary>
        /// <returns>True if migration was successful, false otherwise</returns>
        public async Task<bool> MigrateDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Cette méthode n'est pas recommandée pour les applications MAUI. Utilisez EnsureDatabaseCreatedAsync() à la place.");

                // Cette méthode est conservée pour la compatibilité, mais nous déconseillerons son utilisation
                // avec une application MAUI qui ne supporte pas bien les migrations EF Core
                return await EnsureDatabaseCreatedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la migration de la base de données");
                return false;
            }
        }

        /// <summary>
        /// Seeds the database with initial data including spot types, admin user, test user, and sample spots
        /// </summary>
        /// <returns>True if seeding was successful, false otherwise</returns>
        public async Task<bool> SeedDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Vérification des données existantes...");

                // Vérifier si des données existent déjà
                bool hasSpotTypes = false;
                bool hasSpots = false;

                try
                {
                    hasSpotTypes = await _context.SpotTypes.AnyAsync().ConfigureAwait(false);
                    hasSpots = await _context.Spots.AnyAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Impossible de vérifier l'existence de données. Cela peut être normal si le schéma n'existe pas encore.");
                    hasSpotTypes = false;
                    hasSpots = false;
                }

                // Nettoyer les anciens types de spots non conformes
                if (hasSpotTypes)
                {
                    await CleanupObsoleteSpotTypesAsync();
                }
                    
                // Migrer vers la nouvelle organisation des types (toujours exécuter)
                var migrationLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SpotTypeDataMigrationService>.Instance;
                var migrationService = new SpotTypeDataMigrationService(_context, migrationLogger);
                await migrationService.MigrateSpotTypesAsync();

                _logger.LogInformation("Initialisation des données de base...");

                // Ajouter les types de spots seulement s'ils n'existent pas
                if (!hasSpotTypes)
                {
                    // Nouvelle organisation : Activités, Structures, Boutiques
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
                            ["RequiredFields"] = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" },
                            ["MaxDepthRange"] = new[] { 0, 200 }
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
                        Category = ActivityCategory.Other,
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
                        Category = ActivityCategory.Other,
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
                        Category = ActivityCategory.Other,
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
                    _logger.LogInformation("Types de spots ajoutés");
                }
                else
                {
                    _logger.LogInformation("Types de spots déjà présents, ignorés");
                }

                // Création d'un compte administrateur seulement s'il n'existe pas
                var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@subexplore.com").ConfigureAwait(false);
                if (existingAdmin == null)
                {
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
                    IsEmailConfirmed = true, // 🔑 ESSENTIEL: Admin email pré-vérifié pour permettre la connexion
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
                    _logger.LogInformation("Compte administrateur ajouté");
                }
                else
                {
                    _logger.LogInformation("Compte administrateur déjà présent, ignoré");
                }

                // Création d'un utilisateur de test seulement s'il n'existe pas
                var existingTestUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@subexplore.com").ConfigureAwait(false);
                if (existingTestUser == null)
                {
                var testUser = new User
                {
                    Email = "test@subexplore.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                    Username = "testuser",
                    FirstName = "Test",
                    LastName = "User",
                    AccountType = AccountType.Standard,
                    SubscriptionStatus = SubscriptionStatus.Free,
                    ExpertiseLevel = ExpertiseLevel.Intermediate,
                    CreatedAt = DateTime.UtcNow,
                    Preferences = new UserPreferences
                    {
                        Theme = "light",
                        DisplayNamePreference = "fullname",
                        NotificationSettings = new Dictionary<string, object>
                        {
                            { "SpotValidations", true },
                            { "NewSpots", false },
                            { "Comments", true }
                        },
                        Language = "fr",
                        CreatedAt = DateTime.UtcNow
                    }
                    };

                    _context.Users.Add(testUser);
                    _logger.LogInformation("Utilisateur de test ajouté");
                }
                else
                {
                    _logger.LogInformation("Utilisateur de test déjà présent, ignoré");
                }

                await _context.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogInformation("Users et SpotTypes initialisés avec succès");

                // Ajouter des spots d'exemple seulement s'ils n'existent pas
                if (!hasSpots)
                {
                    // Récupérer les IDs des données créées pour les spots
                    var divingType = await _context.SpotTypes.FirstOrDefaultAsync(st => st.Name == "Plongée récréative").ConfigureAwait(false);
                    var freedivingType = await _context.SpotTypes.FirstOrDefaultAsync(st => st.Name == "Apnée").ConfigureAwait(false);
                    var snorkelingType = await _context.SpotTypes.FirstOrDefaultAsync(st => st.Name == "Randonnée sous marine").ConfigureAwait(false);
                    var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@subexplore.com").ConfigureAwait(false);
                    var adminUserId = adminUser?.Id ?? Guid.NewGuid();

                    // Ajouter des spots d'exemple
                var sampleSpots = new List<Spot>
                {
                    new Spot
                    {
                        Name = "Calanque de Sormiou",
                        Description = "Magnifique calanque avec une eau cristalline, idéale pour la plongée et le snorkeling. Fonds rocheux avec une faune variée.",
                        Latitude = 43.2148m,
                        Longitude = 5.4203m,
                        MaxDepth = 25,
                        DifficultyLevel = DifficultyLevel.Intermediate,
                        ValidationStatus = SpotValidationStatus.Approved,
                        TypeId = divingType?.Id ?? Guid.NewGuid(),
                        CreatorId = adminUserId,
                        CreatedAt = DateTime.UtcNow,
                        CurrentStrength = CurrentStrength.Moderate,
                        BestConditions = "Mer calme, visibilité 15-20m",
                        SafetyNotes = "Attention aux bateaux de plaisance en été",
                        RequiredEquipment = "Palmes, masque, tuba, combinaison recommandée"
                    },
                    new Spot
                    {
                        Name = "Île Maïre",
                        Description = "Site de plongée emblématique de Marseille avec tombant et grottes. Biodiversité exceptionnelle.",
                        Latitude = 43.2105m,
                        Longitude = 5.3520m,
                        MaxDepth = 40,
                        DifficultyLevel = DifficultyLevel.Advanced,
                        ValidationStatus = SpotValidationStatus.Approved,
                        TypeId = divingType?.Id ?? Guid.NewGuid(),
                        CreatorId = adminUserId,
                        CreatedAt = DateTime.UtcNow,
                        CurrentStrength = CurrentStrength.Strong,
                        BestConditions = "Mer peu agitée, visibilité 20-25m",
                        SafetyNotes = "Plongée technique, niveau 2 minimum requis",
                        RequiredEquipment = "Équipement complet de plongée, lampe obligatoire"
                    },
                    new Spot
                    {
                        Name = "Calanque de Cassis",
                        Description = "Site parfait pour l'apnée avec une profondeur progressive et une faune accessible.",
                        Latitude = 43.2148m,
                        Longitude = 5.5385m,
                        MaxDepth = 15,
                        DifficultyLevel = DifficultyLevel.Beginner,
                        ValidationStatus = SpotValidationStatus.Approved,
                        TypeId = freedivingType?.Id ?? Guid.NewGuid(),
                        CreatorId = adminUserId,
                        CreatedAt = DateTime.UtcNow,
                        CurrentStrength = CurrentStrength.Weak,
                        BestConditions = "Mer calme, visibilité 10-15m",
                        SafetyNotes = "Idéal pour débutants, surveiller les autres utilisateurs",
                        RequiredEquipment = "Palmes, masque, tuba"
                    },
                    new Spot
                    {
                        Name = "Plage de la Pointe Rouge",
                        Description = "Excellent spot de snorkeling accessible à tous, avec parking et commodités.",
                        Latitude = 43.2380m,
                        Longitude = 5.3590m,
                        MaxDepth = 5,
                        DifficultyLevel = DifficultyLevel.Beginner,
                        ValidationStatus = SpotValidationStatus.Approved,
                        TypeId = snorkelingType?.Id ?? Guid.NewGuid(),
                        CreatorId = adminUserId,
                        CreatedAt = DateTime.UtcNow,
                        CurrentStrength = CurrentStrength.Weak,
                        BestConditions = "Toute condition, protégé du mistral",
                        SafetyNotes = "Attention aux baigneurs en été",
                        RequiredEquipment = "Palmes, masque, tuba"
                    },
                    new Spot
                    {
                        Name = "Cap Croisette",
                        Description = "Site de plongée avec épave accessible, parfait pour la photographie sous-marine.",
                        Latitude = 43.2065m,
                        Longitude = 5.4810m,
                        MaxDepth = 30,
                        DifficultyLevel = DifficultyLevel.Intermediate,
                        ValidationStatus = SpotValidationStatus.Approved,
                        TypeId = divingType?.Id ?? Guid.NewGuid(),
                        CreatorId = adminUserId,
                        CreatedAt = DateTime.UtcNow,
                        CurrentStrength = CurrentStrength.Moderate,
                        BestConditions = "Mer calme, visibilité 15-20m",
                        SafetyNotes = "Épave à 25m, attention aux filets",
                        RequiredEquipment = "Équipement complet de plongée, appareil photo étanche"
                    }
                    };

                    _context.Spots.AddRange(sampleSpots);
                    _logger.LogInformation("Spots d'exemple ajoutés: {Count}", sampleSpots.Count);

                    await _context.SaveChangesAsync().ConfigureAwait(false);
                }
                else
                {
                    _logger.LogInformation("Spots déjà présents, ignorés");
                }
                _logger.LogInformation("Toutes les données initialisées avec succès");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation des données: {Message}", ex.Message);

                if (ex.InnerException != null)
                {
                    _logger.LogError("Exception interne: {Message}", ex.InnerException.Message);
                }

                return false;
            }
        }

        /// <summary>
        /// Tests the database connection and retrieves server information
        /// </summary>
        /// <returns>True if connection is successful, false otherwise</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Test de la connexion à la base de données...");

                // Simple test de connexion
                bool canConnect = await _context.Database.CanConnectAsync().ConfigureAwait(false);

                if (canConnect)
                {
                    _logger.LogInformation("Connexion à la base de données établie avec succès");

                    // Récupérer des informations sur le serveur pour validation
                    try
                    {
                        var connection = _context.Database.GetDbConnection();
                        if (connection.State == System.Data.ConnectionState.Closed)
                        {
                            await connection.OpenAsync().ConfigureAwait(false);
                        }

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT version()";
                            var version = await command.ExecuteScalarAsync().ConfigureAwait(false);
                            _logger.LogInformation("Version MySQL: {Version}", version);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Connexion réussie mais impossible d'exécuter une requête simple");
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible de se connecter à la base de données");
                }

                return canConnect;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du test de connexion à la base de données: {Message}", ex.Message);

                if (ex.InnerException != null)
                {
                    _logger.LogError("Exception interne: {Message}", ex.InnerException.Message);
                }

                return false;
            }
        }

        // Nouvelle méthode pour tester directement la connexion MySQL
        /// <summary>
        /// Tests a direct MySQL connection using the provided or default connection string
        /// </summary>
        /// <param name="connectionString">Optional connection string to test; uses default if null</param>
        /// <returns>True if direct connection is successful, false otherwise</returns>
        public async Task<bool> TestDirectConnectionAsync(string? connectionString = null)
        {
            try
            {
                _logger.LogInformation("Test direct de la connexion MySQL...");

                // Utiliser la chaîne de connexion fournie ou celle du contexte
                string? connString = connectionString ?? _context.Database.GetConnectionString();

                if (string.IsNullOrEmpty(connString))
                {
                    _logger.LogError("Chaîne de connexion non disponible");
                    return false;
                }

                using (var connection = new Npgsql.NpgsqlConnection(connString))
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    _logger.LogInformation("Connexion PostgreSQL directe établie avec succès");

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT version()";
                        var version = await command.ExecuteScalarAsync().ConfigureAwait(false);
                        _logger.LogInformation("Version PostgreSQL (connexion directe): {Version}", version);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du test direct de connexion: {Message}", ex.Message);

                if (ex.InnerException != null)
                {
                    _logger.LogError("Exception interne: {Message}", ex.InnerException.Message);
                }

                return false;
            }
        }

        /// <summary>
        /// Cleans up obsolete spot types that don't conform to the required 5 types
        /// </summary>
        /// <returns>True if cleanup was successful, false otherwise</returns>
        public async Task<bool> CleanupSpotTypesAsync()
        {
            try
            {
                await CleanupObsoleteSpotTypesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du nettoyage des types de spots");
                return false;
            }
        }

        /// <summary>
        /// Nettoie les anciens types de spots non conformes aux 5 types requis
        /// </summary>
        /// <summary>
        /// Internal method to clean up obsolete spot types and associated spots
        /// </summary>
        /// <returns>Task representing the cleanup operation</returns>
        private async Task CleanupObsoleteSpotTypesAsync()
        {
            try
            {
                _logger.LogInformation("Nettoyage des anciens types de spots non conformes...");

                // Types autorisés selon les exigences
                var allowedSpotTypes = new[]
                {
                    "Apnée",
                    "Photo sous-marine", 
                    "Plongée récréative",
                    "Plongée technique",
                    "Randonnée sous marine"
                };

                // Récupérer tous les types de spots existants
                var existingSpotTypes = await _context.SpotTypes.ToListAsync().ConfigureAwait(false);
                
                // Identifier les types à supprimer
                var typesToRemove = existingSpotTypes
                    .Where(st => !allowedSpotTypes.Contains(st.Name))
                    .ToList();

                if (typesToRemove.Any())
                {
                    _logger.LogInformation("Suppression de {Count} types de spots obsolètes : {Types}", 
                        typesToRemove.Count, 
                        string.Join(", ", typesToRemove.Select(t => t.Name)));

                    // Supprimer les spots associés aux types obsolètes
                    var spotsToRemove = await _context.Spots
                        .Where(s => typesToRemove.Select(t => t.Id).Contains(s.TypeId))
                        .ToListAsync().ConfigureAwait(false);

                    if (spotsToRemove.Any())
                    {
                        _logger.LogInformation("Suppression de {Count} spots associés aux types obsolètes", spotsToRemove.Count);
                        _context.Spots.RemoveRange(spotsToRemove);
                    }

                    // Supprimer les types obsolètes
                    _context.SpotTypes.RemoveRange(typesToRemove);
                    
                    await _context.SaveChangesAsync().ConfigureAwait(false);
                    _logger.LogInformation("Nettoyage terminé avec succès");
                }
                else
                {
                    _logger.LogInformation("Aucun type de spot obsolète trouvé");
                }

                // Vérifier si les 5 types requis existent et les ajouter si nécessaire
                await EnsureRequiredSpotTypesExistAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du nettoyage des types de spots obsolètes");
                throw;
            }
        }

        /// <summary>
        /// S'assure que les 5 types de spots requis existent dans la base de données
        /// </summary>
        /// <summary>
        /// Ensures all 5 required spot types exist in the database, adding missing ones
        /// </summary>
        /// <returns>Task representing the verification and creation operation</returns>
        private async Task EnsureRequiredSpotTypesExistAsync()
        {
            try
            {
                var requiredSpotTypes = new[]
                {
                    new { Name = "Apnée", IconPath = "marker_freediving.png", ColorCode = "#00B4D8", Category = ActivityCategory.Activity, Description = "Sites adaptés à la plongée en apnée" },
                    new { Name = "Photo sous-marine", IconPath = "marker_photography.png", ColorCode = "#2EC4B6", Category = ActivityCategory.Activity, Description = "Sites d'intérêt pour la photographie sous-marine" },
                    new { Name = "Plongée récréative", IconPath = "marker_diving.png", ColorCode = "#006994", Category = ActivityCategory.Activity, Description = "Sites adaptés à la plongée avec bouteille" },
                    new { Name = "Plongée technique", IconPath = "marker_technical.png", ColorCode = "#FF9F1C", Category = ActivityCategory.Activity, Description = "Sites pour plongée technique (profondeur, épaves...)" },
                    new { Name = "Randonnée sous marine", IconPath = "marker_snorkeling.png", ColorCode = "#48CAE4", Category = ActivityCategory.Activity, Description = "Sites de surface accessibles pour la randonnée sous-marine" }
                };

                foreach (var requiredType in requiredSpotTypes)
                {
                    var existingType = await _context.SpotTypes
                        .FirstOrDefaultAsync(st => st.Name == requiredType.Name).ConfigureAwait(false);

                    if (existingType == null)
                    {
                        var newSpotType = new SpotType
                        {
                            Name = requiredType.Name,
                            IconPath = requiredType.IconPath,
                            ColorCode = requiredType.ColorCode,
                            Category = requiredType.Category,
                            Description = requiredType.Description,
                            RequiresExpertValidation = requiredType.Name.Contains("technique") || requiredType.Name.Contains("Apnée") || requiredType.Name.Contains("récréative"),
                            ValidationCriteria = new Dictionary<string, object>
                            {
                                { "RequiredFields", new[] { "DifficultyLevel", "SafetyNotes" } }
                            },
                            IsActive = true
                        };

                        _context.SpotTypes.Add(newSpotType);
                        _logger.LogInformation("Ajout du type de spot manquant : {Name}", requiredType.Name);
                    }
                }

                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des types de spots requis");
                throw;
            }
        }

        /// <summary>
        /// Imports real spot data from a JSON file (embedded resource or file path)
        /// </summary>
        /// <param name="jsonFilePath">Optional path to JSON file; uses embedded resource if null</param>
        /// <returns>True if import was successful with spots imported, false otherwise</returns>
        public async Task<bool> ImportRealSpotsAsync(string jsonFilePath = null)
        {
            try
            {
                _logger.LogInformation("Début de l'import des spots réels...");

                string jsonContent;

                // Si un chemin spécifique est fourni, l'utiliser, sinon lire depuis les ressources embarquées
                if (!string.IsNullOrEmpty(jsonFilePath) && File.Exists(jsonFilePath))
                {
                    _logger.LogInformation("Lecture du fichier JSON depuis : {FilePath}", jsonFilePath);
                    jsonContent = await File.ReadAllTextAsync(jsonFilePath).ConfigureAwait(false);
                }
                else
                {
                    // Lire depuis les ressources embarquées
                    _logger.LogInformation("Lecture du fichier JSON depuis les ressources embarquées...");
                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = "SubExplore.Data.real_spots.json";

                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null)
                    {
                        _logger.LogError("Ressource embarquée non trouvée : {ResourceName}", resourceName);
                        _logger.LogInformation("Ressources disponibles : {Resources}", 
                            string.Join(", ", assembly.GetManifestResourceNames()));
                        return false;
                    }

                    using var reader = new StreamReader(stream);
                    jsonContent = await reader.ReadToEndAsync().ConfigureAwait(false);
                    _logger.LogInformation("Fichier JSON lu depuis les ressources embarquées avec succès");
                }
                var importData = JsonSerializer.Deserialize<Models.Import.SpotsImportFile>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (importData?.Spots == null || !importData.Spots.Any())
                {
                    _logger.LogWarning("Aucun spot trouvé dans le fichier d'import");
                    return false;
                }

                // Récupérer l'utilisateur admin pour créer les spots
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@subexplore.com").ConfigureAwait(false);
                if (adminUser == null)
                {
                    _logger.LogError("Utilisateur admin introuvable pour l'import des spots");
                    return false;
                }

                int importedCount = 0;
                int skippedCount = 0;

                foreach (var spotData in importData.Spots)
                {
                    try
                    {
                        // Vérifier si le spot existe déjà
                        var existingSpot = await _context.Spots
                            .FirstOrDefaultAsync(s => s.Name == spotData.Name && 
                                                     s.Latitude == spotData.Latitude && 
                                                     s.Longitude == spotData.Longitude).ConfigureAwait(false);

                        if (existingSpot != null)
                        {
                            _logger.LogInformation("Spot déjà existant ignoré : {SpotName}", spotData.Name);
                            skippedCount++;
                            continue;
                        }

                        // Trouver le type de spot correspondant
                        var spotType = await _context.SpotTypes
                            .FirstOrDefaultAsync(st => st.Name == spotData.SpotType && st.IsActive).ConfigureAwait(false);

                        if (spotType == null)
                        {
                            _logger.LogWarning("Type de spot non trouvé pour : {SpotType}", spotData.SpotType);
                            skippedCount++;
                            continue;
                        }

                        // Convertir les enums avec traduction français -> anglais
                        var difficultyLevel = ConvertDifficultyLevelFromFrench(spotData.DifficultyLevel);
                        if (difficultyLevel == null)
                        {
                            _logger.LogWarning("Niveau de difficulté invalide : {DifficultyLevel}", spotData.DifficultyLevel);
                            skippedCount++;
                            continue;
                        }

                        var currentStrength = ConvertCurrentStrengthFromFrench(spotData.CurrentStrength);
                        if (currentStrength == null)
                        {
                            _logger.LogWarning("Force de courant invalide : {CurrentStrength}", spotData.CurrentStrength);
                            skippedCount++;
                            continue;
                        }

                        var validationStatus = ConvertValidationStatusFromFrench(spotData.ValidationStatus) ?? Models.Enums.SpotValidationStatus.Pending;

                        // Créer le nouveau spot
                        var newSpot = new Models.Domain.Spot
                        {
                            Name = spotData.Name,
                            Description = spotData.Description,
                            Latitude = spotData.Latitude,
                            Longitude = spotData.Longitude,
                            MaxDepth = spotData.MaxDepth,
                            DifficultyLevel = difficultyLevel.Value,
                            ValidationStatus = validationStatus,
                            TypeId = spotType.Id,
                            CreatorId = adminUser.Id,
                            CreatedAt = DateTime.UtcNow,
                            CurrentStrength = currentStrength.Value,
                            BestConditions = spotData.BestConditions,
                            SafetyNotes = spotData.SafetyNotes,
                            RequiredEquipment = spotData.RequiredEquipment
                        };

                        _context.Spots.Add(newSpot);
                        importedCount++;

                        _logger.LogInformation("Spot préparé pour import : {SpotName}", spotData.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de l'import du spot : {SpotName}", spotData.Name);
                        skippedCount++;
                    }
                }

                // Sauvegarder tous les changements
                await _context.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation("Import terminé - Importés: {ImportedCount}, Ignorés: {SkippedCount}", 
                    importedCount, skippedCount);

                return importedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'import des spots réels");
                return false;
            }
        }

        /// <summary>
        /// Convertit un niveau de difficulté depuis le français vers l'enum anglais
        /// </summary>
        /// <summary>
        /// Converts a French difficulty level string to the corresponding enum value
        /// </summary>
        /// <param name="frenchValue">French difficulty level string</param>
        /// <returns>Corresponding DifficultyLevel enum value or null if invalid</returns>
        private Models.Enums.DifficultyLevel? ConvertDifficultyLevelFromFrench(string frenchValue)
        {
            return frenchValue?.ToLower() switch
            {
                "débutant" => Models.Enums.DifficultyLevel.Beginner,
                "intermédiaire" => Models.Enums.DifficultyLevel.Intermediate,
                "avancé" => Models.Enums.DifficultyLevel.Advanced,
                "expert" => Models.Enums.DifficultyLevel.Expert,
                _ => null
            };
        }

        /// <summary>
        /// Convertit une force de courant depuis le français vers l'enum anglais
        /// </summary>
        /// <summary>
        /// Converts a French current strength string to the corresponding enum value
        /// </summary>
        /// <param name="frenchValue">French current strength string</param>
        /// <returns>Corresponding CurrentStrength enum value or null if invalid</returns>
        private Models.Enums.CurrentStrength? ConvertCurrentStrengthFromFrench(string frenchValue)
        {
            return frenchValue?.ToLower() switch
            {
                "aucun" => Models.Enums.CurrentStrength.None,
                "léger" => Models.Enums.CurrentStrength.Weak,
                "modéré" => Models.Enums.CurrentStrength.Moderate,
                "fort" => Models.Enums.CurrentStrength.Strong,
                "très fort" => Models.Enums.CurrentStrength.Extreme,
                "extrême" => Models.Enums.CurrentStrength.Extreme,
                _ => null
            };
        }

        /// <summary>
        /// Convertit un statut de validation depuis le français vers l'enum anglais
        /// </summary>
        /// <summary>
        /// Converts a French validation status string to the corresponding enum value
        /// </summary>
        /// <param name="frenchValue">French validation status string</param>
        /// <returns>Corresponding SpotValidationStatus enum value or null if invalid</returns>
        private Models.Enums.SpotValidationStatus? ConvertValidationStatusFromFrench(string frenchValue)
        {
            return frenchValue?.ToLower() switch
            {
                "brouillon" => Models.Enums.SpotValidationStatus.Draft,
                "en attente" => Models.Enums.SpotValidationStatus.Pending,
                "révision nécessaire" => Models.Enums.SpotValidationStatus.NeedsRevision,
                "en révision" => Models.Enums.SpotValidationStatus.NeedsRevision,
                "approuvé" => Models.Enums.SpotValidationStatus.Approved,
                "rejeté" => Models.Enums.SpotValidationStatus.Rejected,
                "archivé" => Models.Enums.SpotValidationStatus.Archived,
                _ => null
            };
        }

        /// <summary>
        /// Executes the FixSpotTypeCategoryMapping migration
        /// </summary>
        /// <returns>True if migration executed successfully, false otherwise</returns>
        public async Task<bool> ExecuteSpotTypeCategoryMappingMigrationAsync()
        {
            try
            {
                _logger.LogInformation("FixSpotTypeCategoryMapping migration no longer needed - using simplified approach");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecuteSpotTypeCategoryMappingMigrationAsync: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Analyzes filtering issues after migration
        /// </summary>
        /// <returns>Detailed analysis report</returns>
        public async Task<string> AnalyzeFilteringIssuesAsync()
        {
            try
            {
                var debugTool = new Helpers.FilterDebugTool(_context, Microsoft.Extensions.Logging.Abstractions.NullLogger<Helpers.FilterDebugTool>.Instance);
                return await debugTool.AnalyzeFilteringIssuesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze filtering issues: {Message}", ex.Message);
                return $"❌ Analysis failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Méthode de diagnostic pour vérifier le contenu de la base de données
        /// </summary>
    }
}