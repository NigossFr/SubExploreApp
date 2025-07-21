namespace SubExplore.Constants
{
    /// <summary>
    /// Application-wide constants for configuration and business rules
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// Map and location related constants
        /// </summary>
        public static class Map
        {
            /// <summary>
            /// Default search radius in kilometers for finding nearby spots
            /// </summary>
            public const double DEFAULT_SEARCH_RADIUS_KM = 10.0;

            /// <summary>
            /// Maximum number of spots to load at once for performance
            /// </summary>
            public const int MAX_SPOTS_LIMIT = 100;

            /// <summary>
            /// Minimum number of spots required for automatic zoom adjustment
            /// </summary>
            public const int MIN_SPOTS_FOR_AUTO_ZOOM = 1;

            /// <summary>
            /// Maximum number of spots for automatic zoom adjustment
            /// </summary>
            public const int MAX_SPOTS_FOR_AUTO_ZOOM = 5;

            /// <summary>
            /// Minimum allowed zoom level for map display
            /// </summary>
            public const double MIN_ZOOM_LEVEL = 1.0;

            /// <summary>
            /// Maximum allowed zoom level for map display
            /// </summary>
            public const double MAX_ZOOM_LEVEL = 18.0;

            /// <summary>
            /// Number of spots to process in each batch for performance optimization
            /// </summary>
            public const int SPOTS_BATCH_SIZE = 20;

            /// <summary>
            /// Delay in milliseconds between map updates to prevent excessive refreshing
            /// </summary>
            public const int MAP_UPDATE_DELAY_MS = 500;

            /// <summary>
            /// Cache expiry time in minutes for map data
            /// </summary>
            public const int CACHE_EXPIRY_MINUTES = 5;
        }

        /// <summary>
        /// Database and data access constants
        /// </summary>
        public static class Database
        {
            /// <summary>
            /// Default connection timeout in seconds
            /// </summary>
            public const int CONNECTION_TIMEOUT_SECONDS = 30;

            /// <summary>
            /// Command timeout for database operations in seconds
            /// </summary>
            public const int COMMAND_TIMEOUT_SECONDS = 120;

            /// <summary>
            /// Maximum retry attempts for database operations
            /// </summary>
            public const int MAX_RETRY_ATTEMPTS = 3;

            /// <summary>
            /// Delay between retry attempts in milliseconds
            /// </summary>
            public const int RETRY_DELAY_MS = 1000;
        }

        /// <summary>
        /// User interface and interaction constants
        /// </summary>
        public static class UI
        {
            /// <summary>
            /// Default toast message duration in seconds
            /// </summary>
            public const int DEFAULT_TOAST_DURATION_SECONDS = 2;

            /// <summary>
            /// Loading indicator minimum display time in milliseconds
            /// </summary>
            public const int MIN_LOADING_DISPLAY_MS = 500;

            /// <summary>
            /// Debounce delay for search input in milliseconds
            /// </summary>
            public const int SEARCH_DEBOUNCE_DELAY_MS = 500;

            /// <summary>
            /// Minimum search text length before triggering search
            /// </summary>
            public const int MIN_SEARCH_LENGTH = 2;

            /// <summary>
            /// Maximum number of recent searches to store
            /// </summary>
            public const int MAX_RECENT_SEARCHES = 10;
        }

        /// <summary>
        /// Media and content constants
        /// </summary>
        public static class Media
        {
            /// <summary>
            /// Maximum number of photos allowed per spot
            /// </summary>
            public const int MAX_PHOTOS_PER_SPOT = 5;

            /// <summary>
            /// Maximum file size for photos in bytes (5MB)
            /// </summary>
            public const long MAX_PHOTO_SIZE_BYTES = 5 * 1024 * 1024;

            /// <summary>
            /// Supported image formats
            /// </summary>
            public static readonly string[] SUPPORTED_IMAGE_FORMATS = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            /// <summary>
            /// Image compression quality (0-100)
            /// </summary>
            public const int IMAGE_COMPRESSION_QUALITY = 85;

            /// <summary>
            /// Maximum image width for optimization
            /// </summary>
            public const int MAX_IMAGE_WIDTH = 1920;

            /// <summary>
            /// Maximum image height for optimization
            /// </summary>
            public const int MAX_IMAGE_HEIGHT = 1080;
        }

        /// <summary>
        /// Validation and business rules constants
        /// </summary>
        public static class Validation
        {
            /// <summary>
            /// Minimum length for spot names
            /// </summary>
            public const int MIN_SPOT_NAME_LENGTH = 3;

            /// <summary>
            /// Maximum length for spot names
            /// </summary>
            public const int MAX_SPOT_NAME_LENGTH = 100;

            /// <summary>
            /// Minimum length for spot descriptions
            /// </summary>
            public const int MIN_SPOT_DESCRIPTION_LENGTH = 10;

            /// <summary>
            /// Maximum length for spot descriptions
            /// </summary>
            public const int MAX_SPOT_DESCRIPTION_LENGTH = 2000;

            /// <summary>
            /// Maximum allowed depth in meters for diving spots
            /// </summary>
            public const int MAX_DEPTH_METERS = 200;

            /// <summary>
            /// Minimum length for access descriptions
            /// </summary>
            public const int MIN_ACCESS_DESCRIPTION_LENGTH = 10;

            /// <summary>
            /// Maximum length for safety notes
            /// </summary>
            public const int MAX_SAFETY_NOTES_LENGTH = 500;
        }

        /// <summary>
        /// Network and API constants
        /// </summary>
        public static class Network
        {
            /// <summary>
            /// Default HTTP client timeout in seconds
            /// </summary>
            public const int HTTP_TIMEOUT_SECONDS = 30;

            /// <summary>
            /// Maximum number of concurrent requests
            /// </summary>
            public const int MAX_CONCURRENT_REQUESTS = 5;

            /// <summary>
            /// Request retry delay in milliseconds
            /// </summary>
            public const int REQUEST_RETRY_DELAY_MS = 2000;

            /// <summary>
            /// Maximum number of request retry attempts
            /// </summary>
            public const int MAX_REQUEST_RETRIES = 3;
        }

        /// <summary>
        /// Security and authentication constants
        /// </summary>
        public static class Security
        {
            /// <summary>
            /// JWT token expiry time in hours
            /// </summary>
            public const int JWT_EXPIRY_HOURS = 24;

            /// <summary>
            /// Refresh token expiry time in days
            /// </summary>
            public const int REFRESH_TOKEN_EXPIRY_DAYS = 30;

            /// <summary>
            /// Minimum password length
            /// </summary>
            public const int MIN_PASSWORD_LENGTH = 8;

            /// <summary>
            /// Maximum login attempts before lockout
            /// </summary>
            public const int MAX_LOGIN_ATTEMPTS = 5;

            /// <summary>
            /// Account lockout duration in minutes
            /// </summary>
            public const int LOCKOUT_DURATION_MINUTES = 15;
        }
    }
}