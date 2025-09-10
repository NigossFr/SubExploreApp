using Postgrest.Attributes;
using Postgrest.Models;

namespace SubExplore.Models.Supabase
{
    /// <summary>
    /// Modèle SpotType pour l'API Supabase
    /// </summary>
    [Table("spot_types")]
    public class SupabaseSpotType : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }  // UUID en DB → Guid en C#

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("icon_path")]
        public string? IconPath { get; set; }

        [Column("color_code")]
        public string ColorCode { get; set; } = "#000000";

        [Column("requires_expert_validation")]
        public bool RequiresExpertValidation { get; set; }

        [Column("validation_criteria")]
        public object? ValidationCriteria { get; set; }

        // Enum stocké comme string
        [Column("category")]
        public string Category { get; set; } = "Activity";

        [Column("description")]
        public string? Description { get; set; }


        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}