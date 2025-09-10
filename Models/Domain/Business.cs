using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Domain
{
    public class Business
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

        // Type de commerce (SEULEMENT 3 types)
        [Required]
        public BusinessType BusinessType { get; set; }

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

        // Informations légales
        [MaxLength(20)]
        public string? Siret { get; set; }

        [MaxLength(20)]
        public string? VatNumber { get; set; }

        // Services et produits
        [Column(TypeName = "jsonb")]
        public string? ProductsServices { get; set; }

        [Column(TypeName = "jsonb")]
        public string? BrandsCarried { get; set; }

        [Column(TypeName = "jsonb")]
        public string? RentalEquipment { get; set; }

        public bool RepairServices { get; set; } = false;

        // Horaires
        [Column(TypeName = "jsonb")]
        public string? BusinessHours { get; set; }

        [Column(TypeName = "jsonb")]
        public string? SeasonalSchedule { get; set; }

        // Informations commerciales
        public PriceRange PriceRange { get; set; } = PriceRange.MidRange;

        public bool AcceptsCreditCards { get; set; } = true;

        public bool DeliveryAvailable { get; set; } = false;

        [Url]
        [MaxLength(255)]
        public string? OnlineStoreUrl { get; set; }

        // Spécialisations
        [Column(TypeName = "jsonb")]
        public string? Specializations { get; set; }

        [Column(TypeName = "jsonb")]
        public string? TargetCustomers { get; set; }

        // Validation commerciale
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

        public Guid? VerifiedBy { get; set; }  // UUID → Guid

        public DateTime? VerifiedAt { get; set; }

        public bool BusinessLicenseVerified { get; set; } = false;

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