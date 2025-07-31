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

        [MinLength(3)]
        [MaxLength(30)]
        [RegularExpression(@"^[a-zA-Z0-9_-]*$")]
        public string? Username { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Display name combining first and last name
        /// </summary>
        [NotMapped]
        public string DisplayName => $"{FirstName} {LastName}".Trim();

        [Url]
        public string? AvatarUrl { get; set; }

        [Required]
        public AccountType AccountType { get; set; } = AccountType.Standard;

        [Required]
        public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Free;

        public ExpertiseLevel? ExpertiseLevel { get; set; }

        /// <summary>
        /// Moderator specialization area (for ExpertModerator account type)
        /// </summary>
        public ModeratorSpecialization ModeratorSpecialization { get; set; } = ModeratorSpecialization.None;

        /// <summary>
        /// Current moderation status (for ExpertModerator account type)
        /// </summary>
        public ModeratorStatus ModeratorStatus { get; set; } = ModeratorStatus.None;

        /// <summary>
        /// User permissions flags for fine-grained access control
        /// </summary>
        public UserPermissions Permissions { get; set; } = UserPermissions.CreateSpots;

        /// <summary>
        /// Date when user became a moderator (null if not a moderator)
        /// </summary>
        public DateTime? ModeratorSince { get; set; }

        /// <summary>
        /// Organization ID for professional users (foreign key for future Organizations table)
        /// </summary>
        public int? OrganizationId { get; set; }

        /// <summary>
        /// JSON storage for user certifications and qualifications
        /// Format: [{"Type": "PADI Open Water", "Level": "Certified", "Date": "2023-01-15"}]
        /// </summary>
        [Column(TypeName = "json")]
        public string? Certifications { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// Whether the user's email has been confirmed
        /// </summary>
        public bool IsEmailConfirmed { get; set; } = false;

        // Relations
        public virtual UserPreferences? Preferences { get; set; }
        public virtual ICollection<Spot> CreatedSpots { get; set; } = new List<Spot>();
        public virtual ICollection<UserFavoriteSpot> FavoriteSpots { get; set; } = new List<UserFavoriteSpot>();
    }
}
