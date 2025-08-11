using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service pour exécuter la migration vers la nouvelle structure des types de spots
    /// </summary>
    public class SpotTypeMigrationService
    {
        private readonly SubExploreDbContext _context;
        private readonly ILogger<SpotTypeMigrationService> _logger;

        public SpotTypeMigrationService(SubExploreDbContext context, ILogger<SpotTypeMigrationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Exécute la migration vers la nouvelle structure des types de spots
        /// </summary>
        public async Task<bool> ExecuteMigrationAsync()
        {
            try
            {
                _logger.LogInformation("🚀 Démarrage de la migration vers la nouvelle structure des types de spots...");

                // Vérifier si la migration est nécessaire
                var hasOldTypes = await _context.SpotTypes.AnyAsync(st => 
                    (st.Name == "Plongée récréative" || st.Name == "Plongée technique") && st.IsActive);

                var hasNewTypes = await _context.SpotTypes.AnyAsync(st => 
                    st.Name == "Plongée bouteille" && st.IsActive);

                if (!hasOldTypes && hasNewTypes)
                {
                    _logger.LogInformation("✅ Migration déjà effectuée - nouveaux types présents");
                    return true;
                }

                if (!hasOldTypes && !hasNewTypes)
                {
                    _logger.LogInformation("⚠️ Aucun type de spot détecté - initialisation requise");
                    return await InitializeNewTypesAsync();
                }

                // Exécuter la migration manuellement
                await ExecuteMigrationStepsAsync();

                _logger.LogInformation("✅ Migration terminée avec succès!");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la migration des types de spots");
                return false;
            }
        }

        private async Task ExecuteMigrationStepsAsync()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Désactiver les anciens types
                _logger.LogInformation("📝 Étape 1: Désactivation des anciens types...");
                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE SpotTypes 
                    SET IsActive = 0 
                    WHERE Name IN ('Plongée récréative', 'Plongée technique')
                ");

                // 2. Supprimer les nouveaux types s'ils existent (nettoyage)
                _logger.LogInformation("🧹 Étape 2: Nettoyage des types existants...");
                await _context.Database.ExecuteSqlRawAsync(@"
                    DELETE FROM SpotTypes WHERE Name IN (
                        'Plongée bouteille', 'Apnée', 'Randonnée sous-marine', 'Photo sous-marine',
                        'Clubs', 'Professionnels', 'Bases fédérales', 'Boutiques'
                    )
                ");

                // 3. Créer les nouveaux types
                _logger.LogInformation("🎨 Étape 3: Création des nouveaux types de spots...");
                await CreateNewSpotTypesAsync();

                // 4. Migrer les spots existants
                _logger.LogInformation("📍 Étape 4: Migration des spots existants...");
                var migratedCount = await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE Spots 
                    SET TypeId = (SELECT Id FROM SpotTypes WHERE Name = 'Plongée bouteille' AND IsActive = 1 LIMIT 1)
                    WHERE TypeId IN (
                        SELECT Id FROM SpotTypes 
                        WHERE Name IN ('Plongée récréative', 'Plongée technique') AND IsActive = 0
                    )
                ");

                _logger.LogInformation($"📍 {migratedCount} spots migrés vers 'Plongée bouteille'");

                await transaction.CommitAsync();
                _logger.LogInformation("✅ Toutes les étapes de migration terminées avec succès!");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "❌ Erreur durant la migration - rollback effectué");
                throw;
            }
        }

        private async Task<bool> InitializeNewTypesAsync()
        {
            _logger.LogInformation("🎯 Initialisation des nouveaux types de spots...");
            await CreateNewSpotTypesAsync();
            return true;
        }

        private async Task CreateNewSpotTypesAsync()
        {
            // ACTIVITÉS (variations de bleus)
            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Plongée bouteille', 'marker_scuba.png', '#0077BE', 0, 'Sites de plongée avec bouteille (tous niveaux - récréative et technique)', 1, 
                 '{""RequiredFields"":[""MaxDepth"",""DifficultyLevel"",""SafetyNotes""],""MaxDepthRange"":[0,200]}', 1)
            ");

            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Apnée', 'marker_freediving.png', '#4A90E2', 0, 'Sites adaptés à la plongée en apnée', 1, 
                 '{""RequiredFields"":[""MaxDepth"",""DifficultyLevel"",""SafetyNotes""],""MaxDepthRange"":[0,30]}', 1)
            ");

            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Randonnée sous-marine', 'marker_snorkeling.png', '#87CEEB', 0, 'Sites de surface accessibles pour la randonnée sous-marine', 0, 
                 '{""RequiredFields"":[""DifficultyLevel"",""SafetyNotes""],""MaxDepthRange"":[0,5]}', 1)
            ");

            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Photo sous-marine', 'marker_photography.png', '#5DADE2', 0, 'Sites d''intérêt pour la photographie sous-marine', 0, 
                 '{""RequiredFields"":[""DifficultyLevel""]}', 1)
            ");

            // STRUCTURES (variations de verts)
            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Clubs', 'marker_club.png', '#228B22', 1, 'Clubs de plongée et associations', 0, 
                 '{""RequiredFields"":[""Description""]}', 1)
            ");

            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Professionnels', 'marker_pro.png', '#32CD32', 1, 'Centres de plongée, instructeurs et guides professionnels', 1, 
                 '{""RequiredFields"":[""Description"",""SafetyNotes""]}', 1)
            ");

            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Bases fédérales', 'marker_federal.png', '#90EE90', 1, 'Bases fédérales et structures officielles', 1, 
                 '{""RequiredFields"":[""Description"",""SafetyNotes""]}', 1)
            ");

            // BOUTIQUES (tons oranges)
            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Boutiques', 'marker_shop.png', '#FF8C00', 2, 'Magasins de matériel de plongée et équipements sous-marins', 0, 
                 '{""RequiredFields"":[""Description""]}', 1)
            ");

            _logger.LogInformation("✅ 8 nouveaux types de spots créés avec succès!");
        }

        /// <summary>
        /// Obtient un rapport sur l'état de la migration
        /// </summary>
        public async Task<string> GetMigrationStatusAsync()
        {
            try
            {
                var oldTypesActive = await _context.SpotTypes.CountAsync(st => 
                    (st.Name == "Plongée récréative" || st.Name == "Plongée technique") && st.IsActive);

                var newTypesActive = await _context.SpotTypes.CountAsync(st => st.IsActive &&
                    (st.Name == "Plongée bouteille" || st.Name == "Apnée" || st.Name == "Randonnée sous-marine" ||
                     st.Name == "Photo sous-marine" || st.Name == "Clubs" || st.Name == "Professionnels" ||
                     st.Name == "Bases fédérales" || st.Name == "Boutiques"));

                var totalSpots = await _context.Spots.CountAsync();

                var status = $@"
=== ÉTAT DE LA MIGRATION ===
📊 Anciens types actifs: {oldTypesActive}
🎯 Nouveaux types actifs: {newTypesActive}/8
📍 Total des spots: {totalSpots}

État: {(newTypesActive == 8 && oldTypesActive == 0 ? "✅ MIGRATION TERMINÉE" : 
         newTypesActive > 0 ? "⚠️ MIGRATION PARTIELLE" : "❌ MIGRATION REQUISE")}
=== FIN DU RAPPORT ===";

                return status;
            }
            catch (Exception ex)
            {
                return $"❌ Erreur lors de la récupération du statut: {ex.Message}";
            }
        }
    }
}