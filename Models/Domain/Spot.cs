using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Domain
{
    public class Spot
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CreatorId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

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

        [Required]
        public DifficultyLevel DifficultyLevel { get; set; }

        [Required]
        public int TypeId { get; set; }

        [Required]
        public string RequiredEquipment { get; set; } = string.Empty;

        [Required]
        public string SafetyNotes { get; set; } = string.Empty;

        [Required]
        public string BestConditions { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public SpotValidationStatus ValidationStatus { get; set; } = SpotValidationStatus.Pending;

        public DateTime? LastSafetyReview { get; set; }

        [Column(TypeName = "json")]
        public string? SafetyFlags { get; set; }

        // Caractéristiques spécifiques aux activités
        [Range(0, 200)]
        public int? MaxDepth { get; set; }

        public CurrentStrength? CurrentStrength { get; set; }

        public bool? HasMooring { get; set; }

        [MaxLength(100)]
        public string? BottomType { get; set; }

        // Navigation properties
        [ForeignKey("CreatorId")]
        public virtual User? Creator { get; set; }

        [ForeignKey("TypeId")]
        public virtual SpotType? Type { get; set; }

        public virtual ICollection<SpotMedia> Media { get; set; } = new List<SpotMedia>();
        public virtual ICollection<UserFavoriteSpot> UserFavorites { get; set; } = new List<UserFavoriteSpot>();
    }
}
