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
    public class SpotMedia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SpotId { get; set; }

        [Required]
        public MediaType MediaType { get; set; }

        [Required]
        [MaxLength(500)]
        [Url]
        public string MediaUrl { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public MediaStatus Status { get; set; } = MediaStatus.Pending;

        public string? Caption { get; set; }

        [Required]
        public bool IsPrimary { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        [Range(0, 5242880)] // 5MB en bytes
        public long? FileSize { get; set; }

        public string? ContentType { get; set; }

        // Navigation properties
        [ForeignKey("SpotId")]
        public virtual Spot? Spot { get; set; }
    }
}
