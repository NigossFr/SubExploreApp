using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Domain
{
    public class PracticeSpot
    {
        [Key]
        public int Id { get; set; }  // SERIAL dans DB, mais reste int en C#

        [Required]
        public Guid CreatorId { get; set; }  // UUID → Guid

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(-90, 90)]
        [Column(TypeName = "decimal(10,8)")]
        public decimal Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        [Column(TypeName = "decimal(11,8)")]
        public decimal Longitude { get; set; }

        // Spécifique aux pratiques
        [Required]
        public Guid SpotTypeId { get; set; }  // UUID → Guid

        [Required]
        public DifficultyLevel DifficultyLevel { get; set; }

        [Range(0, 200)]
        public int? MaxDepth { get; set; }

        public CurrentStrength CurrentStrength { get; set; } = CurrentStrength.None;

        [MaxLength(100)]
        public string? BottomType { get; set; }

        public bool HasMooring { get; set; } = false;

        [Required]
        public string RequiredEquipment { get; set; } = string.Empty;

        [Required]
        public string SafetyNotes { get; set; } = string.Empty;

        [Required]
        public string BestConditions { get; set; } = string.Empty;

        // Conditions météo et saisonnières
        [Column(TypeName = "jsonb")]
        public string? BestWeatherConditions { get; set; }

        [Column(TypeName = "jsonb")]
        public string? SeasonalRecommendations { get; set; }

        // Gestion et validation
        [Required]
        public SpotValidationStatus ValidationStatus { get; set; } = SpotValidationStatus.Pending;

        public Guid? ValidatedBy { get; set; }  // UUID → Guid

        public DateTime? ValidatedAt { get; set; }

        public DateTime? LastSafetyReview { get; set; }

        [Column(TypeName = "jsonb")]
        public string? SafetyFlags { get; set; }

        // Timestamps
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CreatorId")]
        public virtual User? Creator { get; set; }

        [ForeignKey("SpotTypeId")]
        public virtual SpotType? SpotType { get; set; }

        [ForeignKey("ValidatedBy")]
        public virtual User? Validator { get; set; }

        public virtual ICollection<UnifiedMedia> Media { get; set; } = new List<UnifiedMedia>();
    }
}