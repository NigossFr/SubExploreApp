// ========================================
// MODÈLES SUPABASE POUR NOUVELLE ARCHITECTURE 3 TABLES
// ========================================
// Modèles pour practice_spots, organizations, businesses

using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SubExplore.Models.Supabase
{
    /// <summary>
    /// Modèle PracticeSpot pour table practice_spots
    /// </summary>
    [Table("practice_spots")]
    public class SupabasePracticeSpot : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }  // SERIAL en DB → int en C#

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("latitude")]
        public decimal Latitude { get; set; }

        [Column("longitude")]
        public decimal Longitude { get; set; }

        [Column("difficulty_level")]
        public string? DifficultyLevel { get; set; }

        [Column("max_depth")]
        public decimal? MaxDepth { get; set; }

        [Column("current_strength")]
        public string? CurrentStrength { get; set; }

        [Column("has_mooring")]
        public bool HasMooring { get; set; }

        [Column("bottom_type")]
        public string? BottomType { get; set; }

        [Column("required_equipment")]
        public string? RequiredEquipment { get; set; }

        [Column("safety_notes")]
        public string? SafetyNotes { get; set; }

        [Column("best_conditions")]
        public string? BestConditions { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("creator_id")]
        public Guid CreatorId { get; set; }  // UUID vers users.id

        [Column("validation_status")]
        public string ValidationStatus { get; set; } = "pending";

        [Column("last_safety_review")]
        public DateTime? LastSafetyReview { get; set; }

        [Column("safety_flags")]
        public object? SafetyFlags { get; set; }
    }

    /// <summary>
    /// Modèle Organization pour table organizations
    /// </summary>
    [Table("organizations")]
    public class SupabaseOrganization : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }  // SERIAL en DB → int en C#

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("latitude")]
        public decimal Latitude { get; set; }

        [Column("longitude")]
        public decimal Longitude { get; set; }

        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Column("city")]
        public string City { get; set; } = string.Empty;

        [Column("postal_code")]
        public string? PostalCode { get; set; }

        [Column("country")]
        public string Country { get; set; } = string.Empty;

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("website")]
        public string? Website { get; set; }

        [Column("organization_type")]
        public string OrganizationType { get; set; } = string.Empty;

        [Column("services")]
        public object? Services { get; set; }

        [Column("certifications")]
        public object? Certifications { get; set; }

        [Column("operating_hours")]
        public object? OperatingHours { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Modèle Business pour table businesses
    /// </summary>
    [Table("businesses")]
    public class SupabaseBusiness : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }  // SERIAL en DB → int en C#

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("latitude")]
        public decimal Latitude { get; set; }

        [Column("longitude")]
        public decimal Longitude { get; set; }

        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Column("city")]
        public string City { get; set; } = string.Empty;

        [Column("postal_code")]
        public string? PostalCode { get; set; }

        [Column("country")]
        public string Country { get; set; } = string.Empty;

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("website")]
        public string? Website { get; set; }

        [Column("business_type")]
        public string BusinessType { get; set; } = string.Empty;

        [Column("services")]
        public object? Services { get; set; }

        [Column("price_range")]
        public string? PriceRange { get; set; }

        [Column("payment_methods")]
        public object? PaymentMethods { get; set; }

        [Column("operating_hours")]
        public object? OperatingHours { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}