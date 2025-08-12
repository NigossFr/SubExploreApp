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

        public override async Task<Spot?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Spots
                .AsNoTracking() // Performance: Disable change tracking for read-only queries
                .AsSplitQuery() // Performance: Use split queries for multiple includes
                .Include(s => s.Type)
                .Include(s => s.Creator)
                .Include(s => s.Media.OrderByDescending(m => m.CreatedAt).Take(10)) // Limit media to first 10 items
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
                .ConfigureAwait(false);
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
                .AsSplitQuery() // Performance: Use split queries for better performance
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

        /// <summary>
        /// High-performance method for getting spot list with minimal data loading
        /// Optimized for map display and listing scenarios
        /// Enhanced with better filtering and error handling
        /// </summary>
        public async Task<IEnumerable<Spot>> GetSpotsMinimalAsync(int limit = 100, CancellationToken cancellationToken = default)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] GetSpotsMinimalAsync called with limit: {limit}");
                
                var spots = await _context.Spots
                    .AsNoTracking() // Performance: No change tracking
                    .AsSplitQuery() // Performance: Optimize for complex queries
                    .Include(s => s.Type) // Ensure Type is loaded
                    .Where(s => s.ValidationStatus == SpotValidationStatus.Approved && 
                               s.Type != null && s.Type.IsActive == true) // Critical: Only active types
                    .Select(s => new Spot
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        MaxDepth = s.MaxDepth,
                        DifficultyLevel = s.DifficultyLevel,
                        ValidationStatus = s.ValidationStatus,
                        TypeId = s.TypeId,
                        CreatedAt = s.CreatedAt,
                        CreatorId = s.CreatorId,
                        Type = new SpotType 
                        { 
                            Id = s.Type.Id, 
                            Name = s.Type.Name, 
                            IconPath = s.Type.IconPath,
                            ColorCode = s.Type.ColorCode,
                            Category = s.Type.Category,
                            IsActive = s.Type.IsActive
                        }
                    }) // Performance: Project only needed fields
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                    
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] GetSpotsMinimalAsync returned {spots.Count()} spots");
                
                // Additional validation for debugging
                if (!spots.Any())
                {
                    var totalSpots = await _context.Spots.CountAsync(cancellationToken);
                    var approvedSpots = await _context.Spots.Where(s => s.ValidationStatus == SpotValidationStatus.Approved).CountAsync(cancellationToken);
                    var activeTypes = await _context.SpotTypes.Where(t => t.IsActive).CountAsync(cancellationToken);
                    
                    System.Diagnostics.Debug.WriteLine($"[SpotRepository] No spots returned. Total: {totalSpots}, Approved: {approvedSpots}, Active types: {activeTypes}");
                }
                
                return spots;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] Error in GetSpotsMinimalAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] Stack trace: {ex.StackTrace}");
                return new List<Spot>();
            }
        }

        /// <summary>
        /// Ultra-fast method for getting spots by multiple categories
        /// Optimized for hierarchical filtering
        /// </summary>
        public async Task<IEnumerable<Spot>> GetSpotsByMultipleCategoriesAsync(ActivityCategory[] categories)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] GetSpotsByMultipleCategoriesAsync called for {categories.Length} categories");
                
                var spots = await _context.Spots
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(s => s.Type)
                    .Where(s => categories.Contains(s.Type.Category) && 
                               s.Type.IsActive &&
                               s.ValidationStatus == SpotValidationStatus.Approved)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync()
                    .ConfigureAwait(false);
                    
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] Found {spots.Count()} spots across {categories.Length} categories");
                return spots;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] Error in GetSpotsByMultipleCategoriesAsync: {ex.Message}");
                return new List<Spot>();
            }
        }

        /// <summary>
        /// Cached method for getting spot counts by category
        /// Useful for UI badges and statistics
        /// </summary>
        public async Task<Dictionary<ActivityCategory, int>> GetSpotCountsByCategoryAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[SpotRepository] GetSpotCountsByCategoryAsync called");
                
                var counts = await _context.Spots
                    .AsNoTracking()
                    .Where(s => s.ValidationStatus == SpotValidationStatus.Approved && s.Type.IsActive)
                    .GroupBy(s => s.Type.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count)
                    .ConfigureAwait(false);
                    
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] Category counts: {string.Join(", ", counts.Select(c => $"{c.Key}={c.Value}"))}");
                return counts;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] Error in GetSpotCountsByCategoryAsync: {ex.Message}");
                return new Dictionary<ActivityCategory, int>();
            }
        }

        public async Task<IEnumerable<Spot>> GetSpotsByTypeAsync(int typeId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] GetSpotsByTypeAsync called for typeId: {typeId}");
                
                var spots = await _context.Spots
                    .AsNoTracking() // Performance: Disable change tracking for read-only queries
                    .Include(s => s.Type)
                    .Include(s => s.Media.Where(m => m.IsPrimary))
                    .Where(s => s.TypeId == typeId && s.ValidationStatus == SpotValidationStatus.Approved)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync()
                    .ConfigureAwait(false);
                    
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] Found {spots.Count()} spots for typeId {typeId}");
                return spots;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] Error in GetSpotsByTypeAsync: {ex.Message}");
                return new List<Spot>();
            }
        }

        public async Task<IEnumerable<Spot>> GetSpotsByCategoryAsync(ActivityCategory category)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] GetSpotsByCategoryAsync called for category: {category}");
                
                // First check if any SpotTypes exist for this category
                var typeExists = await _context.SpotTypes
                    .AsNoTracking()
                    .AnyAsync(t => t.Category == category && t.IsActive)
                    .ConfigureAwait(false);
                    
                if (!typeExists)
                {
                    System.Diagnostics.Debug.WriteLine($"[SpotRepository] No active SpotTypes found for category {category}");
                    
                    // Log all active types for debugging
                    var allActiveTypes = await _context.SpotTypes
                        .AsNoTracking()
                        .Where(t => t.IsActive)
                        .Select(t => new { t.Name, t.Category, t.IsActive })
                        .ToListAsync();
                        
                    System.Diagnostics.Debug.WriteLine($"[SpotRepository] Active types: {string.Join(", ", allActiveTypes.Select(t => $"{t.Name}({t.Category})"))}");
                }
                
                var spots = await _context.Spots
                    .AsNoTracking()
                    .Include(s => s.Type)
                    .Include(s => s.Media.Where(m => m.IsPrimary))
                    .Where(s => s.Type != null &&
                               s.Type.Category == category && 
                               s.Type.IsActive == true &&
                               s.ValidationStatus == SpotValidationStatus.Approved)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync()
                    .ConfigureAwait(false);
                    
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] Found {spots.Count()} spots for category {category}");
                return spots;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] Error in GetSpotsByCategoryAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] Stack trace: {ex.StackTrace}");
                return new List<Spot>();
            }
        }

        public async Task<IEnumerable<Spot>> GetSpotsByUserAsync(int userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] GetSpotsByUserAsync called for userId: {userId}");
                
                var spots = await _context.Spots
                    .AsNoTracking() // Performance: Disable change tracking for read-only queries
                    .Include(s => s.Type)
                    .Include(s => s.Media.Where(m => m.IsPrimary))
                    .Where(s => s.CreatorId == userId && 
                               s.Type != null && s.Type.IsActive) // Add null-safe filter for active types
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync()
                    .ConfigureAwait(false);
                    
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] Found {spots.Count()} spots for userId {userId}");
                
                // Log any potential spots that were filtered out due to missing/inactive types
                var allUserSpots = await _context.Spots
                    .AsNoTracking()
                    .Where(s => s.CreatorId == userId)
                    .CountAsync()
                    .ConfigureAwait(false);
                
                if (allUserSpots > spots.Count())
                {
                    var filteredCount = allUserSpots - spots.Count();
                    System.Diagnostics.Debug.WriteLine($"[SpotRepository] WARNING: {filteredCount} spots filtered out due to missing/inactive types for userId {userId}");
                }
                
                return spots;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotRepository] Error in GetSpotsByUserAsync: {ex.Message}");
                return new List<Spot>();
            }
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
                            EF.Functions.Like(s.Description.ToLower(), $"%{normalizedQuery}%") ||
                            EF.Functions.Like(s.Type.Name.ToLower(), $"%{normalizedQuery}%")) &&
                            s.ValidationStatus == SpotValidationStatus.Approved)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<Spot>> SearchSpotsWithLocationAsync(string query, decimal? userLatitude = null, decimal? userLongitude = null, double radiusKm = 50)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                if (userLatitude.HasValue && userLongitude.HasValue)
                {
                    return await GetNearbySpots(userLatitude.Value, userLongitude.Value, radiusKm);
                }
                return await GetAllAsync();
            }

            string normalizedQuery = query.ToLower();
            var baseQuery = _context.Spots
                .AsNoTracking()
                .Include(s => s.Type)
                .Include(s => s.Media.Where(m => m.IsPrimary))
                .Where(s => (EF.Functions.Like(s.Name.ToLower(), $"%{normalizedQuery}%") ||
                            EF.Functions.Like(s.Description.ToLower(), $"%{normalizedQuery}%") ||
                            EF.Functions.Like(s.Type.Name.ToLower(), $"%{normalizedQuery}%")) &&
                            s.ValidationStatus == SpotValidationStatus.Approved);

            if (userLatitude.HasValue && userLongitude.HasValue)
            {
                // Calculate distance and sort by relevance + proximity
                var results = await baseQuery.ToListAsync().ConfigureAwait(false);
                
                return results
                    .Select(s => new
                    {
                        Spot = s,
                        Distance = CalculateDistance((double)userLatitude.Value, (double)userLongitude.Value, 
                                                   (double)s.Latitude, (double)s.Longitude)
                    })
                    .Where(x => x.Distance <= radiusKm)
                    .OrderBy(x => x.Distance)
                    .Select(x => x.Spot);
            }

            return await baseQuery
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
                return Enumerable.Empty<string>();

            string normalizedQuery = query.ToLower();
            var suggestions = new List<string>();

            // Get spot name suggestions
            var spotNames = await _context.Spots
                .AsNoTracking()
                .Where(s => EF.Functions.Like(s.Name.ToLower(), $"%{normalizedQuery}%") &&
                           s.ValidationStatus == SpotValidationStatus.Approved)
                .Select(s => s.Name)
                .Distinct()
                .Take(3)
                .ToListAsync()
                .ConfigureAwait(false);
            
            suggestions.AddRange(spotNames);

            // Get spot type suggestions
            var typeNames = await _context.SpotTypes
                .AsNoTracking()
                .Where(t => EF.Functions.Like(t.Name.ToLower(), $"%{normalizedQuery}%"))
                .Select(t => t.Name)
                .Distinct()
                .Take(2)
                .ToListAsync()
                .ConfigureAwait(false);
                
            suggestions.AddRange(typeNames);

            return suggestions.Take(limit);
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double degrees) => degrees * (Math.PI / 180);
    }
}
