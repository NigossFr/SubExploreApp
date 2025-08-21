// Models/User.cs - Adapté pour PostgreSQL/Supabase
using System.ComponentModel.DataAnnotations;
using SubExplore.Models.Enums;
// 🚫 NetTopologySuite supprimé - Utilisation coordonnées décimales uniquement

namespace SubExplore.Models.Domain
{
    public class User
    {
        // CHANGEMENT MAJEUR : int → Guid pour tous les ID
        public Guid Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [StringLength(30, MinimumLength = 3)]
        public string? Username { get; set; }
        
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;
        
        public string? AvatarUrl { get; set; }

        public AccountType AccountType { get; set; } = AccountType.Standard;
        
        public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Free;
        
        public ExpertiseLevel? ExpertiseLevel { get; set; }
        
        // CHANGEMENT : JSON MySQL → JSONB PostgreSQL
        public Dictionary<string, object>? Certifications { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public DateTime? LastLogin { get; set; }
        
        public bool IsEmailConfirmed { get; set; } = false;
        
        public ModeratorSpecialization ModeratorSpecialization { get; set; } = ModeratorSpecialization.None;
        
        public ModeratorStatus ModeratorStatus { get; set; } = ModeratorStatus.None;
        
        public UserPermissions Permissions { get; set; } = UserPermissions.CreateSpots;
        
        public DateTime? ModeratorSince { get; set; }
        
        public Guid? OrganizationId { get; set; }
        
        // Propriétés calculées pour compatibilité
        public string DisplayName => !string.IsNullOrEmpty(Username) ? Username : $"{FirstName} {LastName}";
        
        // Relations
        public UserPreferences? Preferences { get; set; }
        public ICollection<Spot> Spots { get; set; } = new List<Spot>();
        public ICollection<Spot> CreatedSpots => Spots; // Alias pour compatibilité
        public ICollection<UserFavoriteSpot> FavoriteSpots { get; set; } = new List<UserFavoriteSpot>();
    }
}
