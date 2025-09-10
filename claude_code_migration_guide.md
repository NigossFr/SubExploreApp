# Guide Complet Claude Code - Migration Architecture 3 Tables + Hybride PostGIS

## 🎯 **CONTEXTE CRITIQUE**

La base de données SubExplore a été **COMPLÈTEMENT MIGRÉE** d'une architecture unifiée vers une **architecture 3 tables spécialisées**. Le code C# doit être entièrement adapté.

### **Migration Effectuée**
- ✅ **AVANT** : 1 table `spots` pour tout (spots + structures + boutiques)  
- ✅ **APRÈS** : 3 tables spécialisées :
  - `practice_spots` → Lieux de pratique (plongée, apnée, etc.)
  - `organizations` → Structures (clubs, SCA, bases fédérales)  
  - `businesses` → Boutiques spécialisées
- ✅ **Suppression** : Ancienne table `spots` et types inadéquats
- ✅ **Fonctions PostGIS** : Recherche géographique hybride opérationnelle

### **Spécificités Supabase UUID**
- **CRITIQUE** : Tous les IDs sont des `UUID`, pas des `int`
- **FK vers** : `public.users(id)` (UUID)
- **Types spots** : `spot_types.id` (UUID)

## 🚀 **OBJECTIFS POUR CLAUDE CODE**

### **1. Adapter Tous les Modèles C#** 
- Remplacer `Spot.cs` par `PracticeSpot.cs`, `Organization.cs`, `Business.cs`
- Convertir tous les IDs `int` → `Guid` (UUID)
- Créer le modèle `UnifiedMedia.cs`

### **2. Implémenter l'Approche Hybride PostGIS**
- Utiliser les fonctions DB pour la géolocalisation (performance)
- Garder la logique métier en C# (maintenabilité)
- Pattern Repository modernisé

### **3. Préserver TOUTES les Fonctionnalités Existantes**
- MapViewModel doit continuer à fonctionner
- Filtres et recherches conservés
- Navigation et UI intactes

## 📋 **NOUVEAUX MODÈLES C# À CRÉER**

