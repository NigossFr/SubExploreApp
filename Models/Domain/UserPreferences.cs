// Models/UserPreferences.cs
using System.ComponentModel.DataAnnotations;

namespace SubExplore.Models.Domain
{
    public class UserPreferences
    {
        public Guid Id { get; set; }
        
        public Guid UserId { get; set; }
        
        [StringLength(20)]
        public string Theme { get; set; } = "light";
        
        [StringLength(20)]
        public string DisplayNamePreference { get; set; } = "username";
        
        // CHANGEMENT : JSON → JSONB
        public Dictionary<string, object> NotificationSettings { get; set; } = new();
        
        [StringLength(5)]
        public string Language { get; set; } = "fr";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Relations
        public User User { get; set; } = null!;
    }
}