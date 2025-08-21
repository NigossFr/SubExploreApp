// Models/Spot.cs - Adapté pour PostGIS
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SubExplore.Models.Enums;
// 🚫 NetTopologySuite supprimé - Utilisation coordonnées décimales uniquement

namespace SubExplore.Models.Domain
{
    public class Spot
    {
        public Guid Id { get; set; }
        
        public Guid CreatorId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        // 🚫 PostGIS supprimé - Utilisation coordonnées décimales uniquement
        
        [Required]
        [Range(-90, 90)]
        public decimal Latitude { get; set; }
        
        [Required]
        [Range(-180, 180)]
        public decimal Longitude { get; set; }
        
        public DifficultyLevel DifficultyLevel { get; set; }
        
        public Guid TypeId { get; set; }

        [Required]
        public string RequiredEquipment { get; set; } = string.Empty;
        
        [Required]
        public string SafetyNotes { get; set; } = string.Empty;
        
        [Required]
        public string BestConditions { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public SpotValidationStatus ValidationStatus { get; set; } = SpotValidationStatus.Pending;
        
        public DateTime? LastSafetyReview { get; set; }
        
        // CHANGEMENT : JSON → JSONB
        public Dictionary<string, object>? SafetyFlags { get; set; }

        [Range(0, 200)]
        public int? MaxDepth { get; set; }
        
        public CurrentStrength? CurrentStrength { get; set; }
        
        public bool? HasMooring { get; set; }
        
        [StringLength(100)]
        public string? BottomType { get; set; }
        
        // Relations
        public User Creator { get; set; } = null!;
        public SpotType Type { get; set; } = null!;
        public ICollection<SpotMedia> Media { get; set; } = new List<SpotMedia>();
    }
}