### **Models/Domain/PracticeSpot.cs**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Domain
{
    public class PracticeSpot
    {
        [Key]
        public int Id { get; set; }  // SERIAL dans DB, mais reste int en C#

        [Required]
        public Guid CreatorId { get; set; }  // UUID → Guid

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(-90, 90)]
        [Column(TypeName = "decimal(10,8)")]
        public decimal Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        [Column(TypeName = "decimal(11,8)")]
        public decimal Longitude { get; set; }

        // Spécifique aux pratiques
        [Required]
        public Guid SpotTypeId { get; set; }  // UUID → Guid

        [Required]
        public DifficultyLevel DifficultyLevel { get; set; }

        [Range(0, 200)]
        public int? MaxDepth { get; set; }

        public CurrentStrength CurrentStrength { get; set; } = CurrentStrength.None;

        [MaxLength(100)]
        public string? BottomType { get; set; }

        public bool HasMooring { get; set; } = false;

        [Required]
        public string RequiredEquipment { get; set; } = string.Empty;

        [Required]
        public string SafetyNotes { get; set; } = string.Empty;

        [Required]
        public string BestConditions { get; set; } = string.Empty;

        // Conditions météo et saisonnières
        [Column(TypeName = "jsonb")]
        public string? BestWeatherConditions { get; set; }

        [Column(TypeName = "jsonb")]
        public string? SeasonalRecommendations { get; set; }

        // Gestion et validation
        [Required]
        public SpotValidationStatus ValidationStatus { get; set; } = SpotValidationStatus.Pending;

        public Guid? ValidatedBy { get; set; }  // UUID → Guid

        public DateTime? ValidatedAt { get; set; }

        public DateTime? LastSafetyReview { get; set; }

        [Column(TypeName = "jsonb")]
        public string? SafetyFlags { get; set; }

        // Timestamps
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CreatorId")]
        public virtual User? Creator { get; set; }

        [ForeignKey("SpotTypeId")]
        public virtual SpotType? SpotType { get; set; }

        [ForeignKey("ValidatedBy")]
        public virtual User? Validator { get; set; }

        public virtual ICollection<UnifiedMedia> Media { get; set; } = new List<UnifiedMedia>();
    }
}
```

### **Models/Domain/Organization.cs**
```csharp
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
```

### **Models/Domain/Business.cs**
```csharp
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
```

### **Models/Domain/UnifiedMedia.cs**
```csharp
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
```

## 🔧 **NOUVEAUX ENUMS À CRÉER/MODIFIER**

### **Models/Enums/OrganizationType.cs**
```csharp
namespace SubExplore.Models.Enums
{
    public enum OrganizationType
    {
        ClubFFESSM,
        SCA,
        FederalBase,
        DiveCenter
    }
}
```

### **Models/Enums/BusinessType.cs** 
```csharp
namespace SubExplore.Models.Enums
{
    public enum BusinessType
    {
        DiveShop,        // Magasin de plongée
        EquipmentRental, // Location de matériel
        BoatCharter     // Service bateau
    }
}
```

### **Models/Enums/EntityType.cs**
```csharp
namespace SubExplore.Models.Enums
{
    public enum EntityType
    {
        PracticeSpot,
        Organization,
        Business,
        Story
    }
}
```

### **Models/Enums/VerificationStatus.cs**
```csharp
namespace SubExplore.Models.Enums
{
    public enum VerificationStatus
    {
        Pending,
        Verified,
        Rejected
    }
}
```

### **Models/Enums/PriceRange.cs**
```csharp
namespace SubExplore.Models.Enums
{
    public enum PriceRange
    {
        Budget,
        MidRange,
        Premium
    }
}
```

### **Models/Enums/CurrentStrength.cs** (Modifier)
```csharp
namespace SubExplore.Models.Enums
{
    public enum CurrentStrength
    {
        None,
        Light,
        Moderate,
        Strong
    }
}
```

## 🗃️ **DBCONTEXT MIS À JOUR**

### **DataAccess/SubExploreDbContext.cs**
```csharp
using Microsoft.EntityFrameworkCore;
using SubExplore.Models.Domain;

namespace SubExplore.DataAccess
{
    public class SubExploreDbContext : DbContext
    {
        public SubExploreDbContext(DbContextOptions<SubExploreDbContext> options)
            : base(options)
        {
        }

        // NOUVELLES entités principales
        public DbSet<PracticeSpot> PracticeSpots { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Business> Businesses { get; set; }
        public DbSet<UnifiedMedia> UnifiedMedia { get; set; }

        // Entités existantes (à conserver)
        public DbSet<User> Users { get; set; }
        public DbSet<SpotType> SpotTypes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration des nouvelles entités
            ConfigurePracticeSpots(modelBuilder);
            ConfigureOrganizations(modelBuilder);
            ConfigureBusinesses(modelBuilder);
            ConfigureUnifiedMedia(modelBuilder);

            // Configuration des entités existantes
            ConfigureExistingEntities(modelBuilder);
        }

