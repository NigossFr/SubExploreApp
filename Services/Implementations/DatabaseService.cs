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
using System.IO;
using System.Reflection;

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

                // Nettoyer les anciens types de spots non conformes
                if (hasSpotTypes)
                {
                    await CleanupObsoleteSpotTypesAsync();
                }

                _logger.LogInformation("Initialisation des données de base...");

                // Ajouter les types de spots seulement s'ils n'existent pas
                if (!hasSpotTypes)
                {
                    // Types de spots conformes aux exigences : 5 types + filtre "Tous"
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
                        ValidationCriteria = JsonSerializer.Serialize(new {
                            RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" },
                            MaxDepthRange = new[] { 0, 30 }
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
                        Name = "Randonnée sous marine",
                        IconPath = "marker_snorkeling.png",
                        ColorCode = "#48CAE4",
                        Category = ActivityCategory.Snorkeling,
                        Description = "Sites de surface accessibles pour la randonnée sous-marine",
                        RequiresExpertValidation = false,
                        ValidationCriteria = JsonSerializer.Serialize(new {
                            RequiredFields = new[] { "DifficultyLevel", "SafetyNotes" },
                            MaxDepthRange = new[] { 0, 5 }
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
                    var snorkelingType = await _context.SpotTypes.FirstOrDefaultAsync(st => st.Name == "Randonnée sous marine");
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
                var existingSpotTypes = await _context.SpotTypes.ToListAsync();
                
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
                        .ToListAsync();

                    if (spotsToRemove.Any())
                    {
                        _logger.LogInformation("Suppression de {Count} spots associés aux types obsolètes", spotsToRemove.Count);
                        _context.Spots.RemoveRange(spotsToRemove);
                    }

                    // Supprimer les types obsolètes
                    _context.SpotTypes.RemoveRange(typesToRemove);
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Nettoyage terminé avec succès");
                }
                else
                {
                    _logger.LogInformation("Aucun type de spot obsolète trouvé");
                }

                // Vérifier si les 5 types requis existent et les ajouter si nécessaire
                await EnsureRequiredSpotTypesExistAsync();
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
        private async Task EnsureRequiredSpotTypesExistAsync()
        {
            try
            {
                var requiredSpotTypes = new[]
                {
                    new { Name = "Apnée", IconPath = "marker_freediving.png", ColorCode = "#00B4D8", Category = ActivityCategory.Freediving, Description = "Sites adaptés à la plongée en apnée" },
                    new { Name = "Photo sous-marine", IconPath = "marker_photography.png", ColorCode = "#2EC4B6", Category = ActivityCategory.UnderwaterPhotography, Description = "Sites d'intérêt pour la photographie sous-marine" },
                    new { Name = "Plongée récréative", IconPath = "marker_diving.png", ColorCode = "#006994", Category = ActivityCategory.Diving, Description = "Sites adaptés à la plongée avec bouteille" },
                    new { Name = "Plongée technique", IconPath = "marker_technical.png", ColorCode = "#FF9F1C", Category = ActivityCategory.Diving, Description = "Sites pour plongée technique (profondeur, épaves...)" },
                    new { Name = "Randonnée sous marine", IconPath = "marker_snorkeling.png", ColorCode = "#48CAE4", Category = ActivityCategory.Snorkeling, Description = "Sites de surface accessibles pour la randonnée sous-marine" }
                };

                foreach (var requiredType in requiredSpotTypes)
                {
                    var existingType = await _context.SpotTypes
                        .FirstOrDefaultAsync(st => st.Name == requiredType.Name);

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
                            ValidationCriteria = JsonSerializer.Serialize(new {
                                RequiredFields = new[] { "DifficultyLevel", "SafetyNotes" }
                            }),
                            IsActive = true
                        };

                        _context.SpotTypes.Add(newSpotType);
                        _logger.LogInformation("Ajout du type de spot manquant : {Name}", requiredType.Name);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des types de spots requis");
                throw;
            }
        }

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
                    jsonContent = await File.ReadAllTextAsync(jsonFilePath);
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
                    jsonContent = await reader.ReadToEndAsync();
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
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@subexplore.com");
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
                                                     s.Longitude == spotData.Longitude);

                        if (existingSpot != null)
                        {
                            _logger.LogInformation("Spot déjà existant ignoré : {SpotName}", spotData.Name);
                            skippedCount++;
                            continue;
                        }

                        // Trouver le type de spot correspondant
                        var spotType = await _context.SpotTypes
                            .FirstOrDefaultAsync(st => st.Name == spotData.SpotType && st.IsActive);

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
                await _context.SaveChangesAsync();

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
        private Models.Enums.CurrentStrength? ConvertCurrentStrengthFromFrench(string frenchValue)
        {
            return frenchValue?.ToLower() switch
            {
                "aucun" => Models.Enums.CurrentStrength.None,
                "léger" => Models.Enums.CurrentStrength.Light,
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
        /// Méthode de diagnostic pour vérifier le contenu de la base de données
        /// </summary>
        public async Task<string> GetDatabaseDiagnosticsAsync()
        {
            try
            {
                var diagnostics = new StringBuilder();
                diagnostics.AppendLine("=== DIAGNOSTICS BASE DE DONNÉES ===");
                
                // Compter les spots par statut
                var totalSpots = await _context.Spots.CountAsync();
                var approvedSpots = await _context.Spots.CountAsync(s => s.ValidationStatus == SpotValidationStatus.Approved);
                var pendingSpots = await _context.Spots.CountAsync(s => s.ValidationStatus == SpotValidationStatus.Pending);
                var draftSpots = await _context.Spots.CountAsync(s => s.ValidationStatus == SpotValidationStatus.Draft);
                
                diagnostics.AppendLine($"📊 SPOTS:");
                diagnostics.AppendLine($"  Total: {totalSpots}");
                diagnostics.AppendLine($"  Approuvés: {approvedSpots}");
                diagnostics.AppendLine($"  En attente: {pendingSpots}");
                diagnostics.AppendLine($"  Brouillons: {draftSpots}");
                
                // Lister les premiers spots avec leurs détails
                if (totalSpots > 0)
                {
                    var sampleSpots = await _context.Spots
                        .Include(s => s.Type)
                        .Take(5)
                        .ToListAsync();
                    
                    diagnostics.AppendLine($"\n📍 EXEMPLES DE SPOTS:");
                    foreach (var spot in sampleSpots)
                    {
                        diagnostics.AppendLine($"  - {spot.Name} ({spot.Type?.Name ?? "Type inconnu"})");
                        diagnostics.AppendLine($"    Position: {spot.Latitude}, {spot.Longitude}");
                        diagnostics.AppendLine($"    Statut: {spot.ValidationStatus}");
                    }
                }
                
                // Compter les types de spots
                var totalSpotTypes = await _context.SpotTypes.CountAsync();
                var activeSpotTypes = await _context.SpotTypes.CountAsync(st => st.IsActive);
                
                diagnostics.AppendLine($"\n🏷️ TYPES DE SPOTS:");
                diagnostics.AppendLine($"  Total: {totalSpotTypes}");
                diagnostics.AppendLine($"  Actifs: {activeSpotTypes}");
                
                if (activeSpotTypes > 0)
                {
                    var activeTypes = await _context.SpotTypes
                        .Where(st => st.IsActive)
                        .Select(st => st.Name)
                        .ToListAsync();
                    
                    diagnostics.AppendLine($"  Types actifs: {string.Join(", ", activeTypes)}");
                }
                
                // Compter les utilisateurs
                var totalUsers = await _context.Users.CountAsync();
                diagnostics.AppendLine($"\n👤 UTILISATEURS: {totalUsers}");
                
                diagnostics.AppendLine("=== FIN DIAGNOSTICS ===");
                
                return diagnostics.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du diagnostic de base de données");
                return $"❌ Erreur lors du diagnostic: {ex.Message}";
            }
        }
    }
}