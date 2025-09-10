using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Domain
{
    public class Organization
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid CreatorId { get; set; }  // UUID → Guid

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,8)")]
        public decimal Latitude { get; set; }

        [Required]
        [Column(TypeName = "decimal(11,8)")]
        public decimal Longitude { get; set; }

        // Type d'organisation
        [Required]
        public OrganizationType OrganizationType { get; set; }

        // Informations de contact
        [Required]
        public string Address { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? PostalCode { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Country { get; set; } = "France";

        [MaxLength(20)]
        public string? Phone { get; set; }

        [EmailAddress]
        [MaxLength(255)]
        public string? Email { get; set; }

        [Url]
        [MaxLength(255)]
        public string? Website { get; set; }

        // Informations légales et certifications
        [MaxLength(50)]
        public string? RegistrationNumber { get; set; }

        [Column(TypeName = "jsonb")]
        public string? Certifications { get; set; }

        [Column(TypeName = "jsonb")]
        public string? InsuranceInfo { get; set; }

        // Services proposés
        [Column(TypeName = "jsonb")]
        public string? ServicesOffered { get; set; }

        [Column(TypeName = "jsonb")]
        public string? Specializations { get; set; }

        public bool EquipmentRental { get; set; } = false;

        public bool Accommodation { get; set; } = false;

        // Horaires
        [Column(TypeName = "jsonb")]
        public string? BusinessHours { get; set; }

        [Column(TypeName = "jsonb")]
        public string? SeasonalSchedule { get; set; }

        // Tarification
        [Column(TypeName = "jsonb")]
        public string? MembershipFees { get; set; }

        [Column(TypeName = "jsonb")]
        public string? PricingInfo { get; set; }

        // Capacités
        public int? MaxStudentsPerSession { get; set; }

        public int? MaxDiversPerTrip { get; set; }

        public int BoatsAvailable { get; set; } = 0;

        // Validation et vérification
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

        public Guid? VerifiedBy { get; set; }  // UUID → Guid

        public DateTime? VerifiedAt { get; set; }

        [Column(TypeName = "jsonb")]
        public string? VerificationDocuments { get; set; }

        // Timestamps
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CreatorId")]
        public virtual User? Creator { get; set; }

        [ForeignKey("VerifiedBy")]
        public virtual User? Verifier { get; set; }

        public virtual ICollection<UnifiedMedia> Media { get; set; } = new List<UnifiedMedia>();
    }
}