        private void ConfigurePracticeSpots(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PracticeSpot>(entity =>
            {
                entity.ToTable("practice_spots");
                
                // Relations
                entity.HasOne(e => e.Creator)
                      .WithMany()
                      .HasForeignKey(e => e.CreatorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SpotType)
                      .WithMany()
                      .HasForeignKey(e => e.SpotTypeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Validator)
                      .WithMany()
                      .HasForeignKey(e => e.ValidatedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                // Configuration des enums
                entity.Property(e => e.DifficultyLevel)
                      .HasConversion<string>();

                entity.Property(e => e.CurrentStrength)
                      .HasConversion<string>();

                entity.Property(e => e.ValidationStatus)
                      .HasConversion<string>();

                // Propriétés JSON pour PostgreSQL/Supabase
                entity.Property(e => e.BestWeatherConditions)
                      .HasColumnType("jsonb");

                entity.Property(e => e.SeasonalRecommendations)
                      .HasColumnType("jsonb");

                entity.Property(e => e.SafetyFlags)
                      .HasColumnType("jsonb");
            });
        }

        private void ConfigureOrganizations(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Organization>(entity =>
            {
                entity.ToTable("organizations");
                
                // Relations
                entity.HasOne(e => e.Creator)
                      .WithMany()
                      .HasForeignKey(e => e.CreatorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Verifier)
                      .WithMany()
                      .HasForeignKey(e => e.VerifiedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                // Configuration des enums
                entity.Property(e => e.OrganizationType)
                      .HasConversion<string>();

                entity.Property(e => e.VerificationStatus)
                      .HasConversion<string>();

                // Propriétés JSON
                entity.Property(e => e.Certifications)
                      .HasColumnType("jsonb");

                entity.Property(e => e.InsuranceInfo)
                      .HasColumnType("jsonb");

                entity.Property(e => e.ServicesOffered)
                      .HasColumnType("jsonb");

                entity.Property(e => e.Specializations)
                      .HasColumnType("jsonb");

                entity.Property(e => e.BusinessHours)
                      .HasColumnType("jsonb");

                entity.Property(e => e.SeasonalSchedule)
                      .HasColumnType("jsonb");

                entity.Property(e => e.MembershipFees)
                      .HasColumnType("jsonb");

                entity.Property(e => e.PricingInfo)
                      .HasColumnType("jsonb");

                entity.Property(e => e.VerificationDocuments)
                      .HasColumnType("jsonb");
            });
        }

        private void ConfigureBusinesses(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Business>(entity =>
            {
                entity.ToTable("businesses");
                
                // Relations
                entity.HasOne(e => e.Creator)
                      .WithMany()
                      .HasForeignKey(e => e.CreatorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Verifier)
                      .WithMany()
                      .HasForeignKey(e => e.VerifiedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                // Configuration des enums
                entity.Property(e => e.BusinessType)
                      .HasConversion<string>();

                entity.Property(e => e.VerificationStatus)
                      .HasConversion<string>();

                entity.Property(e => e.PriceRange)
                      .HasConversion<string>();

                // Propriétés JSON
                entity.Property(e => e.ProductsServices)
                      .HasColumnType("jsonb");

                entity.Property(e => e.BrandsCarried)
                      .HasColumnType("jsonb");

                entity.Property(e => e.RentalEquipment)
                      .HasColumnType("jsonb");

                entity.Property(e => e.BusinessHours)
                      .HasColumnType("jsonb");

                entity.Property(e => e.SeasonalSchedule)
                      .HasColumnType("jsonb");

                entity.Property(e => e.Specializations)
                      .HasColumnType("jsonb");

                entity.Property(e => e.TargetCustomers)
                      .HasColumnType("jsonb");
            });
        }

        private void ConfigureUnifiedMedia(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UnifiedMedia>(entity =>
            {
                entity.ToTable("unified_media");

                // Configuration des enums
                entity.Property(e => e.EntityType)
                      .HasConversion<string>();

                entity.Property(e => e.MediaType)
                      .HasConversion<string>();
            });
        }

        private void ConfigureExistingEntities(ModelBuilder modelBuilder)
        {
            // Configuration User (existante)
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                
                entity.HasOne(e => e.Preferences)
                      .WithOne(e => e.User)
                      .HasForeignKey<UserPreferences>(e => e.UserId);
            });

            // Configuration SpotType (existante)
            modelBuilder.Entity<SpotType>(entity =>
            {
                entity.Property(e => e.Category)
                      .HasConversion<string>();

                entity.Property(e => e.ValidationCriteria)
                      .HasColumnType("jsonb");
            });
        }
    }
}
```

## 🔄 **REPOSITORIES HYBRIDES (Approche PostGIS)**

### **Repositories/Interfaces/IPracticeSpotRepository.cs**
```csharp
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Repositories.Interfaces
{
    public interface IPracticeSpotRepository : IGenericRepository<PracticeSpot>
    {
        // Approche HYBRIDE : Utilise les fonctions PostGIS
        Task<IEnumerable<PracticeSpot>> GetNearbyPracticeSpotsAsync(decimal latitude, decimal longitude, int radiusKm = 10, Guid? spotTypeFilter = null);
        
        // Méthodes classiques en C#
        Task<IEnumerable<PracticeSpot>> GetBySpotTypeAsync(Guid spotTypeId);
        Task<IEnumerable<PracticeSpot>> GetByDifficultyLevelAsync(DifficultyLevel difficulty);
        Task<IEnumerable<PracticeSpot>> GetByCreatorAsync(Guid creatorId);
        Task<IEnumerable<PracticeSpot>> GetByValidationStatusAsync(SpotValidationStatus status);
        Task<IEnumerable<PracticeSpot>> SearchPracticeSpotsAsync(string query);
        Task<bool> ValidateSpotAsync(int spotId, Guid validatorId);
    }
}
```

### **Repositories/Implementations/PracticeSpotRepository.cs**
```csharp
using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;

namespace SubExplore.Repositories.Implementations
{
    public class PracticeSpotRepository : GenericRepository<PracticeSpot>, IPracticeSpotRepository
    {
        public PracticeSpotRepository(SubExploreDbContext context) : base(context)
        {
        }

