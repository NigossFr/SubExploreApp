// Models/UserFavoriteSpot.cs
using System.ComponentModel.DataAnnotations;

namespace SubExplore.Models.Domain
{
    public class UserFavoriteSpot
    {
        public Guid Id { get; set; }
        
        [Range(1, int.MaxValue)]
        public Guid UserId { get; set; }
        
        [Range(1, int.MaxValue)]
        public Guid SpotId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        [Range(1, 10)]
        public int Priority { get; set; } = 5;
        
        public bool NotificationEnabled { get; set; } = true;
        
        // Relations
        public User User { get; set; } = null!;
        public Spot Spot { get; set; } = null!;
    }
}