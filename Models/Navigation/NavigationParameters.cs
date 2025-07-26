using System;

namespace SubExplore.Models.Navigation
{
    /// <summary>
    /// Base interface for all navigation parameters
    /// </summary>
    public interface INavigationParameter
    {
    }

    /// <summary>
    /// Location parameter for navigation with GPS coordinates
    /// </summary>
    public class LocationNavigationParameter : INavigationParameter
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Description { get; set; }
        public bool IsFromUserLocation { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public LocationNavigationParameter()
        {
        }

        public LocationNavigationParameter(decimal latitude, decimal longitude, string description = null, bool isFromUserLocation = false)
        {
            Latitude = latitude;
            Longitude = longitude;
            Description = description;
            IsFromUserLocation = isFromUserLocation;
        }

        public override string ToString()
        {
            return $"LocationNavigationParameter(Lat: {Latitude}, Lng: {Longitude}, UserLocation: {IsFromUserLocation})";
        }
    }

    /// <summary>
    /// Spot parameter for navigation with spot information
    /// </summary>
    public class SpotNavigationParameter : INavigationParameter
    {
        public int SpotId { get; set; }
        public string SpotName { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public SpotNavigationParameter()
        {
        }

        public SpotNavigationParameter(int spotId, string spotName = null, decimal? latitude = null, decimal? longitude = null)
        {
            SpotId = spotId;
            SpotName = spotName;
            Latitude = latitude;
            Longitude = longitude;
        }

        public override string ToString()
        {
            return $"SpotNavigationParameter(Id: {SpotId}, Name: {SpotName})";
        }
    }

    /// <summary>
    /// User parameter for navigation with user information
    /// </summary>
    public class UserNavigationParameter : INavigationParameter
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }

        public UserNavigationParameter()
        {
        }

        public UserNavigationParameter(int userId, string userName = null, string email = null)
        {
            UserId = userId;
            UserName = userName;
            Email = email;
        }

        public override string ToString()
        {
            return $"UserNavigationParameter(Id: {UserId}, Name: {UserName})";
        }
    }

    /// <summary>
    /// Generic parameter for navigation with key-value pairs
    /// </summary>
    public class GenericNavigationParameter : INavigationParameter
    {
        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        public GenericNavigationParameter()
        {
        }

        public GenericNavigationParameter Add(string key, object value)
        {
            Parameters[key] = value;
            return this;
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (Parameters.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public bool HasValue(string key)
        {
            return Parameters.ContainsKey(key);
        }

        public override string ToString()
        {
            return $"GenericNavigationParameter({Parameters.Count} parameters)";
        }
    }

    /// <summary>
    /// Extension methods for navigation parameters
    /// </summary>
    public static class NavigationParameterExtensions
    {
        /// <summary>
        /// Check if type is anonymous
        /// </summary>
        public static bool IsAnonymousType(this Type type)
        {
            return type.Name.Contains("AnonymousType") && 
                   type.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Length > 0;
        }
        /// <summary>
        /// Safe cast to specific navigation parameter type
        /// </summary>
        public static T AsParameter<T>(this object parameter) where T : class, INavigationParameter
        {
            return parameter as T;
        }

        /// <summary>
        /// Check if parameter is of specific type
        /// </summary>
        public static bool IsParameterType<T>(this object parameter) where T : class, INavigationParameter
        {
            return parameter is T;
        }

        /// <summary>
        /// Extract location from any navigation parameter that contains location data
        /// </summary>
        public static (decimal? Latitude, decimal? Longitude) ExtractLocation(this object parameter)
        {
            return parameter switch
            {
                LocationNavigationParameter locationParam => (locationParam.Latitude, locationParam.Longitude),
                SpotNavigationParameter spotParam => (spotParam.Latitude, spotParam.Longitude),
                _ => (null, null)
            };
        }
    }
}