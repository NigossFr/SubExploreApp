// ========================================
// MODÈLES SUPABASE POUR API REST
// ========================================
// Modèles adaptés pour l'API Supabase sans enum mapping

using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SubExplore.Models.Supabase
{
    /// <summary>
    /// Modèle User pour l'API Supabase
    /// Utilise des strings pour les enums pour éviter les problèmes de mapping
    /// </summary>
    [Table("users")]
    public class SupabaseUser : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")]
        public string? PasswordHash { get; set; }

        [Column("username")]
        public string? Username { get; set; }

        [Column("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [Column("last_name")]
        public string LastName { get; set; } = string.Empty;

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        // Enums stockés comme strings - pas de problème de mapping !
        [Column("account_type")]
        public string AccountType { get; set; } = "Standard";

        [Column("subscription_status")]
        public string SubscriptionStatus { get; set; } = "Free";

        [Column("expertise_level")]
        public string? ExpertiseLevel { get; set; }

        [Column("certifications")]
        public object? Certifications { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        [Column("is_email_confirmed")]
        public bool IsEmailConfirmed { get; set; }

        [Column("moderator_specialization")]
        public int ModeratorSpecialization { get; set; } = 0;

        [Column("moderator_status")]
        public int ModeratorStatus { get; set; } = 0;

        [Column("permissions")]
        public int Permissions { get; set; } = 1;

        [Column("moderator_since")]
        public DateTime? ModeratorSince { get; set; }

        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }
    }

    /// <summary>
    /// Modèle SpotType pour l'API Supabase
    /// </summary>
    [Table("spot_types")]
    public class SupabaseSpotType : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

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

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Modèle Spot pour l'API Supabase
    /// </summary>
    [Table("spots")]
    public class SupabaseSpot : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("creator_id")]
        public Guid CreatorId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("latitude")]
        public decimal Latitude { get; set; }

        [Column("longitude")]
        public decimal Longitude { get; set; }

        [Column("difficulty_level")]
        public int? DifficultyLevel { get; set; }

        [Column("type_id")]
        public Guid TypeId { get; set; }

        [Column("required_equipment")]
        public string? RequiredEquipment { get; set; }

        [Column("safety_notes")]
        public string? SafetyNotes { get; set; }

        [Column("best_conditions")]
        public string? BestConditions { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("validation_status")]
        public int ValidationStatus { get; set; } = 0;

        [Column("last_safety_review")]
        public DateTime? LastSafetyReview { get; set; }

        [Column("safety_flags")]
        public object? SafetyFlags { get; set; }

        [Column("max_depth")]
        public decimal? MaxDepth { get; set; }

        [Column("current_strength")]
        public int CurrentStrength { get; set; } = 0;

        [Column("has_mooring")]
        public bool HasMooring { get; set; }

        [Column("bottom_type")]
        public string? BottomType { get; set; }
    }

    /// <summary>
    /// Modèle UserFavoriteSpot pour l'API Supabase
    /// </summary>
    [Table("user_favorite_spots")]
    public class SupabaseUserFavoriteSpot : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("spot_id")]
        public Guid SpotId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("priority")]
        public int Priority { get; set; } = 5;

        [Column("notification_enabled")]
        public bool NotificationEnabled { get; set; } = true;
    }
}