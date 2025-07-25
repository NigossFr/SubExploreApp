﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;

namespace SubExplore.Repositories.Implementations
{
    public class SpotRepository : GenericRepository<Spot>, ISpotRepository
    {
        public SpotRepository(SubExploreDbContext context) : base(context)
        {
        }

        public override async Task<Spot?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Spots
                .Include(s => s.Type)
                .Include(s => s.Creator)
                .Include(s => s.Media)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Spot>> GetNearbySpots(decimal latitude, decimal longitude, double radiusInKm, int limit = 50)
        {
            // Approximation simple: 1 degré de latitude = environ 111 km
            // Cette méthode n'est pas très précise mais suffit pour un premier filtrage
            double latDelta = radiusInKm / 111.0;
            double lonDelta = radiusInKm / (111.0 * Math.Cos((double)latitude * (Math.PI / 180.0)));

            decimal minLat = latitude - (decimal)latDelta;
            decimal maxLat = latitude + (decimal)latDelta;
            decimal minLon = longitude - (decimal)lonDelta;
            decimal maxLon = longitude + (decimal)lonDelta;

            return await _context.Spots
                .AsNoTracking() // Performance: Disable change tracking for read-only queries
                .Include(s => s.Type)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.Latitude >= minLat &&
                           s.Latitude <= maxLat &&
                           s.Longitude >= minLon &&
                           s.Longitude <= maxLon &&
                           s.ValidationStatus == SpotValidationStatus.Approved)
                .OrderBy(s =>
                    Math.Sqrt(Math.Pow((double)(s.Latitude - latitude), 2) +
                             Math.Pow((double)(s.Longitude - longitude), 2)))
                .Take(limit)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<Spot>> GetSpotsByTypeAsync(int typeId)
        {
            return await _context.Spots
                .AsNoTracking() // Performance: Disable change tracking for read-only queries
                .Include(s => s.Type)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.TypeId == typeId && s.ValidationStatus == SpotValidationStatus.Approved)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<Spot>> GetSpotsByUserAsync(int userId)
        {
            return await _context.Spots
                .AsNoTracking() // Performance: Disable change tracking for read-only queries
                .Include(s => s.Type)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.CreatorId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<Spot>> GetSpotsByValidationStatusAsync(SpotValidationStatus status)
        {
            try
            {
                // Optimisation critique: projection spécifique pour réduire les données transférées
                return await _context.Spots
                    .AsNoTracking() // Performance: Disable change tracking for read-only queries
                    .Where(s => s.ValidationStatus == status) // Filtre d'abord pour réduire les données
                    .Select(s => new Spot
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        MaxDepth = s.MaxDepth,
                        DifficultyLevel = s.DifficultyLevel,
                        CreatedAt = s.CreatedAt,
                        ValidationStatus = s.ValidationStatus,
                        SafetyFlags = s.SafetyFlags,
                        CurrentStrength = s.CurrentStrength,
                        BestConditions = s.BestConditions,
                        RequiredEquipment = s.RequiredEquipment,
                        SafetyNotes = s.SafetyNotes,
                        TypeId = s.TypeId,
                        CreatorId = s.CreatorId,
                        // Navigation properties optimisées
                        Type = new SpotType 
                        {
                            Id = s.Type.Id,
                            Name = s.Type.Name,
                            ColorCode = s.Type.ColorCode,
                            Category = s.Type.Category
                        },
                        Creator = new User 
                        {
                            Id = s.Creator.Id,
                            FirstName = s.Creator.FirstName,
                            LastName = s.Creator.LastName,
                            Email = s.Creator.Email
                        },
                        // Seulement les médias primaires pour réduire les données
                        Media = s.Media.Where(m => m.IsPrimary).Select(m => new SpotMedia
                        {
                            Id = m.Id,
                            MediaUrl = m.MediaUrl,
                            IsPrimary = m.IsPrimary,
                            MediaType = m.MediaType,
                            SpotId = m.SpotId
                        }).ToList()
                    })
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Fallback vers la méthode originale si la projection échoue
                System.Diagnostics.Debug.WriteLine($"[WARNING] Optimized query failed, falling back to original: {ex.Message}");
                return await _context.Spots
                    .AsNoTracking()
                    .Include(s => s.Type)
                    .Include(s => s.Creator)
                    .Include(s => s.Media.Where(m => m.IsPrimary))
                    .Where(s => s.ValidationStatus == status)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<Spot>> SearchSpotsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetAllAsync();

            string normalizedQuery = query.ToLower();

            return await _context.Spots
                .AsNoTracking() // Performance: Disable change tracking for read-only queries
                .Include(s => s.Type)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => (EF.Functions.Like(s.Name.ToLower(), $"%{normalizedQuery}%") ||
                            EF.Functions.Like(s.Description.ToLower(), $"%{normalizedQuery}%")) &&
                            s.ValidationStatus == SpotValidationStatus.Approved)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync()
                .ConfigureAwait(false);
        }
    }
}