        // APPROCHE HYBRIDE : Fonction PostGIS pour la géolocalisation
        public async Task<IEnumerable<PracticeSpot>> GetNearbyPracticeSpotsAsync(decimal latitude, decimal longitude, int radiusKm = 10, Guid? spotTypeFilter = null)
        {
            var sql = "SELECT * FROM find_practice_spots_near_location({0}, {1}, {2}, {3})";
            
            // Exécuter la fonction PostGIS et récupérer les IDs
            var nearbySpotIds = await _context.Database
                .SqlQueryRaw<int>(sql, latitude, longitude, radiusKm, spotTypeFilter)
                .ToListAsync();

            // Récupérer les objets complets via EF Core (avec navigation properties)
            return await _context.PracticeSpots
                .Include(s => s.SpotType)
                .Include(s => s.Creator)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => nearbySpotIds.Contains(s.Id))
                .ToListAsync();
        }

        public async Task<IEnumerable<PracticeSpot>> GetBySpotTypeAsync(Guid spotTypeId)
        {
            return await _context.PracticeSpots
                .Include(s => s.SpotType)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.SpotTypeId == spotTypeId && s.ValidationStatus == SpotValidationStatus.Approved)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PracticeSpot>> GetByDifficultyLevelAsync(DifficultyLevel difficulty)
        {
            return await _context.PracticeSpots
                .Include(s => s.SpotType)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.DifficultyLevel == difficulty && s.ValidationStatus == SpotValidationStatus.Approved)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PracticeSpot>> GetByCreatorAsync(Guid creatorId)
        {
            return await _context.PracticeSpots
                .Include(s => s.SpotType)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.CreatorId == creatorId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PracticeSpot>> GetByValidationStatusAsync(SpotValidationStatus status)
        {
            return await _context.PracticeSpots
                .Include(s => s.SpotType)
                .Include(s => s.Creator)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.ValidationStatus == status)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PracticeSpot>> SearchPracticeSpotsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetAllAsync();

            string normalizedQuery = query.ToLower();

            return await _context.PracticeSpots
                .Include(s => s.SpotType)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => (s.Name.ToLower().Contains(normalizedQuery) ||
                            s.Description.ToLower().Contains(normalizedQuery)) &&
                            s.ValidationStatus == SpotValidationStatus.Approved)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ValidateSpotAsync(int spotId, Guid validatorId)
        {
            var spot = await GetByIdAsync(spotId);
            if (spot == null)
                return false;

            spot.ValidationStatus = SpotValidationStatus.Approved;
            spot.ValidatedBy = validatorId;
            spot.ValidatedAt = DateTime.UtcNow;
            spot.UpdatedAt = DateTime.UtcNow;

            await UpdateAsync(spot);
            return true;
        }
    }
}
```

### **Repositories similaires pour Organizations et Businesses**
Créer `IOrganizationRepository`, `IBusinessRepository` avec les mêmes patterns hybrides utilisant les fonctions PostGIS respectives.

## 🎯 **SERVICES ADAPTÉS**

### **Services/Interfaces/IPracticeSpotService.cs**
```csharp
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    public interface IPracticeSpotService
    {
        Task<IEnumerable<PracticeSpot>> GetNearbyPracticeSpotsAsync(decimal latitude, decimal longitude, int radiusKm = 10, Guid? spotTypeFilter = null);
        Task<IEnumerable<PracticeSpot>> GetPracticeSpotsByTypeAsync(Guid spotTypeId);
        Task<IEnumerable<PracticeSpot>> GetPracticeSpotsByDifficultyAsync(DifficultyLevel difficulty);
        Task<PracticeSpot?> GetPracticeSpotByIdAsync(int id);
        Task<PracticeSpot> CreatePracticeSpotAsync(PracticeSpot spot);
        Task<PracticeSpot> UpdatePracticeSpotAsync(PracticeSpot spot);
        Task<bool> DeletePracticeSpotAsync(int id);
        Task<IEnumerable<PracticeSpot>> SearchPracticeSpotsAsync(string query);
        Task<bool> ValidatePracticeSpotAsync(int spotId, Guid validatorId);
    }
}
```

### **Services/Implementations/PracticeSpotService.cs**
```csharp
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    public class PracticeSpotService : IPracticeSpotService
    {
        private readonly IPracticeSpotRepository _practiceSpotRepository;
        private readonly ILogger<PracticeSpotService> _logger;

        public PracticeSpotService(IPracticeSpotRepository practiceSpotRepository, ILogger<PracticeSpotService> logger)
        {
            _practiceSpotRepository = practiceSpotRepository;
            _logger = logger;
        }

        // GÉOLOCALISATION HYBRIDE avec PostGIS
        public async Task<IEnumerable<PracticeSpot>> GetNearbyPracticeSpotsAsync(decimal latitude, decimal longitude, int radiusKm = 10, Guid? spotTypeFilter = null)
        {
            try
            {
                return await _practiceSpotRepository.GetNearbyPracticeSpotsAsync(latitude, longitude, radiusKm, spotTypeFilter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nearby practice spots");
                throw;
            }
        }

        // Autres méthodes...
        public async Task<IEnumerable<PracticeSpot>> GetPracticeSpotsByTypeAsync(Guid spotTypeId)
        {
            try
            {
                return await _practiceSpotRepository.GetBySpotTypeAsync(spotTypeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting practice spots by type {SpotTypeId}", spotTypeId);
                throw;
            }
        }

        // ... Implémenter toutes les autres méthodes de l'interface
    }
}
```

## 🗺️ **MAPVIEWMODEL ADAPTÉ**

### **ViewModels/Main/MapViewModel.cs (Mise à jour)**
```csharp
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Maps;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Main
{
    public partial class MapViewModel : ViewModelBase
    {
        // NOUVEAUX SERVICES pour les 3 entités
        private readonly IPracticeSpotService _practiceSpotService;
        private readonly IOrganizationService _organizationService;
        private readonly IBusinessService _businessService;
        private readonly ILocationService _locationService;

        [ObservableProperty]
        private Location? currentLocation;

        [ObservableProperty]
        private bool isLocationLoading;

        [ObservableProperty]
        private string selectedFilter = "all";

        // NOUVELLES COLLECTIONS pour les 3 types d'entités
        public ObservableCollection<PracticeSpot> PracticeSpots { get; } = new();
        public ObservableCollection<Organization> Organizations { get; } = new();
        public ObservableCollection<Business> Businesses { get; } = new();

        // Propriétés de filtrage
        [ObservableProperty]
        private bool showPracticeSpots = true;

        [ObservableProperty]
        private bool showOrganizations = true;

        [ObservableProperty]
        private bool showBusinesses = true;

        [ObservableProperty]
        private int searchRadius = 10;

        public MapViewModel(
            IPracticeSpotService practiceSpotService,
            IOrganizationService organizationService,
            IBusinessService businessService,
            ILocationService locationService)
        {
            _practiceSpotService = practiceSpotService;
            _organizationService = organizationService;
            _businessService = businessService;
            _locationService = locationService;

            Title = "Carte";
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsBusy || CurrentLocation == null)
                return;

            try
            {
                IsBusy = true;

                var tasks = new List<Task>();

                if (ShowPracticeSpots)
                {
                    tasks.Add(LoadPracticeSpotsAsync());
                }

                if (ShowOrganizations)
                {
                    tasks.Add(LoadOrganizationsAsync());
                }

                if (ShowBusinesses)
                {
                    tasks.Add(LoadBusinessesAsync());
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les données.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // UTILISATION DE L'APPROCHE HYBRIDE PostGIS
        private async Task LoadPracticeSpotsAsync()
        {
            if (CurrentLocation == null) return;

            var spots = await _practiceSpotService.GetNearbyPracticeSpotsAsync(
                (decimal)CurrentLocation.Latitude,
                (decimal)CurrentLocation.Longitude,
                SearchRadius);

            PracticeSpots.Clear();
            foreach (var spot in spots)
            {
                PracticeSpots.Add(spot);
            }
        }

        private async Task LoadOrganizationsAsync()
        {
            if (CurrentLocation == null) return;

            var organizations = await _organizationService.GetNearbyOrganizationsAsync(
                (decimal)CurrentLocation.Latitude,
                (decimal)CurrentLocation.Longitude,
                SearchRadius);

            Organizations.Clear();
            foreach (var org in organizations)
            {
                Organizations.Add(org);
            }
        }

        private async Task LoadBusinessesAsync()
        {
            if (CurrentLocation == null) return;

            var businesses = await _businessService.GetNearbyBusinessesAsync(
                (decimal)CurrentLocation.Latitude,
                (decimal)CurrentLocation.Longitude,
                SearchRadius);

            Businesses.Clear();
            foreach (var business in businesses)
            {
                Businesses.Add(business);
            }
        }

        // Commandes de navigation adaptées
        [RelayCommand]
        public async Task ShowPracticeSpotDetailsAsync(PracticeSpot spot)
        {
            if (spot != null)
            {
                await NavigationService.NavigateToAsync("PracticeSpotDetailsPage", spot.Id);
            }
        }

        [RelayCommand]
        public async Task ShowOrganizationDetailsAsync(Organization organization)
        {
            if (organization != null)
            {
                await NavigationService.NavigateToAsync("OrganizationDetailsPage", organization.Id);
            }
        }

        [RelayCommand]
        public async Task ShowBusinessDetailsAsync(Business business)
        {
            if (business != null)
            {
                await NavigationService.NavigateToAsync("BusinessDetailsPage", business.Id);
            }
        }

        // Autres méthodes...
        [RelayCommand]
        public async Task GetCurrentLocationAsync()
        {
            try
            {
                IsLocationLoading = true;
                CurrentLocation = await _locationService.GetCurrentLocationAsync();
                
                if (CurrentLocation != null)
                {
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible d'obtenir votre localisation.");
            }
            finally
            {
                IsLocationLoading = false;
            }
        }
    }
}
```

## ⚙️ **SERVICES REGISTRATION MIS À JOUR**

### **MauiProgram.cs (Mise à jour)**
```csharp
using SubExplore.Services.Extensions;

namespace SubExplore;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Configuration des services avec NOUVEAUX repositories et services
        var connectionString = GetSupabaseConnectionString();
        builder.Services.AddSubExploreServices(connectionString);

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static string GetSupabaseConnectionString()
    {
        // IMPORTANT : Utiliser votre chaîne de connexion Supabase PostgreSQL
        return "Host=db.iguvwnyehojvxkyqzaoi.supabase.co;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;Port=5432;SSL Mode=Require;Trust Server Certificate=true;";
    }
}
```

### **Services/Extensions/ServiceExtensions.cs (Mise à jour)**
```csharp
using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Repositories.Interfaces;
using SubExplore.Repositories.Implementations;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;

namespace SubExplore.Services.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddSubExploreServices(this IServiceCollection services, string connectionString)
        {
            // Configuration PostgreSQL pour Supabase
            services.AddDbContext<SubExploreDbContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                })
                .EnableSensitiveDataLogging(false)
                .EnableServiceProviderCaching()
                .LogTo(Console.WriteLine, LogLevel.Warning));

            // Repositories génériques
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // NOUVEAUX Repositories spécifiques
            services.AddScoped<IPracticeSpotRepository, PracticeSpotRepository>();
            services.AddScoped<IOrganizationRepository, OrganizationRepository>();
            services.AddScoped<IBusinessRepository, BusinessRepository>();
            services.AddScoped<IUnifiedMediaRepository, UnifiedMediaRepository>();

            // Repositories existants (à conserver)
            services.AddScoped<ISpotTypeRepository, SpotTypeRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            // NOUVEAUX Services
            services.AddScoped<IPracticeSpotService, PracticeSpotService>();
            services.AddScoped<IOrganizationService, OrganizationService>();
            services.AddScoped<IBusinessService, BusinessService>();
            services.AddScoped<IUnifiedMediaService, UnifiedMediaService>();

            // Services existants (à conserver)
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<ICacheService, MemoryCacheService>();
            services.AddScoped<ISecureStorageService, SecureStorageService>();

            // Services utilitaires
            services.AddMemoryCache();
            services.AddLogging();

            return services;
        }
    }
}
```

## 🧪 **TESTS ET VÉRIFICATION**

### **Tests pour vérifier la migration**
1. **Test de connexion DB** : Vérifier que l'app se connecte à Supabase
2. **Test des fonctions PostGIS** : Vérifier que `GetNearbyPracticeSpotsAsync` fonctionne
3. **Test des 3 entités** : Vérifier que PracticeSpots, Organizations, Businesses sont récupérées
4. **Test MapViewModel** : Vérifier que la carte affiche les 3 types de points

### **Points de contrôle**
- [ ] Compilation sans erreur
- [ ] DbContext reconnaît les nouvelles tables
- [ ] Services d'injection fonctionnent
- [ ] MapViewModel charge les données
- [ ] Interface affiche les nouveaux types

## 🚨 **POINTS CRITIQUES À NE PAS OUBLIER**

### **1. Suppression des Anciennes Références**
- Supprimer toutes les références à `Spot.cs` (remplacé par `PracticeSpot.cs`)
- Supprimer `SpotMedia.cs` (remplacé par `UnifiedMedia.cs`)
- Mettre à jour tous les `using` qui pointent vers les anciens modèles

### **2. Conversion UUID/Guid**
- Tous les `int Id` d'utilisateurs → `Guid`
- Tous les `int SpotTypeId` → `Guid` 
- Attention aux FK dans les jointures

### **3. Navigation Properties**
- Vérifier que toutes les relations EF Core fonctionnent
- Tester les `Include()` avec les nouvelles entités

### **4. String de Connexion**
- Utiliser la chaîne PostgreSQL Supabase (pas MySQL)
- Configurer SSL Mode=Require pour Supabase

### **5. Preserve Existing Features**
- Toutes les fonctionnalités de carte doivent rester
- Filtres, recherches, navigation conservés
- UI/UX identique du point de vue utilisateur

## 🎯 **RÉSULTAT ATTENDU**

Après implémentation complète :
- ✅ **Architecture 3 tables** opérationnelle en C#
- ✅ **Fonctions PostGIS** utilisées pour la géolocalisation
- ✅ **Performance** optimisée (DB fait les calculs)
- ✅ **Toutes les fonctionnalités** préservées
- ✅ **16 points de test** visibles sur la carte
- ✅ **Code propre** et maintenable

**L'utilisateur doit voir ses 9 practice spots, 3 organizations et 4 businesses sur la carte du Finistère Sud, avec géolocalisation précise et filtres fonctionnels !** 🎉