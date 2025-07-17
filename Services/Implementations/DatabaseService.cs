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
using MySqlConnector;

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
                bool result = await _context.Database.EnsureCreatedAsync();

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
                    hasSpotTypes = await _context.SpotTypes.AnyAsync();
                    hasSpots = await _context.Spots.AnyAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Impossible de vérifier l'existence de données. Cela peut être normal si le schéma n'existe pas encore.");
                    hasSpotTypes = false;
                    hasSpots = false;
                }

                if (hasSpotTypes && hasSpots)
                {
                    _logger.LogInformation("La base de données contient déjà des données complètes");
                    return true;
                }

                _logger.LogInformation("Initialisation des données de base...");

                // Ajouter les types de spots seulement s'ils n'existent pas
                if (!hasSpotTypes)
                {
                    var spotTypes = new List<SpotType>
                {
                    new SpotType
                    {
                        Name = "Plongée récréative",
                        IconPath = "marker_diving.png",
                        ColorCode = "#006994",
                        Category = ActivityCategory.Diving,
                        Description = "Sites adaptés à la plongée avec bouteille",
                        RequiresExpertValidation = true,
                        ValidationCriteria = JsonSerializer.Serialize(new {
                            RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" },
                            MaxDepthRange = new[] { 0, 50 }
                        }),
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Apnée",
                        IconPath = "marker_freediving.png",
                        ColorCode = "#00B4D8",
                        Category = ActivityCategory.Freediving,
                        Description = "Sites adaptés à la plongée en apnée",
                        RequiresExpertValidation = true,
                        ValidationCriteria = JsonSerializer.Serialize(new {
                            RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" },
                            MaxDepthRange = new[] { 0, 30 }
                        }),
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Randonnée palmée",
                        IconPath = "marker_snorkeling.png",
                        ColorCode = "#48CAE4",
                        Category = ActivityCategory.Snorkeling,
                        Description = "Sites de surface accessibles pour le snorkeling",
                        RequiresExpertValidation = false,
                        ValidationCriteria = JsonSerializer.Serialize(new {
                            RequiredFields = new[] { "DifficultyLevel", "SafetyNotes" },
                            MaxDepthRange = new[] { 0, 5 }
                        }),
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
                        ValidationCriteria = JsonSerializer.Serialize(new {
                            RequiredFields = new[] { "DifficultyLevel" }
                        }),
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
                        ValidationCriteria = JsonSerializer.Serialize(new {
                            RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes", "RequiredEquipment" },
                            MaxDepthRange = new[] { 30, 200 },
                            MinDifficultyLevel = 3
                        }),
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Épave",
                        IconPath = "marker_wreck.png",
                        ColorCode = "#8B4513",
                        Category = ActivityCategory.Diving,
                        Description = "Sites d'épaves historiques ou artificielles",
                        RequiresExpertValidation = true,
                        ValidationCriteria = JsonSerializer.Serialize(new {
                            RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes", "RequiredEquipment" },
                            MaxDepthRange = new[] { 5, 100 }
                        }),
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Grotte marine",
                        IconPath = "marker_cave.png",
                        ColorCode = "#2F4F4F",
                        Category = ActivityCategory.Diving,
                        Description = "Grottes et cavernes sous-marines",
                        RequiresExpertValidation = true,
                        ValidationCriteria = JsonSerializer.Serialize(new {
                            RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes", "RequiredEquipment" },
                            MaxDepthRange = new[] { 5, 40 },
                            MinDifficultyLevel = 2
                        }),
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Tombant",
                        IconPath = "marker_wall.png",
                        ColorCode = "#4169E1",
                        Category = ActivityCategory.Diving,
                        Description = "Murs et tombants sous-marins",
                        RequiresExpertValidation = false,
                        ValidationCriteria = JsonSerializer.Serialize(new {
                            RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" },
                            MaxDepthRange = new[] { 10, 60 }
                        }),
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Récif corallien",
                        IconPath = "marker_reef.png",
                        ColorCode = "#FF6347",
                        Category = ActivityCategory.Snorkeling,
                        Description = "Récifs coralliens et jardins de corail",
                        RequiresExpertValidation = false,
                        ValidationCriteria = JsonSerializer.Serialize(new {
                            RequiredFields = new[] { "DifficultyLevel", "SafetyNotes" },
                            MaxDepthRange = new[] { 0, 25 }
                        }),
                        IsActive = true
                    },
                    new SpotType
                    {
                        Name = "Plongée de nuit",
                        IconPath = "marker_night.png",
                        ColorCode = "#191970",
                        Category = ActivityCategory.Diving,
                        Description = "Sites adaptés à la plongée nocturne",
                        RequiresExpertValidation = true,
                        ValidationCriteria = JsonSerializer.Serialize(new {
                            RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes", "RequiredEquipment" },
                            MaxDepthRange = new[] { 5, 30 },
                            MinDifficultyLevel = 2
                        }),
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
                var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@subexplore.com");
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
                    _logger.LogInformation("Compte administrateur ajouté");
                }
                else
                {
                    _logger.LogInformation("Compte administrateur déjà présent, ignoré");
                }

                // Création d'un utilisateur de test seulement s'il n'existe pas
                var existingTestUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@subexplore.com");
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
                        NotificationSettings = JsonSerializer.Serialize(new
                        {
                            SpotValidations = true,
                            NewSpots = false,
                            Comments = true
                        }),
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

                await _context.SaveChangesAsync();
                _logger.LogInformation("Users et SpotTypes initialisés avec succès");

                // Ajouter des spots d'exemple seulement s'ils n'existent pas
                if (!hasSpots)
                {
                    // Récupérer les IDs des données créées pour les spots
                    var divingType = await _context.SpotTypes.FirstOrDefaultAsync(st => st.Name == "Plongée récréative");
                    var freedivingType = await _context.SpotTypes.FirstOrDefaultAsync(st => st.Name == "Apnée");
                    var snorkelingType = await _context.SpotTypes.FirstOrDefaultAsync(st => st.Name == "Randonnée palmée");
                    var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@subexplore.com");
                    var adminUserId = adminUser?.Id ?? 1;

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
                        TypeId = divingType?.Id ?? 1,
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
                        TypeId = divingType?.Id ?? 1,
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
                        TypeId = freedivingType?.Id ?? 2,
                        CreatorId = adminUserId,
                        CreatedAt = DateTime.UtcNow,
                        CurrentStrength = CurrentStrength.Light,
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
                        TypeId = snorkelingType?.Id ?? 3,
                        CreatorId = adminUserId,
                        CreatedAt = DateTime.UtcNow,
                        CurrentStrength = CurrentStrength.Light,
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
                        TypeId = divingType?.Id ?? 1,
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

                    await _context.SaveChangesAsync();
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

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Test de la connexion à la base de données...");

                // Simple test de connexion
                bool canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    _logger.LogInformation("Connexion à la base de données établie avec succès");

                    // Récupérer des informations sur le serveur pour validation
                    try
                    {
                        var connection = _context.Database.GetDbConnection();
                        if (connection.State == System.Data.ConnectionState.Closed)
                        {
                            await connection.OpenAsync();
                        }

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT version()";
                            var version = await command.ExecuteScalarAsync();
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

                using (var connection = new MySqlConnector.MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Connexion directe établie avec succès");

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT version()";
                        var version = await command.ExecuteScalarAsync();
                        _logger.LogInformation("Version MySQL (connexion directe): {Version}", version);
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
    }
}