// Models/SpotType.cs
using System.ComponentModel.DataAnnotations;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Domain
{
    public class SpotType
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? IconPath { get; set; }
        
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
        public string? ColorCode { get; set; }
        
        public bool RequiresExpertValidation { get; set; } = false;
        
        // CHANGEMENT : JSON → JSONB
        public Dictionary<string, object>? ValidationCriteria { get; set; }
        
        public ActivityCategory Category { get; set; } = ActivityCategory.Activity;
        
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Relations
        public ICollection<Spot> Spots { get; set; } = new List<Spot>();
    }
}