using System;
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

        public override async Task<Spot?> GetByIdAsync(int id)
        {
            return await _context.Spots
                .Include(s => s.Type)
                .Include(s => s.Creator)
                .Include(s => s.Media)
                .FirstOrDefaultAsync(s => s.Id == id);
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
                .ToListAsync();
        }

        public async Task<IEnumerable<Spot>> GetSpotsByTypeAsync(int typeId)
        {
            return await _context.Spots
                .Include(s => s.Type)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.TypeId == typeId && s.ValidationStatus == SpotValidationStatus.Approved)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Spot>> GetSpotsByUserAsync(int userId)
        {
            return await _context.Spots
                .Include(s => s.Type)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.CreatorId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Spot>> GetSpotsByValidationStatusAsync(SpotValidationStatus status)
        {
            return await _context.Spots
                .Include(s => s.Type)
                .Include(s => s.Creator)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => s.ValidationStatus == status)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Spot>> SearchSpotsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetAllAsync();

            string normalizedQuery = query.ToLower();

            return await _context.Spots
                .Include(s => s.Type)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => (s.Name.ToLower().Contains(normalizedQuery) ||
                            s.Description.ToLower().Contains(normalizedQuery)) &&
                            s.ValidationStatus == SpotValidationStatus.Approved)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }
    }
}
