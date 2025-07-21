using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using System.Text.Json;

namespace SubExplore.DataAccess
{
    public class SubExploreDbContext : DbContext
    {
        public SubExploreDbContext(DbContextOptions<SubExploreDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public DbSet<Spot> Spots { get; set; }
        public DbSet<SpotMedia> SpotMedia { get; set; }
        public DbSet<SpotType> SpotTypes { get; set; }
        public DbSet<RevokedToken> RevokedTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration des relations
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();

                entity.HasOne(e => e.Preferences)
                      .WithOne(e => e.User)
                      .HasForeignKey<UserPreferences>(e => e.UserId);

                entity.HasMany(e => e.CreatedSpots)
                      .WithOne(e => e.Creator)
                      .HasForeignKey(e => e.CreatorId);
            });

            modelBuilder.Entity<Spot>(entity =>
            {
                entity.HasIndex(e => new { e.Latitude, e.Longitude });
                entity.HasIndex(e => e.TypeId);
                entity.HasIndex(e => e.ValidationStatus);

                entity.HasMany(e => e.Media)
                      .WithOne(e => e.Spot)
                      .HasForeignKey(e => e.SpotId);
            });

            modelBuilder.Entity<RevokedToken>(entity =>
            {
                entity.HasIndex(e => e.TokenHash).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.RevokedAt);
                entity.HasIndex(e => e.ExpiresAt);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Seed data configuration
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Create admin user
            var adminUser = new User
            {
                Id = 1,
                Email = "admin@subexplore.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Username = "admin",
                FirstName = "Admin",
                LastName = "System",
                AccountType = AccountType.Administrator,
                SubscriptionStatus = SubscriptionStatus.Premium,
                ExpertiseLevel = ExpertiseLevel.Professional,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            modelBuilder.Entity<User>().HasData(adminUser);

            // Create user preferences
            var adminPreferences = new UserPreferences
            {
                Id = 1,
                UserId = 1,
                Theme = "dark",
                DisplayNamePreference = "username",
                NotificationSettings = JsonSerializer.Serialize(new
                {
                    SpotValidations = true,
                    NewSpots = true,
                    Comments = true
                }),
                Language = "fr",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            modelBuilder.Entity<UserPreferences>().HasData(adminPreferences);

            // Create spot types
            var spotTypes = new[]
            {
                new SpotType
                {
                    Id = 1,
                    Name = "Apnée",
                    IconPath = "marker_freediving.png",
                    ColorCode = "#00B4D8",
                    Category = ActivityCategory.Freediving,
                    Description = "Sites adaptés à la plongée en apnée",
                    RequiresExpertValidation = true,
                    ValidationCriteria = JsonSerializer.Serialize(new { RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" } }),
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new SpotType
                {
                    Id = 2,
                    Name = "Photo sous-marine",
                    IconPath = "marker_photography.png",
                    ColorCode = "#2EC4B6",
                    Category = ActivityCategory.UnderwaterPhotography,
                    Description = "Sites d'intérêt pour la photographie sous-marine",
                    RequiresExpertValidation = false,
                    ValidationCriteria = JsonSerializer.Serialize(new { RequiredFields = new[] { "DifficultyLevel" } }),
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new SpotType
                {
                    Id = 3,
                    Name = "Plongée récréative",
                    IconPath = "marker_diving.png",
                    ColorCode = "#006994",
                    Category = ActivityCategory.Diving,
                    Description = "Sites adaptés à la plongée avec bouteille",
                    RequiresExpertValidation = true,
                    ValidationCriteria = JsonSerializer.Serialize(new { RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" } }),
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new SpotType
                {
                    Id = 4,
                    Name = "Plongée technique",
                    IconPath = "marker_technical.png",
                    ColorCode = "#FF9F1C",
                    Category = ActivityCategory.Diving,
                    Description = "Sites pour plongée technique (profondeur, épaves...)",
                    RequiresExpertValidation = true,
                    ValidationCriteria = JsonSerializer.Serialize(new { RequiredFields = new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes", "RequiredEquipment" } }),
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new SpotType
                {
                    Id = 5,
                    Name = "Randonnée sous marine",
                    IconPath = "marker_snorkeling.png",
                    ColorCode = "#48CAE4",
                    Category = ActivityCategory.Snorkeling,
                    Description = "Sites de surface accessibles pour la randonnée sous-marine",
                    RequiresExpertValidation = false,
                    ValidationCriteria = JsonSerializer.Serialize(new { RequiredFields = new[] { "DifficultyLevel", "SafetyNotes" } }),
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            };

            modelBuilder.Entity<SpotType>().HasData(spotTypes);

            // Create spots
            var spots = new[]
            {
                new Spot
                {
                    Id = 1,
                    Name = "Calanque de Sormiou",
                    Description = "Magnifique calanque marseillaise avec une eau cristalline et une biodiversité riche. Site emblématique de la côte méditerranéenne.",
                    Latitude = 43.2148m,
                    Longitude = 5.4203m,
                    MaxDepth = 25,
                    DifficultyLevel = DifficultyLevel.Intermediate,
                    ValidationStatus = SpotValidationStatus.Approved,
                    TypeId = 3, // Plongée récréative
                    CreatorId = 1,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CurrentStrength = CurrentStrength.Moderate,
                    BestConditions = "Mer calme, visibilité 15-20m, éviter les vents du mistral",
                    SafetyNotes = "Attention aux bateaux de plaisance en été. Zone de baignade surveillée.",
                    RequiredEquipment = "Palmes, masque, tuba, combinaison 5mm recommandée"
                },
                new Spot
                {
                    Id = 2,
                    Name = "Île Maïre",
                    Description = "Site de plongée technique avec tombant spectaculaire et grottes sous-marines. Biodiversité exceptionnelle.",
                    Latitude = 43.2105m,
                    Longitude = 5.3520m,
                    MaxDepth = 40,
                    DifficultyLevel = DifficultyLevel.Advanced,
                    ValidationStatus = SpotValidationStatus.Approved,
                    TypeId = 4, // Plongée technique
                    CreatorId = 1,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CurrentStrength = CurrentStrength.Strong,
                    BestConditions = "Mer peu agitée, visibilité 20-25m, pas de vent d'est",
                    SafetyNotes = "Plongée technique - Niveau 2 minimum requis. Courants forts possibles.",
                    RequiredEquipment = "Équipement complet de plongée, lampe obligatoire, parachute de palier"
                },
                new Spot
                {
                    Id = 3,
                    Name = "Baie de Cassis",
                    Description = "Site parfait pour l'apnée avec profondeur progressive et faune accessible. Idéal pour débutants.",
                    Latitude = 43.2148m,
                    Longitude = 5.5385m,
                    MaxDepth = 15,
                    DifficultyLevel = DifficultyLevel.Beginner,
                    ValidationStatus = SpotValidationStatus.Approved,
                    TypeId = 1, // Apnée
                    CreatorId = 1,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CurrentStrength = CurrentStrength.Light,
                    BestConditions = "Mer calme, visibilité 10-15m, température eau >18°C",
                    SafetyNotes = "Idéal pour débutants. Surveiller les autres usagers de la mer.",
                    RequiredEquipment = "Palmes, masque, tuba, combinaison selon saison"
                },
                new Spot
                {
                    Id = 4,
                    Name = "Port-Cros",
                    Description = "Réserve naturelle marine avec une biodiversité exceptionnelle. Parfait pour la photographie sous-marine.",
                    Latitude = 43.0092m,
                    Longitude = 6.3914m,
                    MaxDepth = 12,
                    DifficultyLevel = DifficultyLevel.Beginner,
                    ValidationStatus = SpotValidationStatus.Approved,
                    TypeId = 2, // Photo sous-marine
                    CreatorId = 1,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CurrentStrength = CurrentStrength.Light,
                    BestConditions = "Eau calme, excellente visibilité, lumière naturelle optimale",
                    SafetyNotes = "Zone protégée - respecter la faune et la flore. Pêche interdite.",
                    RequiredEquipment = "Appareil photo étanche, palmes, masque, tuba"
                },
                new Spot
                {
                    Id = 5,
                    Name = "Sentier Sous-Marin de Banyuls",
                    Description = "Parcours balisé idéal pour la randonnée sous-marine familiale. Découverte de la faune méditerranéenne.",
                    Latitude = 42.4875m,
                    Longitude = 3.1286m,
                    MaxDepth = 5,
                    DifficultyLevel = DifficultyLevel.Beginner,
                    ValidationStatus = SpotValidationStatus.Approved,
                    TypeId = 5, // Randonnée sous marine
                    CreatorId = 1,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CurrentStrength = CurrentStrength.None,
                    BestConditions = "Mer calme, visibilité correcte, éviter les jours de tramontane",
                    SafetyNotes = "Parcours familial. Respecter les bouées de balisage.",
                    RequiredEquipment = "Palmes, masque, tuba, lycra anti-UV recommandé"
                }
            };

            modelBuilder.Entity<Spot>().HasData(spots);
        }
    }
}
