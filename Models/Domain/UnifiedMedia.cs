using System.ComponentModel.DataAnnotations;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Domain
{
    public class UnifiedMedia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public EntityType EntityType { get; set; }

        [Required]
        public int EntityId { get; set; }

        [Required]
        public MediaType MediaType { get; set; }

        [Required]
        [MaxLength(500)]
        public string MediaUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ThumbnailUrl { get; set; }

        public string? Caption { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsPrimary { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}