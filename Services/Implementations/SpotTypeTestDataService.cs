using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using SubExplore.Models.Supabase;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service pour créer des données de test des types de spots
    /// </summary>
    public class SpotTypeTestDataService : ISpotTypeTestDataService
    {
        private readonly ISupabaseApiService _apiService;
        private readonly ILogger<SpotTypeTestDataService> _logger;

        public SpotTypeTestDataService(
            ISupabaseApiService apiService,
            ILogger<SpotTypeTestDataService> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Crée les types de spots de base si ils n'existent pas
        /// </summary>
        public async Task EnsureBasicSpotTypesAsync()
        {
            try
            {
                _logger.LogInformation("🔍 Vérification des types de spots de base...");

                // Récupérer les types existants
                var existingTypes = await _apiService.GetSpotTypesAsync();
                if (existingTypes.Any())
                {
                    _logger.LogInformation($"✅ {existingTypes.Count} types de spots déjà présents");
                    return;
                }

                _logger.LogInformation("📋 Création des types de spots de base...");

                // Utiliser les types de spots définis dans le système original SubExplore
                var basicSpotTypes = new List<SupabaseSpotType>
                {
                    // === ACTIVITÉS (variations de bleus) ===
                    new SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Plongée bouteille",
                        Category = "Activity",
                        Description = "Sites de plongée avec bouteille (tous niveaux - récréative et technique)",
                        ColorCode = "#0077BE", // Bleu principal
                        IconPath = "marker_scuba.png",
                        IsActive = true,
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" } },
                            { "MaxDepthRange", new[] { 0, 200 } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Apnée",
                        Category = "Activity",
                        Description = "Sites adaptés à la plongée en apnée",
                        ColorCode = "#4A90E2", // Bleu moyen
                        IconPath = "marker_freediving.png",
                        IsActive = true,
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" } },
                            { "MaxDepthRange", new[] { 0, 30 } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Randonnée sous-marine",
                        Category = "Activity",
                        Description = "Sites de surface accessibles pour la randonnée sous-marine",
                        ColorCode = "#87CEEB", // Bleu clair
                        IconPath = "marker_snorkeling.png",
                        IsActive = true,
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "DifficultyLevel", "SafetyNotes" } },
                            { "MaxDepthRange", new[] { 0, 5 } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Photo sous-marine",
                        Category = "Activity",
                        Description = "Sites d'intérêt pour la photographie sous-marine",
                        ColorCode = "#5DADE2", // Bleu photo
                        IconPath = "marker_photography.png",
                        IsActive = true,
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "DifficultyLevel" } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },

                    // === STRUCTURES (variations de verts) ===
                    new SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Clubs",
                        Category = "Structure",
                        Description = "Clubs de plongée et associations",
                        ColorCode = "#228B22", // Vert foncé
                        IconPath = "marker_club.png",
                        IsActive = true,
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description" } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Professionnels",
                        Category = "Structure",
                        Description = "Centres de plongée, instructeurs et guides professionnels",
                        ColorCode = "#32CD32", // Vert lime
                        IconPath = "marker_pro.png",
                        IsActive = true,
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description", "SafetyNotes" } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Bases fédérales",
                        Category = "Structure",
                        Description = "Bases fédérales et structures officielles",
                        ColorCode = "#90EE90", // Vert clair
                        IconPath = "marker_federal.png",
                        IsActive = true,
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description", "SafetyNotes" } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },

                    // === BOUTIQUES (tons oranges) ===
                    new SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Boutiques",
                        Category = "Shop",
                        Description = "Magasins de matériel de plongée et équipements sous-marins",
                        ColorCode = "#FF8C00", // Orange principal
                        IconPath = "marker_shop.png",
                        IsActive = true,
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description" } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                int created = 0;
                foreach (var spotType in basicSpotTypes)
                {
                    try
                    {
                        // Utiliser la méthode de création depuis l'API Service
                        await _apiService.CreateSpotTypeAsync(spotType);
                        created++;
                        _logger.LogInformation($"✅ Type de spot créé: {spotType.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"❌ Erreur création type: {spotType.Name}");
                    }
                }

                _logger.LogInformation($"🎉 {created}/{basicSpotTypes.Count} types de spots créés avec succès (système SubExplore officiel)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la création des types de spots de base");
                throw;
            }
        }
    }
}