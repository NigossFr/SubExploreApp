using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubExplore.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToNewSpotTypeStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. SAUVEGARDE TEMPORAIRE (créer une vue temporaire pour garder les données des spots)
            migrationBuilder.Sql(@"
                CREATE TEMPORARY TABLE temp_spots_backup AS 
                SELECT s.*, st.Name as OldTypeName 
                FROM Spots s 
                JOIN SpotTypes st ON s.TypeId = st.Id;
            ");

            // 2. DÉSACTIVER LES ANCIENS TYPES DE SPOTS
            migrationBuilder.Sql(@"
                UPDATE SpotTypes 
                SET IsActive = 0 
                WHERE Name IN ('Plongée récréative', 'Plongée technique');
            ");

            // 3. SUPPRIMER LES NOUVEAUX TYPES S'ILS EXISTENT DÉJÀ (nettoyage)
            migrationBuilder.Sql(@"
                DELETE FROM SpotTypes WHERE Name IN (
                    'Plongée bouteille', 'Apnée', 'Randonnée sous-marine', 'Photo sous-marine',
                    'Clubs', 'Professionnels', 'Bases fédérales', 'Boutiques'
                );
            ");

            // 4. CRÉER LES NOUVEAUX TYPES DE SPOTS
            
            // === ACTIVITÉS (variations de bleus) ===
            migrationBuilder.Sql(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Plongée bouteille', 'marker_scuba.png', '#0077BE', 0, 'Sites de plongée avec bouteille (tous niveaux - récréative et technique)', 1, 
                 '{""RequiredFields"":[""MaxDepth"",""DifficultyLevel"",""SafetyNotes""],""MaxDepthRange"":[0,200]}', 1);
            ");

            migrationBuilder.Sql(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Apnée', 'marker_freediving.png', '#4A90E2', 1, 'Sites adaptés à la plongée en apnée', 1, 
                 '{""RequiredFields"":[""MaxDepth"",""DifficultyLevel"",""SafetyNotes""],""MaxDepthRange"":[0,30]}', 1);
            ");

            migrationBuilder.Sql(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Randonnée sous-marine', 'marker_snorkeling.png', '#87CEEB', 2, 'Sites de surface accessibles pour la randonnée sous-marine', 0, 
                 '{""RequiredFields"":[""DifficultyLevel"",""SafetyNotes""],""MaxDepthRange"":[0,5]}', 1);
            ");

            migrationBuilder.Sql(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Photo sous-marine', 'marker_photography.png', '#5DADE2', 3, 'Sites d''intérêt pour la photographie sous-marine', 0, 
                 '{""RequiredFields"":[""DifficultyLevel""]}', 1);
            ");

            // === STRUCTURES (variations de verts) ===
            migrationBuilder.Sql(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Clubs', 'marker_club.png', '#228B22', 4, 'Clubs de plongée et associations', 0, 
                 '{""RequiredFields"":[""Description""]}', 1);
            ");

            migrationBuilder.Sql(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Professionnels', 'marker_pro.png', '#32CD32', 4, 'Centres de plongée, instructeurs et guides professionnels', 1, 
                 '{""RequiredFields"":[""Description"",""SafetyNotes""]}', 1);
            ");

            migrationBuilder.Sql(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Bases fédérales', 'marker_federal.png', '#90EE90', 4, 'Bases fédérales et structures officielles', 1, 
                 '{""RequiredFields"":[""Description"",""SafetyNotes""]}', 1);
            ");

            // === BOUTIQUES (tons oranges) ===
            migrationBuilder.Sql(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
                VALUES 
                ('Boutiques', 'marker_shop.png', '#FF8C00', 4, 'Magasins de matériel de plongée et équipements sous-marins', 0, 
                 '{""RequiredFields"":[""Description""]}', 1);
            ");

            // 5. MIGRATION DES SPOTS EXISTANTS
            // Migrer tous les spots de plongée récréative et technique vers plongée bouteille
            migrationBuilder.Sql(@"
                UPDATE Spots 
                SET TypeId = (SELECT Id FROM SpotTypes WHERE Name = 'Plongée bouteille' AND IsActive = 1 LIMIT 1)
                WHERE TypeId IN (
                    SELECT Id FROM SpotTypes 
                    WHERE Name IN ('Plongée récréative', 'Plongée technique') AND IsActive = 0
                );
            ");

            // 6. NETTOYAGE DE LA SAUVEGARDE TEMPORAIRE
            migrationBuilder.Sql("DROP TEMPORARY TABLE IF EXISTS temp_spots_backup;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ROLLBACK: Restaurer les anciens types de spots
            
            // 1. Réactiver les anciens types
            migrationBuilder.Sql(@"
                UPDATE SpotTypes 
                SET IsActive = 1 
                WHERE Name IN ('Plongée récréative', 'Plongée technique');
            ");

            // 2. Supprimer les nouveaux types
            migrationBuilder.Sql(@"
                DELETE FROM SpotTypes WHERE Name IN (
                    'Plongée bouteille', 'Apnée', 'Randonnée sous-marine', 'Photo sous-marine',
                    'Clubs', 'Professionnels', 'Bases fédérales', 'Boutiques'
                );
            ");

            // 3. Restaurer les spots vers plongée récréative (par défaut)
            // Note: Les données exactes de mappage sont perdues, donc on assigne par défaut à récréative
            migrationBuilder.Sql(@"
                UPDATE Spots 
                SET TypeId = (SELECT Id FROM SpotTypes WHERE Name = 'Plongée récréative' AND IsActive = 1 LIMIT 1)
                WHERE TypeId NOT IN (SELECT Id FROM SpotTypes WHERE IsActive = 1);
            ");
        }
    }
}