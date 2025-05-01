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
    public class SpotType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? IconPath { get; set; }

        [MaxLength(7)]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
        public string? ColorCode { get; set; }

        [Required]
        public bool RequiresExpertValidation { get; set; }

        [Column(TypeName = "json")]
        public string? ValidationCriteria { get; set; }

        [Required]
        public ActivityCategory Category { get; set; }

        public string? Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Spot> Spots { get; set; } = new List<Spot>();
    }
}
