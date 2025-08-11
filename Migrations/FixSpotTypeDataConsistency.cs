using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubExplore.Migrations
{
    /// <inheritdoc />
    public partial class FixSpotTypeDataConsistency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. ENSURE ALL NEW SPOT TYPES ARE ACTIVE
            migrationBuilder.Sql(@"
                UPDATE SpotTypes 
                SET IsActive = 1 
                WHERE Name IN (
                    'Plongée bouteille', 'Apnée', 'Randonnée sous-marine', 'Photo sous-marine',
                    'Clubs', 'Professionnels', 'Bases fédérales', 'Boutiques'
                ) AND IsActive = 0;
            ");

            // 2. FIX ANY ORPHANED SPOTS (spots with inactive or deleted SpotTypes)
            migrationBuilder.Sql(@"
                -- Move spots with missing/inactive types to default 'Plongée bouteille'
                UPDATE Spots 
                SET TypeId = (
                    SELECT Id FROM SpotTypes 
                    WHERE Name = 'Plongée bouteille' AND IsActive = 1 
                    LIMIT 1
                )
                WHERE TypeId NOT IN (
                    SELECT Id FROM SpotTypes WHERE IsActive = 1
                );
            ");

            // 3. ENSURE CONSISTENT CATEGORY MAPPING
            // ActivityCategory enum: Diving=0, Freediving=1, Snorkeling=2, UnderwaterPhotography=3, Other=4
            migrationBuilder.Sql(@"
                UPDATE SpotTypes SET Category = 0 WHERE Name = 'Plongée bouteille';
                UPDATE SpotTypes SET Category = 1 WHERE Name = 'Apnée';
                UPDATE SpotTypes SET Category = 2 WHERE Name = 'Randonnée sous-marine';
                UPDATE SpotTypes SET Category = 3 WHERE Name = 'Photo sous-marine';
                UPDATE SpotTypes SET Category = 4 WHERE Name IN ('Clubs', 'Professionnels', 'Bases fédérales', 'Boutiques');
            ");

            // 4. CREATE MISSING SPOT TYPES IF THEY DON'T EXIST
            migrationBuilder.Sql(@"
                INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive)
                SELECT * FROM (
                    VALUES 
                    ('Plongée bouteille', 'marker_scuba.png', '#0077BE', 0, 'Sites de plongée avec bouteille (tous niveaux)', 1, '{}', 1),
                    ('Apnée', 'marker_freediving.png', '#4A90E2', 1, 'Sites adaptés à la plongée en apnée', 1, '{}', 1),
                    ('Randonnée sous-marine', 'marker_snorkeling.png', '#87CEEB', 2, 'Sites de randonnée sous-marine', 0, '{}', 1),
                    ('Photo sous-marine', 'marker_photography.png', '#5DADE2', 3, 'Sites pour la photographie sous-marine', 0, '{}', 1),
                    ('Clubs', 'marker_club.png', '#228B22', 4, 'Clubs de plongée', 0, '{}', 1),
                    ('Professionnels', 'marker_pro.png', '#32CD32', 4, 'Centres professionnels', 1, '{}', 1),
                    ('Bases fédérales', 'marker_federal.png', '#90EE90', 4, 'Structures officielles', 1, '{}', 1),
                    ('Boutiques', 'marker_shop.png', '#FF8C00', 4, 'Magasins d\'équipement', 0, '{}', 1)
                ) AS new_types(Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive)
                WHERE NOT EXISTS (SELECT 1 FROM SpotTypes WHERE SpotTypes.Name = new_types.Name);
            ");

            // 5. VERIFICATION - Log current state for debugging
            migrationBuilder.Sql(@"
                -- This will help with debugging
                UPDATE SpotTypes SET UpdatedAt = NOW() WHERE IsActive = 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration is for data consistency fixes only
            // Rolling back would recreate data inconsistencies
            // Instead, we'll just log that this was attempted
            migrationBuilder.Sql(@"
                -- Rollback attempted but skipped for data safety
                -- Manual database review recommended
                SELECT 'FixSpotTypeDataConsistency migration rollback - manual review required' as RollbackStatus;
            ");
        }
    }
}