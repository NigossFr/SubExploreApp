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