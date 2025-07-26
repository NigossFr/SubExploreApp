using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Domain
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MinLength(3)]
        [MaxLength(30)]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Url]
        public string? AvatarUrl { get; set; }

        [Required]
        public AccountType AccountType { get; set; } = AccountType.Standard;

        [Required]
        public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Free;

        public ExpertiseLevel? ExpertiseLevel { get; set; }

        [Column(TypeName = "json")]
        public string? Certifications { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastLogin { get; set; }

        // Relations
        public virtual UserPreferences? Preferences { get; set; }
        public virtual ICollection<Spot> CreatedSpots { get; set; } = new List<Spot>();
        public virtual ICollection<UserFavoriteSpot> FavoriteSpots { get; set; } = new List<UserFavoriteSpot>();
    }
}
