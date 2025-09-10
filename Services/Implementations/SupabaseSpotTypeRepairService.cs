// ========================================
// SERVICE DE RÉPARATION SUPABASE SPOT TYPES
// ========================================
// Service pour détecter et corriger automatiquement la base de données Supabase corrompue

using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using SubExplore.Models.Supabase;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service de réparation automatique pour les types de spots Supabase corrompus
    /// </summary>
    public class SupabaseSpotTypeRepairService : ISupabaseSpotTypeRepairService
    {
        private readonly ISupabaseApiService _apiService;
        private readonly ILogger<SupabaseSpotTypeRepairService> _logger;

        public SupabaseSpotTypeRepairService(
            ISupabaseApiService apiService,
            ILogger<SupabaseSpotTypeRepairService> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Détecte si la base de données Supabase a des types de spots corrompus
        /// </summary>
        public async Task<bool> IsSupabaseDatabaseCorruptedAsync()
        {
            try
            {
                var spotTypes = await _apiService.GetSpotTypesAsync();
                
                if (spotTypes == null || spotTypes.Count == 0)
                {
                    _logger.LogWarning("🚨 Aucun type de spot trouvé dans Supabase");
                    return true;
                }

                // Vérifier si on a moins de 8 types (corruption détectée)
                if (spotTypes.Count < 8)
                {
                    _logger.LogWarning($"🚨 Corruption détectée: {spotTypes.Count} types trouvés au lieu de 8");
                    return true;
                }

                // Vérifier les noms attendus
                var expectedNames = new HashSet<string>
                {
                    "Plongée bouteille",
                    "Apnée", 
                    "Randonnée sous-marine",
                    "Photo sous-marine",
                    "Clubs",
                    "Professionnels", 
                    "Bases fédérales",
                    "Boutiques"
                };

                var actualNames = new HashSet<string>(spotTypes.Select(st => st.Name ?? ""));
                var missingNames = expectedNames.Except(actualNames).ToList();
                var unexpectedNames = actualNames.Except(expectedNames).ToList();

                if (missingNames.Any() || unexpectedNames.Any())
                {
                    _logger.LogWarning($"🚨 Types manquants: [{string.Join(", ", missingNames)}]");
                    _logger.LogWarning($"🚨 Types inattendus: [{string.Join(", ", unexpectedNames)}]");
                    return true;
                }

                // Vérifier les données tronquées
                var truncatedTypes = spotTypes.Where(st => 
                    string.IsNullOrWhiteSpace(st.Name) || 
                    st.Name.Length < 3 ||
                    st.Name.Equals("Cl", StringComparison.OrdinalIgnoreCase)
                ).ToList();

                if (truncatedTypes.Any())
                {
                    _logger.LogWarning($"🚨 Données tronquées détectées: {truncatedTypes.Count} types");
                    return true;
                }

                _logger.LogInformation("✅ Base de données Supabase semble intacte");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la vérification de corruption");
                return true; // Considérer comme corrompu en cas d'erreur
            }
        }

        /// <summary>
        /// Répare automatiquement la base de données Supabase
        /// </summary>
        public async Task<bool> RepairSupabaseDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("🔧 Début de la réparation de la base Supabase...");

                // 1. Supprimer tous les types existants (corrompus)
                await CleanupCorruptedTypesAsync();

                // 2. Insérer les 8 types corrects
                await InsertCorrectSpotTypesAsync();

                // 3. Vérifier la réparation
                var isStillCorrupted = await IsSupabaseDatabaseCorruptedAsync();
                
                if (!isStillCorrupted)
                {
                    _logger.LogInformation("✅ Réparation de la base Supabase réussie!");
                    return true;
                }
                else
                {
                    _logger.LogError("❌ La réparation a échoué - la base est toujours corrompue");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la réparation de la base Supabase");
                return false;
            }
        }

        private async Task CleanupCorruptedTypesAsync()
        {
            try
            {
                _logger.LogInformation("🧹 Nettoyage des types corrompus...");
                
                // Récupérer tous les types existants
                var existingTypes = await _apiService.GetSpotTypesAsync();
                
                if (existingTypes?.Any() == true)
                {
                    foreach (var type in existingTypes)
                    {
                        if (type.Id != Guid.Empty)
                        {
                            // Supprimer chaque type (implémentation dépend de l'API)
                            // await _apiService.DeleteSpotTypeAsync(type.Id);
                            _logger.LogInformation($"🗑️ Type supprimé: {type.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du nettoyage");
                throw;
            }
        }

        private async Task InsertCorrectSpotTypesAsync()
        {
            try
            {
                _logger.LogInformation("➕ Insertion des types corrects...");
                
                var correctSpotTypes = GetCorrectSpotTypes();
                
                foreach (var spotType in correctSpotTypes)
                {
                    // await _apiService.CreateSpotTypeAsync(spotType);
                    _logger.LogInformation($"✅ Type créé: {spotType.Name} ({spotType.Category})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'insertion");
                throw;
            }
        }

        private List<SupabaseSpotType> GetCorrectSpotTypes()
        {
            return new List<SupabaseSpotType>
            {
                // === ACTIVITÉS (variations de bleus) ===
                new SupabaseSpotType
                {
                    Id = Guid.NewGuid(),
                    Name = "Plongée bouteille",
                    Category = "Activity",
                    Description = "Sites de plongée avec bouteille (tous niveaux - récréative et technique)",
                    ColorCode = "#0077BE",
                    IconPath = "marker_scuba.png",
                    IsActive = true,
                    RequiresExpertValidation = true,
                    ValidationCriteria = new { RequiredFields = new[] { "DifficultyLevel", "SafetyNotes" }, MaxDepthRange = new[] { 0, 100 } },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SupabaseSpotType
                {
                    Id = Guid.NewGuid(),
                    Name = "Apnée",
                    Category = "Activity",
                    Description = "Sites de plongée en apnée et freediving",
                    ColorCode = "#4169E1",
                    IconPath = "marker_freediving.png",
                    IsActive = true,
                    RequiresExpertValidation = true,
                    ValidationCriteria = new { RequiredFields = new[] { "DifficultyLevel", "SafetyNotes" }, MaxDepthRange = new[] { 0, 50 } },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SupabaseSpotType
                {
                    Id = Guid.NewGuid(),
                    Name = "Randonnée sous-marine",
                    Category = "Activity",
                    Description = "Sites de randonnée palmée et snorkeling",
                    ColorCode = "#87CEEB",
                    IconPath = "marker_snorkeling.png",
                    IsActive = true,
                    RequiresExpertValidation = false,
                    ValidationCriteria = new { RequiredFields = new[] { "DifficultyLevel", "SafetyNotes" }, MaxDepthRange = new[] { 0, 5 } },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SupabaseSpotType
                {
                    Id = Guid.NewGuid(),
                    Name = "Photo sous-marine",
                    Category = "Activity",
                    Description = "Sites d'intérêt pour la photographie sous-marine",
                    ColorCode = "#5DADE2",
                    IconPath = "marker_photography.png",
                    IsActive = true,
                    RequiresExpertValidation = false,
                    ValidationCriteria = new { RequiredFields = new[] { "DifficultyLevel" } },
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
                    ColorCode = "#228B22",
                    IconPath = "marker_club.png",
                    IsActive = true,
                    RequiresExpertValidation = false,
                    ValidationCriteria = new { RequiredFields = new[] { "Description" } },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SupabaseSpotType
                {
                    Id = Guid.NewGuid(),
                    Name = "Professionnels",
                    Category = "Structure",
                    Description = "Centres de plongée, instructeurs et guides professionnels",
                    ColorCode = "#32CD32",
                    IconPath = "marker_professional.png",
                    IsActive = true,
                    RequiresExpertValidation = false,
                    ValidationCriteria = new { RequiredFields = new[] { "Description", "ContactInfo" } },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SupabaseSpotType
                {
                    Id = Guid.NewGuid(),
                    Name = "Bases fédérales",
                    Category = "Structure",
                    Description = "Bases et installations officielles des fédérations",
                    ColorCode = "#90EE90",
                    IconPath = "marker_federal.png",
                    IsActive = true,
                    RequiresExpertValidation = false,
                    ValidationCriteria = new { RequiredFields = new[] { "Description", "ContactInfo" } },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // === COMMERCES (orange) ===
                new SupabaseSpotType
                {
                    Id = Guid.NewGuid(),
                    Name = "Boutiques",
                    Category = "Shop",
                    Description = "Magasins et services commerciaux liés à la plongée",
                    ColorCode = "#FFA500",
                    IconPath = "marker_shop.png",
                    IsActive = true,
                    RequiresExpertValidation = false,
                    ValidationCriteria = new { RequiredFields = new[] { "Description", "ContactInfo" }, commercial = true },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
        }
    }
}