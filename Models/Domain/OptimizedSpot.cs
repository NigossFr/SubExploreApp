using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using SubExplore.Models.Enums;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SubExplore.Models.Domain
{
    /// <summary>
    /// Optimized spot representation with embedded coordinate management
    /// Eliminates conversion overhead and provides direct map integration
    /// </summary>
    public readonly struct OptimizedSpot : IEquatable<OptimizedSpot>
    {
        #region Core Properties
        
        public readonly int Id;
        public readonly string Name;
        public readonly string Description;
        public readonly SpotCoordinate Coordinate;
        public readonly DifficultyLevel DifficultyLevel;
        public readonly SpotValidationStatus ValidationStatus;
        public readonly DateTime CreatedAt;
        
        #endregion
        
        #region Optional Properties
        
        public readonly int? MaxDepth;
        public readonly CurrentStrength? CurrentStrength;
        public readonly bool HasMooring;
        public readonly string? RequiredEquipment;
        public readonly string? SafetyNotes;
        
        #endregion
        
        #region Constructor
        
        [JsonConstructor]
        public OptimizedSpot(
            int id, 
            string name, 
            string description,
            SpotCoordinate coordinate,
            DifficultyLevel difficultyLevel = DifficultyLevel.Beginner,
            SpotValidationStatus validationStatus = SpotValidationStatus.Pending,
            DateTime? createdAt = null,
            int? maxDepth = null,
            CurrentStrength? currentStrength = null,
            bool hasMooring = false,
            string? requiredEquipment = null,
            string? safetyNotes = null)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            Coordinate = coordinate;
            DifficultyLevel = difficultyLevel;
            ValidationStatus = validationStatus;
            CreatedAt = createdAt ?? DateTime.UtcNow;
            MaxDepth = maxDepth;
            CurrentStrength = currentStrength;
            HasMooring = hasMooring;
            RequiredEquipment = requiredEquipment;
            SafetyNotes = safetyNotes;
        }
        
        #endregion
        
        #region Map Integration
        
        /// <summary>
        /// Create MAUI Pin for map display with optimized binding
        /// </summary>
        /// <returns>Configured Pin for map display</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Pin CreateMapPin()
        {
            return new Pin
            {
                Label = Name,
                Address = Description.Length > 50 ? $"{Description[..47]}..." : Description,
                Type = PinType.Place,
                Location = Coordinate.ToLocation(),
                BindingContext = this // Direct struct binding - no reference overhead
            };
        }
        
        /// <summary>
        /// Get distance to another spot
        /// </summary>
        /// <param name="other">Target spot</param>
        /// <returns>Distance in kilometers</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double DistanceToKm(OptimizedSpot other)
        {
            return Coordinate.DistanceToKm(other.Coordinate);
        }
        
        /// <summary>
        /// Check if spot is within radius of location
        /// </summary>
        /// <param name="center">Center coordinate</param>
        /// <param name="radiusKm">Search radius in kilometers</param>
        /// <returns>True if within radius</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWithinRadius(SpotCoordinate center, double radiusKm)
        {
            return Coordinate.IsWithinRadius(center, radiusKm);
        }
        
        #endregion
        
        #region Conversion Methods
        
        /// <summary>
        /// Convert from Entity Framework Spot entity
        /// </summary>
        /// <param name="spot">EF Spot entity</param>
        /// <returns>Optimized spot structure</returns>
        public static OptimizedSpot FromEntity(Spot spot)
        {
            return new OptimizedSpot(
                id: spot.Id,
                name: spot.Name,
                description: spot.Description,
                coordinate: new SpotCoordinate(spot.Latitude, spot.Longitude),
                difficultyLevel: spot.DifficultyLevel,
                validationStatus: spot.ValidationStatus,
                createdAt: spot.CreatedAt,
                maxDepth: spot.MaxDepth,
                currentStrength: spot.CurrentStrength,
                hasMooring: spot.HasMooring ?? false,
                requiredEquipment: spot.RequiredEquipment,
                safetyNotes: spot.SafetyNotes
            );
        }
        
        /// <summary>
        /// Convert to Entity Framework Spot entity
        /// </summary>
        /// <returns>EF-compatible Spot entity</returns>
        public Spot ToEntity()
        {
            return new Spot
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Latitude = Coordinate.LatitudeDecimal,
                Longitude = Coordinate.LongitudeDecimal,
                DifficultyLevel = DifficultyLevel,
                ValidationStatus = ValidationStatus,
                CreatedAt = CreatedAt,
                MaxDepth = MaxDepth,
                CurrentStrength = CurrentStrength,
                HasMooring = HasMooring,
                RequiredEquipment = RequiredEquipment ?? string.Empty,
                SafetyNotes = SafetyNotes ?? string.Empty
            };
        }
        
        #endregion
        
        #region Filtering & Scoring
        
        /// <summary>
        /// Calculate selection score for pin selection algorithms
        /// </summary>
        /// <param name="clickLocation">User click location</param>
        /// <param name="maxDistance">Maximum search distance</param>
        /// <returns>Selection score (higher = better)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CalculateSelectionScore(SpotCoordinate clickLocation, double maxDistance)
        {
            var distance = Coordinate.DistanceToKm(clickLocation);
            if (distance > maxDistance) return 0.0;
            
            // Base distance score with exponential decay
            var distanceScore = Math.Exp(-distance / maxDistance * 2.0);
            
            // Validation status modifier
            var validationModifier = ValidationStatus switch
            {
                SpotValidationStatus.Approved => 1.2,
                SpotValidationStatus.Pending => 1.0,
                SpotValidationStatus.Rejected => 0.7,
                _ => 1.0
            };
            
            // Difficulty accessibility modifier (beginners more accessible)
            var difficultyModifier = DifficultyLevel switch
            {
                DifficultyLevel.Beginner => 1.1,
                DifficultyLevel.Intermediate => 1.0,
                DifficultyLevel.Advanced => 0.9,
                DifficultyLevel.Expert => 0.8,
                _ => 1.0
            };
            
            return distanceScore * validationModifier * difficultyModifier;
        }
        
        /// <summary>
        /// Check if spot matches search criteria
        /// </summary>
        /// <param name="searchText">Text to search in name/description</param>
        /// <param name="difficultyFilter">Optional difficulty filter</param>
        /// <param name="validationFilter">Optional validation status filter</param>
        /// <returns>True if spot matches criteria</returns>
        public bool MatchesSearch(
            string? searchText = null, 
            DifficultyLevel? difficultyFilter = null,
            SpotValidationStatus? validationFilter = null)
        {
            // Text search
            if (!string.IsNullOrEmpty(searchText))
            {
                var search = searchText.ToLowerInvariant();
                if (!Name.ToLowerInvariant().Contains(search) && 
                    !Description.ToLowerInvariant().Contains(search))
                {
                    return false;
                }
            }
            
            // Difficulty filter
            if (difficultyFilter.HasValue && DifficultyLevel != difficultyFilter.Value)
                return false;
            
            // Validation filter
            if (validationFilter.HasValue && ValidationStatus != validationFilter.Value)
                return false;
            
            return true;
        }
        
        #endregion
        
        #region Equality & Hashing
        
        public bool Equals(OptimizedSpot other)
        {
            return Id == other.Id; // ID-based equality for spots
        }
        
        public override bool Equals(object? obj)
        {
            return obj is OptimizedSpot other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return Id.GetHashCode(); // ID-based hashing
        }
        
        public static bool operator ==(OptimizedSpot left, OptimizedSpot right)
        {
            return left.Equals(right);
        }
        
        public static bool operator !=(OptimizedSpot left, OptimizedSpot right)
        {
            return !left.Equals(right);
        }
        
        #endregion
        
        #region String Representation
        
        public override string ToString()
        {
            return $"{Name} ({Coordinate}) - {ValidationStatus}";
        }
        
        /// <summary>
        /// Get detailed string representation
        /// </summary>
        /// <returns>Detailed spot information</returns>
        public string ToDetailedString()
        {
            var depth = MaxDepth.HasValue ? $", {MaxDepth}m" : "";
            var mooring = HasMooring ? ", Mooring" : "";
            return $"{Name} ({Coordinate}) - {DifficultyLevel}{depth}{mooring} - {ValidationStatus}";
        }
        
        #endregion
        
        #region Serialization Support
        
        /// <summary>
        /// Get serializable representation for caching/persistence
        /// </summary>
        /// <returns>Dictionary with serializable data</returns>
        public Dictionary<string, object?> ToSerializableData()
        {
            return new Dictionary<string, object?>
            {
                ["Id"] = Id,
                ["Name"] = Name,
                ["Description"] = Description,
                ["LatitudeDecimal"] = Coordinate.LatitudeDecimal,
                ["LongitudeDecimal"] = Coordinate.LongitudeDecimal,
                ["DifficultyLevel"] = (int)DifficultyLevel,
                ["ValidationStatus"] = (int)ValidationStatus,
                ["CreatedAt"] = CreatedAt,
                ["MaxDepth"] = MaxDepth,
                ["CurrentStrength"] = CurrentStrength.HasValue ? (int)CurrentStrength.Value : null,
                ["HasMooring"] = HasMooring,
                ["RequiredEquipment"] = RequiredEquipment,
                ["SafetyNotes"] = SafetyNotes
            };
        }
        
        /// <summary>
        /// Create OptimizedSpot from serializable data
        /// </summary>
        /// <param name="data">Serialized data dictionary</param>
        /// <returns>Reconstructed OptimizedSpot</returns>
        public static OptimizedSpot FromSerializableData(Dictionary<string, object?> data)
        {
            return new OptimizedSpot(
                id: Convert.ToInt32(data["Id"]),
                name: data["Name"]?.ToString() ?? string.Empty,
                description: data["Description"]?.ToString() ?? string.Empty,
                coordinate: new SpotCoordinate(
                    Convert.ToDecimal(data["LatitudeDecimal"]),
                    Convert.ToDecimal(data["LongitudeDecimal"])
                ),
                difficultyLevel: (DifficultyLevel)Convert.ToInt32(data["DifficultyLevel"]),
                validationStatus: (SpotValidationStatus)Convert.ToInt32(data["ValidationStatus"]),
                createdAt: data["CreatedAt"] as DateTime?,
                maxDepth: data["MaxDepth"] as int?,
                currentStrength: data["CurrentStrength"] != null ? 
                    (CurrentStrength)Convert.ToInt32(data["CurrentStrength"]) : null,
                hasMooring: Convert.ToBoolean(data["HasMooring"]),
                requiredEquipment: data["RequiredEquipment"]?.ToString(),
                safetyNotes: data["SafetyNotes"]?.ToString()
            );
        }
        
        #endregion
    }
    
    /// <summary>
    /// Extension methods for collections of OptimizedSpot
    /// </summary>
    public static class OptimizedSpotExtensions
    {
        /// <summary>
        /// Find spots within radius of a location
        /// </summary>
        /// <param name="spots">Collection of spots</param>
        /// <param name="center">Center coordinate</param>
        /// <param name="radiusKm">Search radius in kilometers</param>
        /// <returns>Spots within radius, ordered by distance</returns>
        public static IEnumerable<OptimizedSpot> WithinRadius(
            this IEnumerable<OptimizedSpot> spots, 
            SpotCoordinate center, 
            double radiusKm)
        {
            return spots
                .Where(spot => spot.IsWithinRadius(center, radiusKm))
                .OrderBy(spot => spot.Coordinate.DistanceToKm(center));
        }
        
        /// <summary>
        /// Convert collection to map pins
        /// </summary>
        /// <param name="spots">Collection of spots</param>
        /// <returns>Collection of map pins</returns>
        public static IEnumerable<Pin> ToMapPins(this IEnumerable<OptimizedSpot> spots)
        {
            return spots.Select(spot => spot.CreateMapPin());
        }
        
        /// <summary>
        /// Filter spots by search criteria
        /// </summary>
        /// <param name="spots">Collection of spots</param>
        /// <param name="searchText">Text search</param>
        /// <param name="difficultyFilter">Difficulty filter</param>
        /// <param name="validationFilter">Validation status filter</param>
        /// <returns>Filtered spots</returns>
        public static IEnumerable<OptimizedSpot> Search(
            this IEnumerable<OptimizedSpot> spots,
            string? searchText = null,
            DifficultyLevel? difficultyFilter = null,
            SpotValidationStatus? validationFilter = null)
        {
            return spots.Where(spot => spot.MatchesSearch(searchText, difficultyFilter, validationFilter));
        }
        
        /// <summary>
        /// Convert collection to serializable format for caching/persistence
        /// </summary>
        /// <param name="spots">Collection of spots</param>
        /// <returns>Serializable data array</returns>
        public static Dictionary<string, object?>[] ToSerializableData(this IEnumerable<OptimizedSpot> spots)
        {
            return spots.Select(spot => spot.ToSerializableData()).ToArray();
        }
        
        /// <summary>
        /// Create spots collection from serializable data
        /// </summary>
        /// <param name="data">Serialized data array</param>
        /// <returns>Collection of OptimizedSpot</returns>
        public static IEnumerable<OptimizedSpot> FromSerializableData(Dictionary<string, object?>[] data)
        {
            return data.Select(OptimizedSpot.FromSerializableData);
        }
    }
}