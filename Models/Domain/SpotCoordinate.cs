using Microsoft.Maui.Maps;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SubExplore.Models.Domain
{
    /// <summary>
    /// Optimized coordinate representation with caching and efficient conversions
    /// Eliminates repetitive decimal↔double conversions and improves performance
    /// </summary>
    public readonly struct SpotCoordinate : IEquatable<SpotCoordinate>
    {
        #region Properties
        
        /// <summary>Database-native decimal latitude for precise storage</summary>
        public readonly decimal LatitudeDecimal;
        
        /// <summary>Database-native decimal longitude for precise storage</summary>
        public readonly decimal LongitudeDecimal;
        
        /// <summary>Cached double latitude for MAUI operations</summary>
        public readonly double LatitudeDouble;
        
        /// <summary>Cached double longitude for MAUI operations</summary>
        public readonly double LongitudeDouble;
        
        /// <summary>Cached MAUI Location for map operations</summary>
        [JsonIgnore]
        private readonly Location? _cachedLocation;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Create coordinate from database decimal values
        /// </summary>
        /// <param name="latitudeDecimal">Latitude as decimal (database precision)</param>
        /// <param name="longitudeDecimal">Longitude as decimal (database precision)</param>
        public SpotCoordinate(decimal latitudeDecimal, decimal longitudeDecimal)
        {
            LatitudeDecimal = latitudeDecimal;
            LongitudeDecimal = longitudeDecimal;
            LatitudeDouble = (double)latitudeDecimal;
            LongitudeDouble = (double)longitudeDecimal;
            _cachedLocation = null; // Lazy initialization
        }
        
        /// <summary>
        /// Create coordinate from MAUI double values
        /// </summary>
        /// <param name="latitudeDouble">Latitude as double (MAUI native)</param>
        /// <param name="longitudeDouble">Longitude as double (MAUI native)</param>
        public SpotCoordinate(double latitudeDouble, double longitudeDouble)
        {
            LatitudeDouble = latitudeDouble;
            LongitudeDouble = longitudeDouble;
            LatitudeDecimal = (decimal)latitudeDouble;
            LongitudeDecimal = (decimal)longitudeDouble;
            _cachedLocation = null; // Lazy initialization
        }
        
        /// <summary>
        /// Create coordinate from MAUI Location
        /// </summary>
        /// <param name="location">MAUI Location object</param>
        [JsonConstructor]
        public SpotCoordinate(Location location)
        {
            LatitudeDouble = location.Latitude;
            LongitudeDouble = location.Longitude;
            LatitudeDecimal = (decimal)location.Latitude;
            LongitudeDecimal = (decimal)location.Longitude;
            _cachedLocation = location;
        }
        
        #endregion
        
        #region Conversion Methods
        
        /// <summary>
        /// Get MAUI Location with caching for performance
        /// </summary>
        /// <returns>Cached or new Location instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Location ToLocation()
        {
            return _cachedLocation ?? new Location(LatitudeDouble, LongitudeDouble);
        }
        
        /// <summary>
        /// Get MapSpan centered on this coordinate
        /// </summary>
        /// <param name="radiusKm">Radius in kilometers</param>
        /// <returns>MapSpan for MAUI maps</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MapSpan ToMapSpan(double radiusKm = 1.0)
        {
            return MapSpan.FromCenterAndRadius(ToLocation(), Distance.FromKilometers(radiusKm));
        }
        
        #endregion
        
        #region Distance Calculations
        
        /// <summary>
        /// Calculate Haversine distance to another coordinate
        /// Optimized with inline calculations
        /// </summary>
        /// <param name="other">Target coordinate</param>
        /// <returns>Distance in kilometers</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double DistanceToKm(SpotCoordinate other)
        {
            return DistanceCalculator.HaversineDistance(
                LatitudeDouble, LongitudeDouble,
                other.LatitudeDouble, other.LongitudeDouble);
        }
        
        /// <summary>
        /// Check if coordinate is within specified radius
        /// </summary>
        /// <param name="center">Center coordinate</param>
        /// <param name="radiusKm">Radius in kilometers</param>
        /// <returns>True if within radius</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWithinRadius(SpotCoordinate center, double radiusKm)
        {
            return DistanceToKm(center) <= radiusKm;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate coordinate values are within valid ranges
        /// </summary>
        /// <returns>True if valid geographic coordinates</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid()
        {
            return LatitudeDouble >= -90.0 && LatitudeDouble <= 90.0 &&
                   LongitudeDouble >= -180.0 && LongitudeDouble <= 180.0;
        }
        
        #endregion
        
        #region Equality & Hashing
        
        public bool Equals(SpotCoordinate other)
        {
            return LatitudeDecimal == other.LatitudeDecimal && 
                   LongitudeDecimal == other.LongitudeDecimal;
        }
        
        public override bool Equals(object? obj)
        {
            return obj is SpotCoordinate other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(LatitudeDecimal, LongitudeDecimal);
        }
        
        public static bool operator ==(SpotCoordinate left, SpotCoordinate right)
        {
            return left.Equals(right);
        }
        
        public static bool operator !=(SpotCoordinate left, SpotCoordinate right)
        {
            return !left.Equals(right);
        }
        
        #endregion
        
        #region String Representation
        
        public override string ToString()
        {
            return $"({LatitudeDouble:F6}, {LongitudeDouble:F6})";
        }
        
        /// <summary>
        /// Get formatted coordinate string for display
        /// </summary>
        /// <param name="precision">Decimal places</param>
        /// <returns>Formatted coordinate string</returns>
        public string ToString(int precision)
        {
            var format = $"F{precision}";
            return $"({LatitudeDouble.ToString(format)}, {LongitudeDouble.ToString(format)})";
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>Default coordinate (Marseille, France)</summary>
        public static SpotCoordinate Default => new(43.2965m, 5.3698m);
        
        /// <summary>
        /// Create coordinate from Spot entity
        /// </summary>
        /// <param name="spot">Spot with latitude/longitude</param>
        /// <returns>Optimized coordinate</returns>
        public static SpotCoordinate FromSpot(Spot spot)
        {
            return new SpotCoordinate(spot.Latitude, spot.Longitude);
        }
        
        #endregion
    }
    
    /// <summary>
    /// High-performance distance calculation utilities
    /// </summary>
    public static class DistanceCalculator
    {
        private const double EarthRadiusKm = 6371.0;
        private const double DegToRad = Math.PI / 180.0;
        
        /// <summary>
        /// Optimized Haversine distance calculation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Quick check for identical coordinates
            if (Math.Abs(lat1 - lat2) < 0.000001 && Math.Abs(lon1 - lon2) < 0.000001)
                return 0.0;
            
            var dLat = (lat2 - lat1) * DegToRad;
            var dLon = (lon2 - lon1) * DegToRad;
            
            var lat1Rad = lat1 * DegToRad;
            var lat2Rad = lat2 * DegToRad;
            
            var sinDLat2 = Math.Sin(dLat * 0.5);
            var sinDLon2 = Math.Sin(dLon * 0.5);
            
            var a = sinDLat2 * sinDLat2 + 
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) * sinDLon2 * sinDLon2;
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return EarthRadiusKm * c;
        }
    }
}