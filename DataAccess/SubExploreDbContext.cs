using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public DbSet<User> Users { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public DbSet<Spot> Spots { get; set; }
        public DbSet<SpotMedia> SpotMedia { get; set; }
        public DbSet<SpotType> SpotTypes { get; set; }

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

            // Seed data pour les types de spots
            modelBuilder.Entity<SpotType>().HasData(
                new SpotType { Id = 1, Name = "Plongée bouteille", Description = "Plongée avec équipement complet", ColorCode = "#1E88E5", IsActive = true, Category = Models.Enums.ActivityCategory.Diving },
                new SpotType { Id = 2, Name = "Apnée", Description = "Plongée en apnée", ColorCode = "#43A047", IsActive = true, Category = Models.Enums.ActivityCategory.Freediving },
                new SpotType { Id = 3, Name = "Snorkeling", Description = "Observation en surface", ColorCode = "#FF7043", IsActive = true, Category = Models.Enums.ActivityCategory.Snorkeling },
                new SpotType { Id = 4, Name = "Plongée technique", Description = "Plongée technique avancée", ColorCode = "#8E24AA", IsActive = true, Category = Models.Enums.ActivityCategory.Diving },
                new SpotType { Id = 5, Name = "Plongée de nuit", Description = "Plongée nocturne", ColorCode = "#3949AB", IsActive = true, Category = Models.Enums.ActivityCategory.Diving }
            );
        }
    }
}
