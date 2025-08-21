// Models/SpotMedia.cs
using System.ComponentModel.DataAnnotations;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Domain
{
    public class SpotMedia
    {
        public Guid Id { get; set; }
        
        public Guid SpotId { get; set; }
        
        public MediaType MediaType { get; set; }
        
        [Required]
        [StringLength(500)]
        public string MediaUrl { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public MediaStatus Status { get; set; } = MediaStatus.Pending;
        
        public string? Caption { get; set; }
        
        public bool IsPrimary { get; set; } = false;
        
        public int? Width { get; set; }
        
        public int? Height { get; set; }
        
        [Range(0, 5242880)] // Max 5MB
        public long? FileSize { get; set; }
        
        public string? ContentType { get; set; }
        
        // Relations
        public Spot Spot { get; set; } = null!;
    }
}