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

            // Configuration simple sans seed data
            // Les données seront gérées par DatabaseService.SeedDatabaseAsync()
        }
    }
}
